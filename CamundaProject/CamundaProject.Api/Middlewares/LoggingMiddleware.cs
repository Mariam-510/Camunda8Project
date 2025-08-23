using Serilog;
using System.Diagnostics;

namespace CamundaProject.Api.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                Log.Information($"Starting request {context.Request.Method} {context.Request.Path}");
                await _next(context);
                stopwatch.Stop();

                var statusCode = context.Response.StatusCode;
                var logLevel = statusCode >= 400 ? Serilog.Events.LogEventLevel.Warning : Serilog.Events.LogEventLevel.Information;

                Log.Write(logLevel, $"Completed request {context.Request.Method} {context.Request.Path} with status {statusCode} in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, $"Request {context.Request.Method} {context.Request.Path} failed with exception after {stopwatch.ElapsedMilliseconds}ms");
                throw; // Re-throw to let the error handling middleware handle it
            }
        }
    }
}
