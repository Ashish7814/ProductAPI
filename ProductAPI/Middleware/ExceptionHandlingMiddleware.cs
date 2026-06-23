using Product.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace ProductAPI.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                NotFoundException ex => new ErrorResponse(
                    (int)HttpStatusCode.NotFound, "Not Found", ex.Message),
               ValidationException ex => new ValidationErrorResponse(
                    (int)HttpStatusCode.BadRequest, "Validation Failed", ex.Message, ex.Errors),
                UnauthorizedException ex => new ErrorResponse(
                    (int)HttpStatusCode.Unauthorized, "Unauthorized", ex.Message),
                ConflictException ex => new ErrorResponse(
                    (int)HttpStatusCode.Conflict, "Conflict", ex.Message),
                _ => new ErrorResponse(
                    (int)HttpStatusCode.InternalServerError, "Internal Server Error",
                    "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = response.StatusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    public record ErrorResponse(int StatusCode, string Error, string Message);
    public record ValidationErrorResponse(
        int StatusCode, string Error, string Message,
        IDictionary<string, string[]> ValidationErrors) : ErrorResponse(StatusCode, Error, Message);

}
