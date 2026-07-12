using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Fintech.Core.Domain;

namespace Fintech;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            await HandleExceptionAsync(context, 400, "VALIDATION_ERROR", errors);
        }
        catch (FinVedaException ex)
        {
            await HandleExceptionAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, 500, "INTERNAL_ERROR", ex.Message);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, int statusCode, string errorCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var errorResponse = new
        {
            statusCode = statusCode,
            error = errorCode,
            message = message
        };

        var result = JsonSerializer.Serialize(errorResponse);
        return context.Response.WriteAsync(result);
    }
}
