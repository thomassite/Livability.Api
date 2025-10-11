using AutoMapper;
using CsvHelper;
using Livability.Api.Context;
using Livability.Api.Dto;
using Livability.Api.Services;
using Livability.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class MapGeocodeService : BaseService, IMapGeocodeService
{
    private readonly HttpClient _httpClient;
    private readonly IApiQuotaService _quotaService;
    private readonly string _apiKey;
    private readonly int _dailyLimit = 2000;
    private readonly int _requestDelayMs = 100; // 避免 QPS 超限

    public MapGeocodeService(
        LivabilityContext db,
        IMapper mapper,
        ILogger<MapGeocodeService> logger,
        IApiQuotaService quotaService,
        IHttpClientFactory factory,
        IConfiguration config): base(db, mapper, logger)
    {
        _quotaService = quotaService;
        _httpClient = factory.CreateClient("google");
        _apiKey = config["GoogleMap:ApiKey"] ?? throw new ArgumentNullException("Google:ApiKey");
        _dailyLimit = Convert.ToInt32(config["GoogleMap:DailyLimit"] ?? "2000");
        _requestDelayMs = Convert.ToInt32(config["GoogleMap:RequestDelayMs"] ?? "100");
    }

    /// <summary>
    /// 查詢地點經緯度，含快取與配額控制。
    /// </summary>
    public async Task<(double? lat, double? lng, bool partialMatch)> GetCoordinatesAsync(string placeName)
    {
        if (string.IsNullOrWhiteSpace(placeName))
            throw new ArgumentException("地點名稱不可為空。", nameof(placeName));

        // ✅ 配額檢查
        var (allowed, blocked) = await _quotaService.TryConsumeAsync(
            provider: "google",
            apiName: "geocode",
            clientId: "livability-api",
            ip: null,
            userAgent: "server-job",
            dailyLimit: _dailyLimit,
            hourlyLimit: 10000,
            blockThreshold: 10000,
            blockMinutes: 1440);

        if (blocked)
        {
            _logger.LogWarning("🚫 Google API 已暫時封鎖，略過 {Place}", placeName);
            return (null, null, false);
        }

        if (!allowed)
        {
            _logger.LogWarning("⛔ 超出 Google Geocode 每日上限，略過 {Place}", placeName);
            return (null, null, false);
        }

        // 📦 查快取
        var cached = await _db.GeoLocations.FirstOrDefaultAsync(x => x.PlaceName == placeName && !string.IsNullOrEmpty(x.JsonRaw));
        if (cached != null)
        {
            _logger.LogInformation("📦 命中快取：{Place}", placeName);
            return ((double?)cached.Latitude, (double?)cached.Longitude, cached.PartialMatch ?? false);
        }

        // 🧭 第一層：Geocoding API
        var encoded = Uri.EscapeDataString(placeName);
        var geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={encoded}&key={_apiKey}";

        await Task.Delay(_requestDelayMs);

        var res = await _httpClient.GetAsync(geocodeUrl);
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("⚠️ Geocode API 回應異常 {Status}：{Place}", res.StatusCode, placeName);
            return (null, null, false);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        double? lat = null, lng = null;
        bool partial = false;
        string? formattedAddress = null, locationType = null, placeId = null, typesJoined = null;
        string? postalCode = null, country = null, level1 = null, level2 = null, level3 = null, route = null, streetNumber = null;

        // ✅ 成功解析 Geocode 結果
        if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
        {
            var first = results[0];
            formattedAddress = first.GetProperty("formatted_address").GetString();
            placeId = first.GetProperty("place_id").GetString();

            if (first.TryGetProperty("geometry", out var geometry) &&
                geometry.TryGetProperty("location", out var location))
            {
                lat = location.GetProperty("lat").GetDouble();
                lng = location.GetProperty("lng").GetDouble();
                locationType = geometry.GetProperty("location_type").GetString();
            }

            partial = first.TryGetProperty("partial_match", out var p) && p.GetBoolean();

            if (first.TryGetProperty("address_components", out var comps))
            {
                foreach (var comp in comps.EnumerateArray())
                {
                    var types = comp.GetProperty("types").EnumerateArray().Select(t => t.GetString()).ToList();
                    var name = comp.GetProperty("long_name").GetString();

                    if (types.Contains("postal_code")) postalCode = name;
                    else if (types.Contains("country")) country = name;
                    else if (types.Contains("administrative_area_level_1")) level1 = name;
                    else if (types.Contains("administrative_area_level_2")) level2 = name;
                    else if (types.Contains("administrative_area_level_3")) level3 = name;
                    else if (types.Contains("route")) route = name;
                    else if (types.Contains("street_number")) streetNumber = name;
                }
            }

            if (first.TryGetProperty("types", out var typeArr))
                typesJoined = string.Join(",", typeArr.EnumerateArray().Select(x => x.GetString()));
        }
        else
        {
            _logger.LogWarning("⚠️ Geocode 查無結果：{Place}，改用 Places API 補查", placeName);

            // 🧭 第二層：Find Place from Text
            var placeUrl = $"https://maps.googleapis.com/maps/api/place/findplacefromtext/json" +
                           $"?input={encoded}&inputtype=textquery&fields=formatted_address,name,geometry,place_id&key={_apiKey}";

            var placeRes = await _httpClient.GetAsync(placeUrl);
            var placeJson = await placeRes.Content.ReadAsStringAsync();

            if (placeRes.IsSuccessStatusCode)
            {
                json = placeJson; // ⚡ 替換 JSON 儲存內容
                using var placeDoc = JsonDocument.Parse(placeJson);
                var placeRoot = placeDoc.RootElement;

                if (placeRoot.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    formattedAddress = first.GetProperty("formatted_address").GetString();
                    placeId = first.GetProperty("place_id").GetString();

                    if (first.TryGetProperty("geometry", out var geom) &&
                        geom.TryGetProperty("location", out var loc))
                    {
                        lat = loc.GetProperty("lat").GetDouble();
                        lng = loc.GetProperty("lng").GetDouble();
                    }

                    _logger.LogInformation("🧭 Places API 成功補查：{Place}", placeName);
                }
                else
                {
                    _logger.LogWarning("⚠️ Places API 仍查無結果：{Place}", placeName);
                }
            }
            else
            {
                _logger.LogWarning("❌ Places API 呼叫失敗 ({Status})：{Place}", placeRes.StatusCode, placeName);
            }
        }

        // 🧩 Upsert 儲存
        var existing = await _db.GeoLocations.FirstOrDefaultAsync(x => x.PlaceName == placeName);
        if (existing != null)
        {
            existing.FormattedAddress = formattedAddress;
            existing.PostalCode = postalCode;
            existing.Country = country;
            existing.AdministrativeAreaLevel1 = level1;
            existing.AdministrativeAreaLevel2 = level2;
            existing.AdministrativeAreaLevel3 = level3;
            existing.Route = route;
            existing.StreetNumber = streetNumber;
            existing.Latitude = lat.HasValue ? (decimal?)lat : null;
            existing.Longitude = lng.HasValue ? (decimal?)lng : null;
            existing.LocationType = locationType;
            existing.PlaceId = placeId;
            existing.PartialMatch = partial;
            existing.Types = typesJoined;
            existing.JsonRaw = json;

            _logger.LogInformation("♻️ 更新地理資料：{Place}", placeName);
        }
        else
        {
            var entity = new GeoLocation
            {
                PlaceName = placeName,
                FormattedAddress = formattedAddress,
                PostalCode = postalCode,
                Country = country,
                AdministrativeAreaLevel1 = level1,
                AdministrativeAreaLevel2 = level2,
                AdministrativeAreaLevel3 = level3,
                Route = route,
                StreetNumber = streetNumber,
                Latitude = lat.HasValue ? (decimal?)lat : null,
                Longitude = lng.HasValue ? (decimal?)lng : null,
                LocationType = locationType,
                PlaceId = placeId,
                PartialMatch = partial,
                Types = typesJoined,
                JsonRaw = json,
                CreatedAt = DateTime.Now
            };
            _db.GeoLocations.Add(entity);
            _logger.LogInformation("✅ 新增地理資料：{Place}", placeName);
        }

        await _db.SaveChangesAsync();

        if (lat.HasValue && lng.HasValue)
            _logger.LogInformation("📍 已儲存：{Place} ({Lat}, {Lng})", placeName, lat, lng);
        else
            _logger.LogWarning("⚠️ 已儲存查無結果的負快取：{Place}", placeName);

        return (lat, lng, partial);
    }

    /// <summary>
    /// 🔁 [Hangfire Job] 批次補齊尚未打過 Google Maps API 的 pcc_agency 座標
    /// </summary>
    public async Task BatchFillMissingAgenciesAsync()
    {
        _logger.LogInformation("🚀 開始執行批次補座標 (pcc_agency)...");

        var geoLocations = await _db.GeoLocations
            .Where(a => string.IsNullOrWhiteSpace(a.JsonRaw))
            .OrderBy(a => a.Id)
            .ToListAsync();

        if (!geoLocations.Any())
        {
            _logger.LogInformation("✅ 所有機關皆已有座標，無需補充。");
            return;
        }

        _logger.LogInformation("📍 發現 {Count} 個尚未補座標的機關。", geoLocations.Count);

        int success = 0;
        foreach (var geoLocation in geoLocations)
        {
            try
            {
                _logger.LogInformation("🌍 查詢中：{geoLocations}", geoLocation.PlaceName);
                var (lat, lng, _) = await GetCoordinatesAsync(geoLocation.PlaceName);

                if (lat.HasValue && lng.HasValue)
                {
                    success++;
                    _logger.LogInformation("✅ 已補齊 {Agency} ({Lat}, {Lng})", geoLocation.PlaceName, lat, lng);
                }

                await Task.Delay(_requestDelayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 查詢失敗：{PlaceName}", geoLocation.PlaceName);
            }
        }

        _logger.LogInformation("🎯 補座標任務完成，成功 {Success}/{Total} 筆。", success, geoLocations.Count);
    }
}
