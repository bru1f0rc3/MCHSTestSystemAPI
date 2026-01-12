using System.Net;
using System.Text.Json;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Middleware;

// Глобальная обработка ошибок
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
            _logger.LogError(ex, "Ошибка: {Message}", ex.Message);
            await HandleError(context, ex);
        }
    }

    private static Task HandleError(HttpContext context, Exception ex)
    {
        var (code, message) = ex switch
        {
            ArgumentException or InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Доступ запрещен"),
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "Ошибка сервера")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var response = ApiResponse<object>.Fail(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        return context.Response.WriteAsync(json);
    }
}

// Extension для регистрации
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        => builder.UseMiddleware<ExceptionHandlingMiddleware>();
}
