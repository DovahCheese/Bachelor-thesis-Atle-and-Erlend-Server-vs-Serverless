using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzureFunctions.Functions;

public class HealthFunctions
{
    /// <summary>
    /// GET /api/health — no-op liveness probe.
    /// Used as a latency baseline in k6 benchmarks (same endpoint as WebApi).
    /// Also used by cold-start.js to measure Functions Consumption plan wake-up time.
    /// </summary>
    [Function("Health")]
    public IActionResult Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        => new OkObjectResult(new { status = "healthy" });
}
