using Livability.Api.Context;
using Livability.Api.Mappings;
using Livability.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()    // 允許所有來源
              .AllowAnyHeader()    // 允許所有標頭
              .AllowAnyMethod();   // 允許所有 HTTP 方法 (GET/POST/PUT/DELETE)
    });
});
// Add services to the container.
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<NpaTmaImportService>();

builder.Services.AddDbContext<LivabilityContext>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
