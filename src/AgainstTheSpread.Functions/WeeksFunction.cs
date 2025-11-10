using AgainstTheSpread.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoints for retrieving available weeks with lines
/// </summary>
public class WeeksFunction
{
    private readonly ILogger<WeeksFunction> _logger;
    private readonly IStorageService _storageService;

    public WeeksFunction(ILogger<WeeksFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    /// <summary>
    /// GET /api/weeks?year={year}
    /// Returns list of week numbers that have lines available
    /// </summary>
    [FunctionName("GetWeeks")]
    public async Task<IActionResult> GetWeeks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weeks")] HttpRequest req)
    {
        _logger.LogInformation("Processing GetWeeks request");

        try
        {
            // Get year from query string, default to current year
            var yearString = req.Query["year"];
            var year = string.IsNullOrEmpty(yearString)
                ? DateTime.UtcNow.Year
                : int.Parse(yearString);

            var weeks = await _storageService.GetAvailableWeeksAsync(year);

            return new OkObjectResult(new
            {
                year,
                weeks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available weeks");
            return new ObjectResult(new { error = "Failed to retrieve available weeks" })
            {
                StatusCode = 500
            };
        }
    }
}
