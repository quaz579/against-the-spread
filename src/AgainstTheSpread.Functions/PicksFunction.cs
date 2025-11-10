using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for submitting user picks and downloading Excel
/// </summary>
public class PicksFunction
{
    private readonly ILogger<PicksFunction> _logger;
    private readonly IExcelService _excelService;

    public PicksFunction(ILogger<PicksFunction> logger, IExcelService excelService)
    {
        _logger = logger;
        _excelService = excelService;
    }

    /// <summary>
    /// POST /api/picks
    /// Accepts user picks and returns Excel file in exact format
    /// </summary>
    [FunctionName("SubmitPicks")]
    public async Task<IActionResult> SubmitPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "picks")] HttpRequest req)
    {
        _logger.LogInformation("Processing SubmitPicks request");

        try
        {
            // Parse request body
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var userPicks = JsonSerializer.Deserialize<UserPicks>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userPicks == null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            // Set submission time
            userPicks.SubmittedAt = DateTime.UtcNow;

            // Validate picks
            if (!userPicks.IsValid())
            {
                var validationError = userPicks.GetValidationError();
                return new BadRequestObjectResult(new { error = validationError });
            }

            // Generate Excel file
            var excelBytes = await _excelService.GeneratePicksExcelAsync(userPicks);

            // Return Excel file
            return new FileContentResult(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"{userPicks.Name.Replace(" ", "_")}_Week_{userPicks.Week}_Picks.xlsx"
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in SubmitPicks");
            return new BadRequestObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing picks submission");
            return new ObjectResult(new { error = "Failed to generate picks file" })
            {
                StatusCode = 500
            };
        }
    }
}
