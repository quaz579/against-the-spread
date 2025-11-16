using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using OfficeOpenXml;

namespace AgainstTheSpread.SmokeTests;

/// <summary>
/// End-to-end smoke tests for the Against The Spread application
/// </summary>
[TestFixture]
public class SmokeTests : PageTest
{
    private TestEnvironment? _testEnv;
    private static bool _environmentStarted;
    private static readonly object _lock = new();
    private static TestEnvironment? _sharedTestEnv;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Set EPPlus license context
        #pragma warning disable CS0618
        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        #pragma warning restore CS0618

        // Start the environment once for all tests
        lock (_lock)
        {
            if (!_environmentStarted)
            {
                var workingDir = GetRepositoryRoot();
                _sharedTestEnv = new TestEnvironment(workingDir);
                
                // Start services synchronously in OneTimeSetUp
                Task.Run(async () =>
                {
                    await _sharedTestEnv.StartAzuriteAsync();
                    await _sharedTestEnv.StartFunctionsAsync();
                    await _sharedTestEnv.StartWebAppAsync();
                    
                    // Upload test data
                    var referenceDocsPath = Path.Combine(workingDir, "reference-docs");
                    await _sharedTestEnv.UploadLinesFileAsync(
                        Path.Combine(referenceDocsPath, "Week 11 Lines.xlsx"), 11, 2025);
                    await _sharedTestEnv.UploadLinesFileAsync(
                        Path.Combine(referenceDocsPath, "Week 12 Lines.xlsx"), 12, 2025);
                }).GetAwaiter().GetResult();

                _environmentStarted = true;
            }
        }

