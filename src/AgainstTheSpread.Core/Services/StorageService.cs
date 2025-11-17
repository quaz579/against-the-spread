using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Service for storing and retrieving data from Azure Blob Storage
/// MVP: Admin manually uploads files, this service only reads them
/// </summary>
public class StorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly IExcelService _excelService;
    private const string ContainerName = "gamefiles";
    private const string LinesFolder = "lines";

    public StorageService(string connectionString, IExcelService excelService)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        _excelService = excelService;
    }

    /// <summary>
    /// Uploads weekly lines Excel file and parsed JSON to blob storage
    /// Note: In MVP, admin uploads manually via script, not via this method
    /// This is kept for potential future use or testing
    /// </summary>
    public async Task<string> UploadLinesAsync(
        int week,
        int year,
        Stream excelStream,
        WeeklyLines weeklyLines,
        CancellationToken cancellationToken = default)
    {
        // Ensure container exists
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Upload Excel file
        var excelBlobName = $"{LinesFolder}/week-{week}-{year}.xlsx";
        var excelBlobClient = _containerClient.GetBlobClient(excelBlobName);

        excelStream.Position = 0;
        await excelBlobClient.UploadAsync(
            excelStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        // Upload parsed JSON for fast API access
        var jsonBlobName = $"{LinesFolder}/week-{week}-{year}.json";
        var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

        var json = JsonSerializer.Serialize(weeklyLines, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await jsonBlobClient.UploadAsync(
            BinaryData.FromString(json),
            overwrite: true,
            cancellationToken: cancellationToken);

        return excelBlobClient.Uri.ToString();
    }

    /// <summary>
    /// Retrieves parsed weekly lines for a specific week
    /// Reads from JSON blob for performance (admin script creates this during upload)
    /// </summary>
    public async Task<WeeklyLines?> GetLinesAsync(
        int week,
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonBlobName = $"{LinesFolder}/week-{week}-{year}.json";
            var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

            if (!await jsonBlobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var download = await jsonBlobClient.DownloadContentAsync(cancellationToken);
            var json = download.Value.Content.ToString();

            return JsonSerializer.Deserialize<WeeklyLines>(json);
        }
        catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("not found"))
        {
            return null;
        }
    }

    /// <summary>
    /// Gets list of all available weeks that have lines uploaded
    /// Scans the lines folder for JSON files
    /// </summary>
    public async Task<List<int>> GetAvailableWeeksAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var weeks = new List<int>();

        try
        {
            var prefix = $"{LinesFolder}/week-";
            Console.WriteLine($"[StorageService] GetAvailableWeeksAsync called for year {year}");
            Console.WriteLine($"[StorageService] Searching with prefix: {prefix}");
            
            int totalBlobs = 0;
            int matchingBlobs = 0;
            
            await foreach (var blobItem in _containerClient.GetBlobsAsync(
                prefix: prefix,
                cancellationToken: cancellationToken))
            {
                totalBlobs++;
                Console.WriteLine($"[StorageService] Found blob: {blobItem.Name}");
                
                // Extract week number from blob name like "lines/week-1-2024.json"
                var fileName = blobItem.Name;
                if (!fileName.EndsWith($"-{year}.json"))
                {
                    Console.WriteLine($"[StorageService] Skipping {fileName} - doesn't end with -{year}.json");
                    continue;
                }
                
                matchingBlobs++;
                Console.WriteLine($"[StorageService] Blob {fileName} matches year {year}");

                var parts = fileName.Split('-');
                Console.WriteLine($"[StorageService] Split parts: [{string.Join(", ", parts)}]");
                
                if (parts.Length >= 2 && int.TryParse(parts[1], out int week))
                {
                    Console.WriteLine($"[StorageService] Extracted week number: {week}");
                    weeks.Add(week);
                }
                else
                {
                    Console.WriteLine($"[StorageService] Failed to parse week from parts[1]: {(parts.Length >= 2 ? parts[1] : "N/A")}");
                }
            }

            Console.WriteLine($"[StorageService] Total blobs found: {totalBlobs}");
            Console.WriteLine($"[StorageService] Matching blobs: {matchingBlobs}");
            Console.WriteLine($"[StorageService] Weeks extracted: [{string.Join(", ", weeks)}]");

            weeks.Sort();
            return weeks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StorageService] Exception in GetAvailableWeeksAsync: {ex.Message}");
            Console.WriteLine($"[StorageService] Stack trace: {ex.StackTrace}");
            return new List<int>();
        }
    }

    /// <summary>
    /// Uploads weekly lines Excel file to blob storage
    /// Parses the Excel and creates JSON cache for API performance
    /// </summary>
    public async Task<string> UploadWeeklyLinesAsync(
        Stream excelStream,
        int week,
        int year,
        CancellationToken cancellationToken = default)
    {
        // Ensure container exists
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Parse the Excel file
        excelStream.Position = 0;
        var weeklyLines = await _excelService.ParseWeeklyLinesAsync(excelStream, week, year, cancellationToken);

        // Upload Excel file
        var excelBlobName = $"{LinesFolder}/week-{week}-{year}.xlsx";
        var excelBlobClient = _containerClient.GetBlobClient(excelBlobName);

        excelStream.Position = 0;
        await excelBlobClient.UploadAsync(
            excelStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        // Upload parsed JSON for fast API access
        var jsonBlobName = $"{LinesFolder}/week-{week}-{year}.json";
        var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

        var json = JsonSerializer.Serialize(weeklyLines, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await jsonBlobClient.UploadAsync(
            BinaryData.FromString(json),
            overwrite: true,
            cancellationToken: cancellationToken);

        return excelBlobClient.Uri.ToString();
    }
}
