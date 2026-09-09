using AuthSystem.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace AuthSystem.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception occurred. TraceId: {TraceId}",
                    context.TraceIdentifier);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                ConflictException =>
                    StatusCodes.Status409Conflict,

                BusinessException =>
                    StatusCodes.Status400BadRequest,

                KeyNotFoundException =>
                    StatusCodes.Status404NotFound,

                _ =>
                    StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var response = new
            {
                StatusCode = statusCode,
                Message = exception switch
                {
                    BusinessException => exception.Message,
                    KeyNotFoundException => exception.Message,
                    _ => "An unexpected error occurred."
                },
                TraceId = context.TraceIdentifier
            };

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }
    }
}
