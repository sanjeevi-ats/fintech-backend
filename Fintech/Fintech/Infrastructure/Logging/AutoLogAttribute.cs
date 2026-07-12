using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Fintech.Infrastructure.Logging
{
    /// <summary>
    /// Action filter attribute that automatically logs controller actions to controller-specific files.
    /// Eliminates the need for manual logging in each controller action.
    /// Usage: Apply [AutoLog] attribute to controller classes or individual action methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AutoLogAttribute : Attribute, IAsyncActionFilter
    {
        private readonly bool _logRequestBody;
        private readonly bool _logResponseBody;

        /// <summary>
        /// Initialize AutoLog attribute
        /// </summary>
        /// <param name="logRequestBody">Whether to log request body (default: true)</param>
        /// <param name="logResponseBody">Whether to log response body (default: true)</param>
        public AutoLogAttribute(bool logRequestBody = true, bool logResponseBody = true)
        {
            _logRequestBody = logRequestBody;
            _logResponseBody = logResponseBody;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var stopwatch = Stopwatch.StartNew();
            var controllerName = context.Controller.GetType().Name;
            var actionName = context.ActionDescriptor.DisplayName;
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<AutoLogAttribute>)) as ILogger<AutoLogAttribute>;
            var controllerLogger = context.HttpContext.RequestServices.GetService(typeof(IControllerFileLoggerService)) as IControllerFileLoggerService;

            if (controllerLogger == null || logger == null)
            {
                await next();
                return;
            }

            try
            {
                // Log request
                var requestParameters = context.ActionArguments;
                object? requestBody = null;

                if (_logRequestBody && context.ActionArguments.Any())
                {
                    requestBody = context.ActionArguments.Count == 1 
                        ? context.ActionArguments.Values.First() 
                        : context.ActionArguments;
                }

                // Execute the action
                var executedContext = await next();
                stopwatch.Stop();

                // Log response
                object? responseData = null;
                if (_logResponseBody && executedContext.Result is ObjectResult objectResult)
                {
                    responseData = objectResult.Value;
                }

                // Determine if successful
                var statusCode = (executedContext.Result as ObjectResult)?.StatusCode ?? 200;
                var isSuccess = statusCode >= 200 && statusCode < 300;

                if (isSuccess)
                {
                    await controllerLogger.LogInfoAsync(
                        controllerName,
                        actionName,
                        requestParameters: requestParameters,
                        requestBody: requestBody,
                        responseData: responseData,
                        executionTimeMs: stopwatch.ElapsedMilliseconds,
                        successMessage: $"{actionName} completed successfully"
                    );
                }
                else
                {
                    await controllerLogger.LogWarningAsync(
                        controllerName,
                        actionName,
                        warningMessage: $"Action returned status code {statusCode}",
                        executionTimeMs: stopwatch.ElapsedMilliseconds
                    );
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger?.LogError(ex, "Error in AutoLog filter for {Controller}.{Action}", controllerName, actionName);

                // Log the exception
                await controllerLogger.LogErrorAsync(
                    controllerName,
                    actionName,
                    ex,
                    requestParameters: context.ActionArguments,
                    executionTimeMs: stopwatch.ElapsedMilliseconds
                );

                throw;
            }
        }
    }
}