        _testEnv = _sharedTestEnv;
        await Task.CompletedTask;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        lock (_lock)
        {
            if (_environmentStarted && _sharedTestEnv != null)
            {
                _sharedTestEnv.Dispose();
                _sharedTestEnv = null;
                _environmentStarted = false;
            }
        }
    }

    [Test, Order(1)]
    public async Task SmokeTest_Week11_CompleteFlow()
    {
        Assert.That(_testEnv, Is.Not.Null, "Test environment should be initialized");

        // Navigate to the application
        await Page.GotoAsync(_testEnv!.WebUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to picks page (assuming there's a "Make Your Picks" button or link)
        var picksLinkLocator = Page.Locator("text=/make.*picks/i").Or(Page.Locator("a[href='/picks']"));
        if (await picksLinkLocator.CountAsync() > 0)
        {
            await picksLinkLocator.First.ClickAsync();
        }
        else
        {
            // Directly navigate if no link found
            await Page.GotoAsync($"{_testEnv!.WebUrl}/picks");
        }
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Enter name
        var nameInput = Page.Locator("input#userName").Or(Page.Locator("input[placeholder*='name' i]"));
        await nameInput.FillAsync("Test User");

        // Select year 2025
        var yearSelect = Page.Locator("select#year");
        await yearSelect.SelectOptionAsync("2025");
        await Page.WaitForTimeoutAsync(1000); // Wait for weeks to load

        // Select Week 11
        var weekSelect = Page.Locator("select#week");
        await weekSelect.SelectOptionAsync("11");

        // Click Continue
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Picks" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000); // Wait for games to load

        // Select 6 games by clicking on team buttons
        // The buttons contain team names and checkmarks
        var gameButtons = Page.Locator("button.btn").Filter(new() { HasNot = Page.Locator("text=Back") });
        var count = await gameButtons.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(6), "Should have at least 6 games available");

        // Click first 6 game buttons
        for (int i = 0; i < 6 && i < count; i++)
        {
            var button = gameButtons.Nth(i);
            // Check if button is enabled before clicking
            if (await button.IsEnabledAsync())
            {
                await button.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        // Wait for download button to appear
        await Page.WaitForSelectorAsync("button:has-text('Download')", new() { Timeout = 5000 });

        // Click download button
        var downloadButton = Page.Locator("button:has-text('Download')");
        
        // Start waiting for download before clicking
        var downloadTask = Page.WaitForDownloadAsync();
        await downloadButton.ClickAsync();
        var download = await downloadTask;

        // Save the download
        var downloadPath = Path.Combine(Path.GetTempPath(), $"week11_picks_{Guid.NewGuid()}.xlsx");
        await download.SaveAsAsync(downloadPath);

        // Verify the Excel file
        Assert.That(File.Exists(downloadPath), Is.True, "Excel file should be downloaded");
        ValidateExcelFile(downloadPath, "Test User", 6);

        // Cleanup
        File.Delete(downloadPath);
    }

    [Test, Order(2)]
    public async Task SmokeTest_Week12_CompleteFlow()
    {
        Assert.That(_testEnv, Is.Not.Null, "Test environment should be initialized");

        // Navigate to the application
        await Page.GotoAsync(_testEnv!.WebUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to picks page
        var picksLinkLocator = Page.Locator("text=/make.*picks/i").Or(Page.Locator("a[href='/picks']"));
        if (await picksLinkLocator.CountAsync() > 0)
        {
            await picksLinkLocator.First.ClickAsync();
        }
        else
        {
            await Page.GotoAsync($"{_testEnv!.WebUrl}/picks");
        }
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Enter name
        var nameInput = Page.Locator("input#userName").Or(Page.Locator("input[placeholder*='name' i]"));
        await nameInput.FillAsync("Test User Week 12");

        // Select year 2025
        var yearSelect = Page.Locator("select#year");
        await yearSelect.SelectOptionAsync("2025");
        await Page.WaitForTimeoutAsync(1000); // Wait for weeks to load

        // Select Week 12
        var weekSelect = Page.Locator("select#week");
        await weekSelect.SelectOptionAsync("12");

        // Click Continue
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Picks" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000); // Wait for games to load

        // Select 6 games by clicking on team buttons
        var gameButtons = Page.Locator("button.btn").Filter(new() { HasNot = Page.Locator("text=Back") });
        var count = await gameButtons.CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(6), "Should have at least 6 games available");

        // Click first 6 game buttons
        for (int i = 0; i < 6 && i < count; i++)
        {
            var button = gameButtons.Nth(i);
            if (await button.IsEnabledAsync())
            {
                await button.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        // Wait for download button to appear
        await Page.WaitForSelectorAsync("button:has-text('Download')", new() { Timeout = 5000 });

        // Click download button
        var downloadButton = Page.Locator("button:has-text('Download')");
        
        // Start waiting for download before clicking
        var downloadTask = Page.WaitForDownloadAsync();
        await downloadButton.ClickAsync();
        var download = await downloadTask;

        // Save the download
        var downloadPath = Path.Combine(Path.GetTempPath(), $"week12_picks_{Guid.NewGuid()}.xlsx");
        await download.SaveAsAsync(downloadPath);

        // Verify the Excel file
        Assert.That(File.Exists(downloadPath), Is.True, "Excel file should be downloaded");
        ValidateExcelFile(downloadPath, "Test User Week 12", 6);

        // Cleanup
        File.Delete(downloadPath);
    }

    /// <summary>
    /// Validate the structure and content of the generated Excel file
    /// </summary>
    private void ValidateExcelFile(string filePath, string expectedName, int expectedPickCount)
    {
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0];

        // Validate structure based on "Weekly Picks Example.xlsx"
        // Row 1: Empty
        Assert.That(worksheet.Cells[1, 1].Value, Is.Null.Or.Empty, "Row 1 should be empty");

        // Row 2: Empty
        Assert.That(worksheet.Cells[2, 1].Value, Is.Null.Or.Empty, "Row 2 should be empty");

        // Row 3: Headers
        Assert.That(worksheet.Cells[3, 1].Value?.ToString(), Is.EqualTo("Name"), "A3 should be 'Name'");
        Assert.That(worksheet.Cells[3, 2].Value?.ToString(), Is.EqualTo("Pick 1"), "B3 should be 'Pick 1'");
        Assert.That(worksheet.Cells[3, 3].Value?.ToString(), Is.EqualTo("Pick 2"), "C3 should be 'Pick 2'");
        Assert.That(worksheet.Cells[3, 4].Value?.ToString(), Is.EqualTo("Pick 3"), "D3 should be 'Pick 3'");
        Assert.That(worksheet.Cells[3, 5].Value?.ToString(), Is.EqualTo("Pick 4"), "E3 should be 'Pick 4'");
        Assert.That(worksheet.Cells[3, 6].Value?.ToString(), Is.EqualTo("Pick 5"), "F3 should be 'Pick 5'");
        Assert.That(worksheet.Cells[3, 7].Value?.ToString(), Is.EqualTo("Pick 6"), "G3 should be 'Pick 6'");

        // Row 4: Data
        Assert.That(worksheet.Cells[4, 1].Value?.ToString(), Is.EqualTo(expectedName), "A4 should contain the user name");
        
        // Verify all picks are present
        for (int i = 2; i <= expectedPickCount + 1; i++)
        {
            Assert.That(worksheet.Cells[4, i].Value, Is.Not.Null.And.Not.Empty, 
                $"Cell at row 4, column {i} should contain a pick");
        }
    }

    /// <summary>
    /// Find the repository root directory
    /// </summary>
    private string GetRepositoryRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "AgainstTheSpread.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        if (currentDir == null)
        {
            throw new Exception("Could not find repository root");
        }

        return currentDir;
    }
}
