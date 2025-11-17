using AgainstTheSpread.Core.Models;
using System.Net.Http.Json;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for calling the Azure Functions API
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get list of available weeks for a given year
    /// </summary>
    public async Task<List<int>> GetAvailableWeeksAsync(int year)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<WeeksResponse>($"api/weeks?year={year}");
            return response?.Weeks ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    /// <summary>
    /// Get lines for a specific week
    /// </summary>
    public async Task<WeeklyLines?> GetLinesAsync(int week, int year)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WeeklyLines>($"api/lines/{week}?year={year}");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Submit user picks and download Excel file
    /// </summary>
    public async Task<byte[]?> SubmitPicksAsync(UserPicks userPicks)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/picks", userPicks);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Upload weekly lines file
    /// </summary>
    public async Task<UploadResponse?> UploadLinesAsync(int week, int year, Stream fileStream, string fileName)
    {
        try
        {
            // The backend expects raw file content in the body, not multipart
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var response = await _httpClient.PostAsync($"api/upload-lines?week={week}&year={year}", streamContent);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UploadResponse>();
            }

            // Try to read error details from response
            try
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new UploadResponse 
                { 
                    Success = false, 
                    Message = $"Upload failed with status {response.StatusCode}: {errorContent}" 
                };
            }
            catch
            {
                return new UploadResponse 
                { 
                    Success = false, 
                    Message = $"Upload failed with status {response.StatusCode}" 
                };
            }
        }
        catch (Exception ex)
        {
            return new UploadResponse 
            { 
                Success = false, 
                Message = $"Exception during upload: {ex.Message}" 
            };
        }
    }

    private class WeeksResponse
    {
        public int Year { get; set; }
        public List<int> Weeks { get; set; } = new();
    }

    public class UploadResponse
    {
        public bool Success { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }
        public int GamesCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
