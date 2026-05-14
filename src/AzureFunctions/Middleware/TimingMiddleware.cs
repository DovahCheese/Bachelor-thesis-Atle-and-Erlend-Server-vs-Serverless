using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Middleware;

/// <summary>
/// Logs timing for every HTTP-triggered function in the same format as WebApi's
/// TimingMiddleware: "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms"
/// </summary>
public class TimingMiddleware(ILogger<TimingMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var reqData = await context.GetHttpRequestDataAsync();
        var method  = reqData?.Method ?? "?";
        var path    = reqData?.Url.AbsolutePath ?? "?";

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();

            if (reqData is not null)
            {
                var statusCode = ResolveStatusCode(context);
                logger.LogInformation(
                    "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                    method, path, statusCode, sw.ElapsedMilliseconds);
            }
        }
    }

    private static int ResolveStatusCode(FunctionContext context)
    {
        var result = context.GetInvocationResult();
        // ObjectResult covers OkObjectResult, BadRequestObjectResult, etc.
        // StatusCodeResult covers OkResult, NoContentResult, NotFoundResult, etc.
        return result?.Value switch
        {
            ObjectResult o     => o.StatusCode ?? 200,
            StatusCodeResult s => s.StatusCode,
            _                  => 200,
        };
    }
}
