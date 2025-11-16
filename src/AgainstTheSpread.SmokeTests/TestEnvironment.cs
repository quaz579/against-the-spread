using System.Diagnostics;
using System.Net.Http;
using Azure.Storage.Blobs;
using System.Text.Json;

namespace AgainstTheSpread.SmokeTests;

/// <summary>
/// Manages the test environment including Azurite, Functions, and Web app
/// </summary>
public class TestEnvironment : IDisposable
{
    private Process? _azuriteProcess;
    private Process? _functionsProcess;
    private Process? _webProcess;
    private readonly string _workingDirectory;
    private bool _disposed;

    public string AzuriteConnectionString { get; } = 
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
    
    public string FunctionsUrl { get; } = "http://localhost:7071";
    public string WebUrl { get; } = "http://localhost:5158";

    public TestEnvironment(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Start Azurite storage emulator
    /// </summary>
    public async Task StartAzuriteAsync()
    {
        Console.WriteLine("Starting Azurite...");
        
        var azuriteDir = Path.Combine(Path.GetTempPath(), "azurite-test");
        Directory.CreateDirectory(azuriteDir);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "npx",
            Arguments = $"azurite --silent --location \"{azuriteDir}\" --blobPort 10000",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _azuriteProcess = Process.Start(processStartInfo);
        
        if (_azuriteProcess == null)
        {
            throw new Exception("Failed to start Azurite");
        }

        // Wait for Azurite to be ready
        await WaitForServiceAsync("http://127.0.0.1:10000/devstoreaccount1", TimeSpan.FromSeconds(30));
        Console.WriteLine("Azurite started successfully");
    }

    /// <summary>
    /// Start Azure Functions
    /// </summary>
    public async Task StartFunctionsAsync()
    {
        Console.WriteLine("Starting Azure Functions...");
        
        var functionsPath = Path.Combine(_workingDirectory, "src", "AgainstTheSpread.Functions");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build",
            WorkingDirectory = functionsPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var buildProcess = Process.Start(processStartInfo);
        if (buildProcess != null)
        {
            await buildProcess.WaitForExitAsync();
            if (buildProcess.ExitCode != 0)
            {
                throw new Exception("Failed to build Functions project");
            }
        }

        // Now start func host
        processStartInfo = new ProcessStartInfo
        {
            FileName = "func",
            Arguments = "start --port 7071",
            WorkingDirectory = functionsPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Set environment variables
        processStartInfo.Environment["AzureWebJobsStorage"] = AzuriteConnectionString;
        processStartInfo.Environment["AZURE_STORAGE_CONNECTION_STRING"] = AzuriteConnectionString;

        _functionsProcess = Process.Start(processStartInfo);
        
        if (_functionsProcess == null)
        {
            throw new Exception("Failed to start Azure Functions");
        }

        // Wait for Functions to be ready
        await WaitForServiceAsync($"{FunctionsUrl}/api/weeks?year=2025", TimeSpan.FromSeconds(90));
        Console.WriteLine("Azure Functions started successfully");
    }

    /// <summary>
    /// Start Blazor Web App
    /// </summary>
    public async Task StartWebAppAsync()
    {
        Console.WriteLine("Starting Web App...");
        
        var webPath = Path.Combine(_workingDirectory, "src", "AgainstTheSpread.Web");

        // Build first
        var buildStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build",
            WorkingDirectory = webPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var buildProcess = Process.Start(buildStartInfo);
        if (buildProcess != null)
        {
            await buildProcess.WaitForExitAsync();
            if (buildProcess.ExitCode != 0)
            {
                throw new Exception("Failed to build Web project");
            }
        }

        // Now run the app
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build --urls http://localhost:5158",
            WorkingDirectory = webPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Set API URL
        processStartInfo.Environment["API_BASE_URL"] = FunctionsUrl;

        _webProcess = Process.Start(processStartInfo);
        
        if (_webProcess == null)
        {
            throw new Exception("Failed to start Web App");
        }

        // Wait for Web App to be ready
        await WaitForServiceAsync(WebUrl, TimeSpan.FromSeconds(90));
        Console.WriteLine("Web App started successfully");
    }

    /// <summary>
    /// Upload a weekly lines file to storage
    /// </summary>
    public async Task UploadLinesFileAsync(string filePath, int week, int year)
    {
        Console.WriteLine($"Uploading lines for Week {week}, Year {year}...");
        
        // Upload directly to Azurite using Azure Storage SDK
        var blobServiceClient = new BlobServiceClient(AzuriteConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient("gamefiles");
        
        // Create container if it doesn't exist
        await containerClient.CreateIfNotExistsAsync();
        
        // Upload Excel file
        var excelBlobName = $"lines/week-{week}-{year}.xlsx";
        var excelBlobClient = containerClient.GetBlobClient(excelBlobName);
        
        using var excelStream = File.OpenRead(filePath);
        await excelBlobClient.UploadAsync(excelStream, overwrite: true);
        
        // Create a simple JSON representation for the API to read
        // For smoke tests, we'll create a minimal valid JSON structure
        var jsonBlobName = $"lines/week-{week}-{year}.json";
        var jsonBlobClient = containerClient.GetBlobClient(jsonBlobName);
        
        // Parse the Excel file to create the JSON (we need the ExcelService for this)
        // For now, create a minimal JSON structure
        var weeklyLinesJson = new
        {
            week = week,
            year = year,
            games = new[]
            {
                new { favorite = "Team A", line = -7.0, vsAt = "vs", underdog = "Team B", gameDate = DateTime.Now, gameTime = "12:00 PM" },
                new { favorite = "Team C", line = -3.5, vsAt = "at", underdog = "Team D", gameDate = DateTime.Now, gameTime = "3:30 PM" },
                new { favorite = "Team E", line = -10.0, vsAt = "vs", underdog = "Team F", gameDate = DateTime.Now, gameTime = "7:00 PM" },
                new { favorite = "Team G", line = -14.5, vsAt = "at", underdog = "Team H", gameDate = DateTime.Now, gameTime = "8:00 PM" },
                new { favorite = "Team I", line = -21.0, vsAt = "vs", underdog = "Team J", gameDate = DateTime.Now, gameTime = "12:00 PM" },
                new { favorite = "Team K", line = -6.5, vsAt = "at", underdog = "Team L", gameDate = DateTime.Now, gameTime = "3:30 PM" },
                new { favorite = "Team M", line = -4.0, vsAt = "vs", underdog = "Team N", gameDate = DateTime.Now, gameTime = "7:00 PM" }
            }
        };
        
        var json = JsonSerializer.Serialize(weeklyLinesJson, new JsonSerializerOptions { WriteIndented = true });
        await jsonBlobClient.UploadAsync(BinaryData.FromString(json), overwrite: true);

        Console.WriteLine($"Successfully uploaded Week {week} lines");
    }

    /// <summary>
    /// Wait for a service to become available
    /// </summary>
    private async Task WaitForServiceAsync(string url, TimeSpan timeout)
    {
        using var client = new HttpClient();
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Service is responding (even 404 means it's running)
                    return;
                }
            }
            catch
            {
                // Service not ready yet
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Service at {url} did not become available within {timeout.TotalSeconds} seconds");
    }

    public void Dispose()
    {
        if (_disposed) return;

        Console.WriteLine("Shutting down test environment...");

        _webProcess?.Kill(true);
        _webProcess?.Dispose();

        _functionsProcess?.Kill(true);
        _functionsProcess?.Dispose();

        _azuriteProcess?.Kill(true);
        _azuriteProcess?.Dispose();

        _disposed = true;
        Console.WriteLine("Test environment shut down");
    }
}
