using AgainstTheSpread.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for retrieving weekly lines (games with betting info)
/// </summary>
public class LinesFunction
{
    private readonly ILogger<LinesFunction> _logger;
    private readonly IStorageService _storageService;

    public LinesFunction(ILogger<LinesFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    /// <summary>
    /// GET /api/lines/{week}?year={year}
    /// Returns all games with lines for a specific week
    /// </summary>
    [FunctionName("GetLines")]
    public async Task<IActionResult> GetLines(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lines/{week}")] HttpRequest req,
        int week)
    {
        _logger.LogInformation("Processing GetLines request for week {Week}", week);

        try
        {
            // Validate week number
            if (week < 1 || week > 14)
            {
                return new BadRequestObjectResult(new { error = "Week must be between 1 and 14" });
            }

            // Get year from query string, default to current year
            var yearString = req.Query["year"];
            var year = string.IsNullOrEmpty(yearString)
                ? DateTime.UtcNow.Year
                : int.Parse(yearString!);

            var lines = await _storageService.GetLinesAsync(week, year);

            if (lines == null)
            {
                return new NotFoundObjectResult(new
                {
                    error = $"Lines not found for week {week} of {year}"
                });
            }

            return new OkObjectResult(lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lines for week {Week}", week);
            return new ObjectResult(new { error = "Failed to retrieve lines" })
            {
                StatusCode = 500
            };
        }
    }
}
