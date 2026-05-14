using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Middleware;
using WebApi.Repositories;
using WebApi.Services;
using WebApi.Stores;
using Shared;
using Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Application Insights ──────────────────────────────────────────────────────
// Only active in Azure where APPLICATIONINSIGHTS_CONNECTION_STRING is set.
// Skipped locally to avoid a crash on startup.
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddApplicationInsightsTelemetry();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Controllers + JSON ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Ordle services ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IWordRepository, FileWordRepository>();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IGameStore, SqlGameStore>();
builder.Services.AddSingleton<OrdleGameFactory>();
builder.Services.AddSingleton<WordValidator>();

// ── Image store: local disk in Development, Azure Blob in Production ──────────
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IImageStore, LocalImageStore>();
else
    builder.Services.AddScoped<IImageStore, BlobImageStore>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AppCors");
app.UseMiddleware<TimingMiddleware>();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();
