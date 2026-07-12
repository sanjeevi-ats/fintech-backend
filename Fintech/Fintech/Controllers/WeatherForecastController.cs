using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Fintech.Infrastructure.Logging;

namespace Fintech.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IFileLoggerService _fileLogger;
        private readonly ILogger<WeatherForecastController> _logger;
        private const string ApiName = "Weather Forecast API";

        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        public WeatherForecastController(IFileLoggerService fileLogger, ILogger<WeatherForecastController> logger)
        {
            _fileLogger = fileLogger;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IActionResult Get()
        {
            var stopwatch = LoggingHelper.StartTimer();
            try
            {
                var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();

                var executionTime = LoggingHelper.StopTimer(stopwatch);
                
                _fileLogger.LogInfoAsync(
                    ApiName,
                    nameof(WeatherForecastController),
                    nameof(Get),
                    responseData: forecasts,
                    executionTimeMs: executionTime,
                    successMessage: "Weather forecasts retrieved successfully"
                ).GetAwaiter().GetResult();
                
                return Ok(forecasts);
            }
            catch (Exception ex)
            {
                var executionTime = LoggingHelper.StopTimer(stopwatch);
                _logger.LogError(ex, "Error in Get");
                
                _fileLogger.LogErrorAsync(
                    ApiName,
                    nameof(WeatherForecastController),
                    nameof(Get),
                    ex,
                    executionTimeMs: executionTime
                ).GetAwaiter().GetResult();
                
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving weather forecast.",
                    Error = ex.Message
                });
            }
        }
    }
}
