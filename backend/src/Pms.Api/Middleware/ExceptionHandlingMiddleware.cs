using System.Text.Json;
using FluentValidation;
using Pms.Domain.Exceptions;

namespace Pms.Api.Middleware;

/// <summary>Translates exceptions into RFC7807-style JSON problem responses.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteAsync(context, StatusCodes.Status400BadRequest, "Validation failed", new { errors });
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (LicenseException ex)
        {
            await WriteAsync(context, StatusCodes.Status402PaymentRequired, ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string message, object? extra = null)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var payload = new Dictionary<string, object?> { ["status"] = status, ["error"] = message };
        if (extra is not null)
            foreach (var prop in extra.GetType().GetProperties())
                payload[prop.Name] = prop.GetValue(extra);

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
