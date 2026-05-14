using System.Text.Json.Serialization;
using AzureFunctions.Middleware;
using AzureFunctions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;
using Shared.Interfaces;
using WebApi.Data;
using WebApi.Services;
using WebApi.Stores;

var host = new HostBuilder()
    .ConfigureAppConfiguration((ctx, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        config.AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWebApplication(worker =>
    {
        worker.UseMiddleware<CorsMiddleware>();
        worker.UseMiddleware<TimingMiddleware>();
    })
    .ConfigureServices((ctx, services) =>
    {
        // ── JSON: serialize enums as strings ("Correct" not 0) ───────────
        // Mirrors WebApi's AddJsonOptions configuration. Both paths need it:
        // ConfigureHttpJsonOptions covers minimal-API results,
        // Configure<JsonOptions> covers IActionResult (OkObjectResult etc.)
        services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // ── Database ──────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(ctx.Configuration.GetConnectionString("DefaultConnection")));

        // ── Ordle services ────────────────────────────────────────────────
        services.AddScoped<IGameStore, SqlGameStore>();
        services.AddSingleton<OrdleGameFactory>();
        services.AddSingleton<WordValidator>();
        services.AddSingleton<IWordRepository, FunctionsWordRepository>();

        // ── Image store (always Blob in Functions — no ephemeral disk) ────
        services.AddScoped<IImageStore, BlobImageStore>();
    })
    .Build();

host.Run();
