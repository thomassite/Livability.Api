using Hangfire;
using Hangfire.MySql;
using Livability.Api.Context;
using Livability.Api.Jobs;
using Livability.Api.Middleware;
using Livability.Api.Services;
using Livability.Api.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

// 加入 Hangfire 並設定 MySQL 儲存
builder.Services.AddHangfire(config =>
    config.UseStorage(
        new MySqlStorage(
            builder.Configuration.GetConnectionString("HangfireConnection"),
            new MySqlStorageOptions
            {
                TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                PrepareSchemaIfNecessary = true
            })
    )
);
// 啟用 Hangfire Server
builder.Services.AddHangfireServer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()    
            .AllowAnyHeader()   
            .AllowAnyMethod();   
    });
});

// Add services to the container.
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("google", client =>
{
    client.BaseAddress = new Uri("https://maps.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IApiQuotaService, ApiQuotaService>();
builder.Services.AddScoped<INpaTmaService, NpaTmaService>();
builder.Services.AddScoped<IPccTenderService, PccTenderService>();
builder.Services.AddScoped<IMapGeocodeService, MapGeocodeService>();
builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.AddDbContext<LivabilityContext>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<ApiRateLimitMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseHangfireDashboard("/hangfire");
HangfireJobRegistrar.RegisterJobs();

app.Run();
