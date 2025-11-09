# Implementation Plan - Against The Spread

## Overview

This document provides a detailed, step-by-step implementation plan for building the Against The Spread PWA. Each phase includes specific tasks, testing requirements, and validation checkpoints.

**Estimated Total Time**: 40-50 hours over 2-3 weeks

---

## Prerequisites

### Required Tools
- [ ] .NET 8 SDK installed
- [ ] Azure CLI installed
- [ ] Terraform installed (v1.5+)
- [ ] Azure Functions Core Tools installed
- [ ] Node.js (for PWA tooling)
- [ ] Git
- [ ] Visual Studio Code or Visual Studio 2022
- [ ] Azure subscription (free tier is sufficient)

### Setup Verification
```bash
dotnet --version        # Should be 8.0+
az --version           # Should be 2.50+
terraform --version    # Should be 1.5+
func --version         # Should be 4.0+
node --version         # Should be 18+
```

---

## Phase 0: Project Initialization (2-3 hours)

### 0.1: Create Solution Structure
**Goal**: Set up the monorepo with all projects

**Tasks**:
1. Create solution file
2. Create project folders
3. Initialize git repository
4. Create .gitignore

**Commands**:
```bash
# Navigate to project root
cd /Users/Ben.Grossman/Code/against-the-spread

# Create solution
dotnet new sln -n AgainstTheSpread

# Create projects
mkdir -p src
cd src

# Core library
dotnet new classlib -n AgainstTheSpread.Core -f net8.0
dotnet sln ../AgainstTheSpread.sln add AgainstTheSpread.Core/AgainstTheSpread.Core.csproj

# Azure Functions
dotnet new azurefunc -n AgainstTheSpread.Functions -f net8.0
dotnet sln ../AgainstTheSpread.sln add AgainstTheSpread.Functions/AgainstTheSpread.Functions.csproj

# Blazor WASM PWA
dotnet new blazorwasm -n AgainstTheSpread.Web -f net8.0 --pwa
dotnet sln ../AgainstTheSpread.sln add AgainstTheSpread.Web/AgainstTheSpread.Web.csproj

# Test project
dotnet new xunit -n AgainstTheSpread.Tests -f net8.0
dotnet sln ../AgainstTheSpread.sln add AgainstTheSpread.Tests/AgainstTheSpread.Tests.csproj

# Add project references
cd AgainstTheSpread.Functions
dotnet add reference ../AgainstTheSpread.Core/AgainstTheSpread.Core.csproj

cd ../AgainstTheSpread.Web
dotnet add reference ../AgainstTheSpread.Core/AgainstTheSpread.Core.csproj

cd ../AgainstTheSpread.Tests
dotnet add reference ../AgainstTheSpread.Core/AgainstTheSpread.Core.csproj
dotnet add reference ../AgainstTheSpread.Functions/AgainstTheSpread.Functions.csproj

cd ../..
```

**Checkpoint**:
```bash
# Build entire solution
dotnet build

# Verify all projects compile
dotnet test --no-build
```

✅ **Success Criteria**: Solution builds without errors, tests run (even if empty)

### 0.2: Add NuGet Dependencies
**Goal**: Install all required packages

**Core Library Packages**:
```bash
cd src/AgainstTheSpread.Core
dotnet add package EPPlus --version 7.0.0
dotnet add package Azure.Storage.Blobs --version 12.19.0
```

**Functions Packages**:
```bash
cd ../AgainstTheSpread.Functions
dotnet add package Microsoft.Azure.Functions.Worker --version 1.21.0
dotnet add package Microsoft.Azure.Functions.Worker.Sdk --version 1.17.0
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http --version 3.1.0
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs --version 6.2.0
dotnet add package EPPlus --version 7.0.0
```

**Web Packages**:
```bash
cd ../AgainstTheSpread.Web
dotnet add package Microsoft.AspNetCore.Components.WebAssembly --version 8.0.0
```

**Test Packages**:
```bash
cd ../AgainstTheSpread.Tests
dotnet add package Moq --version 4.20.70
dotnet add package FluentAssertions --version 6.12.0
dotnet add package bUnit --version 1.28.9
dotnet add package Microsoft.NET.Test.Sdk --version 17.9.0
```

**Checkpoint**:
```bash
cd /Users/Ben.Grossman/Code/against-the-spread
dotnet restore
dotnet build
```

✅ **Success Criteria**: All packages restore and build successfully

### 0.3: Create Directory Structure
**Goal**: Set up infrastructure and documentation folders

```bash
cd /Users/Ben.Grossman/Code/against-the-spread

# Infrastructure
mkdir -p infrastructure/terraform/environments
mkdir -p infrastructure/scripts

# GitHub Actions
mkdir -p .github/workflows

# Documentation (already exists, but ensure structure)
mkdir -p docs
```

**Checkpoint**:
```bash
ls -la
tree -L 2 -I 'bin|obj|node_modules'
```

✅ **Success Criteria**: All folders created, clean directory structure

### 0.4: Create README and Documentation Stubs
**Goal**: Document the project setup

**Create README.md** (in root):
- Project description
- Prerequisites
- Quick start guide
- Development commands
- Contributing guidelines

**Checkpoint**:
```bash
git init
git add .
git commit -m "Initial project structure and configuration"
```

✅ **Success Criteria**: Initial commit made, clean git status

---

## Phase 1: Core Library - Data Models (3-4 hours)

### 1.1: Define Data Models
**Goal**: Create all domain models with proper validation

**Create**: `src/AgainstTheSpread.Core/Models/Game.cs`
```csharp
namespace AgainstTheSpread.Core.Models;

public class Game
{
    public string Favorite { get; set; } = string.Empty;
    public decimal Line { get; set; }
    public string VsAt { get; set; } = string.Empty;
    public string Underdog { get; set; } = string.Empty;
    public DateTime GameDate { get; set; }
    
    // Helper property to get the display text
    public string FavoriteDisplay => $"{Favorite} {Line}";
    public string UnderdogDisplay => Underdog;
}
```

**Create**: `src/AgainstTheSpread.Core/Models/WeeklyLines.cs`
```csharp
namespace AgainstTheSpread.Core.Models;

public class WeeklyLines
{
    public int Week { get; set; }
    public List<Game> Games { get; set; } = new();
    public DateTime UploadedAt { get; set; }
}
```

**Create**: `src/AgainstTheSpread.Core/Models/UserPicks.cs`
```csharp
namespace AgainstTheSpread.Core.Models;

public class UserPicks
{
    public string Name { get; set; } = string.Empty;
    public int Week { get; set; }
    public List<string> Picks { get; set; } = new(); // Team names (6 required)
    public DateTime SubmittedAt { get; set; }
    
    public bool IsValid() => Picks.Count == 6 && !string.IsNullOrWhiteSpace(Name);
}
```

**Checkpoint**:
```bash
dotnet build src/AgainstTheSpread.Core
```

✅ **Success Criteria**: Models compile without errors

### 1.2: Write Model Tests
**Goal**: Test model validation and behavior

**Create**: `src/AgainstTheSpread.Tests/Models/UserPicksTests.cs`
```csharp
using AgainstTheSpread.Core.Models;
using FluentAssertions;
using Xunit;

namespace AgainstTheSpread.Tests.Models;

public class UserPicksTests
{
    [Fact]
    public void IsValid_WithSixPicks_ReturnsTrue()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "Test User",
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithFewerThanSixPicks_ReturnsFalse()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "Test User",
            Picks = new List<string> { "Team1", "Team2" }
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyName_ReturnsFalse()
    {
        // Arrange
        var picks = new UserPicks
        {
            Name = "",
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" }
        };

        // Act
        var result = picks.IsValid();

        // Assert
        result.Should().BeFalse();
    }
}
```

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~UserPicksTests"
```

✅ **Success Criteria**: All 3 tests pass

### 1.3: Create Interfaces for Services
**Goal**: Define contracts for Excel and Storage operations

**Create**: `src/AgainstTheSpread.Core/Interfaces/IExcelService.cs`
```csharp
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

public interface IExcelService
{
    WeeklyLines ParseLinesFromExcel(Stream excelStream);
    Stream GeneratePicksExcel(UserPicks picks);
}
```

**Create**: `src/AgainstTheSpread.Core/Interfaces/IStorageService.cs`
```csharp
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadLinesAsync(int week, Stream excelStream, CancellationToken cancellationToken = default);
    Task<WeeklyLines?> GetLinesAsync(int week, CancellationToken cancellationToken = default);
    Task<List<int>> GetAvailableWeeksAsync(CancellationToken cancellationToken = default);
}
```

**Checkpoint**:
```bash
dotnet build src/AgainstTheSpread.Core
```

✅ **Success Criteria**: Interfaces compile successfully

---

## Phase 2: Core Library - Excel Processing (5-6 hours)

### 2.1: Implement Excel Parsing
**Goal**: Parse uploaded Excel files into WeeklyLines model

**Create**: `src/AgainstTheSpread.Core/Services/ExcelService.cs`

**Key Implementation Points**:
- Use EPPlus to read Excel
- Handle the specific format from Week X Lines.xlsx
- Parse dates correctly
- Handle empty rows
- Extract week number from "WEEK X" cell

**Test Data**: Use `reference-docs/Week 1 Lines.csv` for expected structure

**Write Tests First**: `src/AgainstTheSpread.Tests/Services/ExcelServiceTests.cs`
```csharp
using AgainstTheSpread.Core.Services;
using FluentAssertions;
using Xunit;

namespace AgainstTheSpread.Tests.Services;

public class ExcelServiceTests
{
    private readonly ExcelService _sut;

    public ExcelServiceTests()
    {
        _sut = new ExcelService();
    }

    [Fact]
    public void ParseLinesFromExcel_WithValidFile_ReturnsWeeklyLines()
    {
        // Arrange
        var filePath = "../../../../reference-docs/Week 1 Lines.xlsx";
        using var stream = File.OpenRead(filePath);

        // Act
        var result = _sut.ParseLinesFromExcel(stream);

        // Assert
        result.Should().NotBeNull();
        result.Week.Should().Be(1);
        result.Games.Should().NotBeEmpty();
        result.Games.Should().Contain(g => g.Favorite == "Boise State" && g.Line == -10);
    }

    [Fact]
    public void ParseLinesFromExcel_ParsesAllGames_Correctly()
    {
        // Arrange
        var filePath = "../../../../reference-docs/Week 1 Lines.xlsx";
        using var stream = File.OpenRead(filePath);

        // Act
        var result = _sut.ParseLinesFromExcel(stream);

        // Assert
        result.Games.Count.Should().BeGreaterThan(50); // Week 1 has many games
        result.Games.Should().OnlyContain(g => !string.IsNullOrEmpty(g.Favorite));
        result.Games.Should().OnlyContain(g => !string.IsNullOrEmpty(g.Underdog));
    }
}
```

**Implementation Steps**:
1. Write the failing tests
2. Implement basic parsing logic
3. Run tests and fix issues
4. Handle edge cases (empty rows, date parsing)
5. Refine until all tests pass

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~ExcelServiceTests"
```

✅ **Success Criteria**: All parsing tests pass with real Excel files

### 2.2: Implement Excel Generation
**Goal**: Generate picks Excel in the correct format

**Reference**: Use `reference-docs/Weekly Picks Example.xlsx` for output format

**Write Tests First**: Add to `ExcelServiceTests.cs`
```csharp
[Fact]
public void GeneratePicksExcel_CreatesValidExcel()
{
    // Arrange
    var picks = new UserPicks
    {
        Name = "Test User",
        Week = 1,
        Picks = new List<string> 
        { 
            "Notre Dame", 
            "Akron", 
            "Michigan", 
            "Alabama", 
            "Clemson", 
            "FSU" 
        }
    };

    // Act
    var stream = _sut.GeneratePicksExcel(picks);

    // Assert
    stream.Should().NotBeNull();
    stream.Length.Should().BeGreaterThan(0);
    
    // Verify content by reading back
    stream.Position = 0;
    using var package = new ExcelPackage(stream);
    var worksheet = package.Workbook.Worksheets[0];
    
    worksheet.Cells["A3"].Value.Should().Be("Test User");
    worksheet.Cells["B3"].Value.Should().Be("Notre Dame");
    worksheet.Cells["G3"].Value.Should().Be("FSU");
}
```

**Implementation Steps**:
1. Write failing test
2. Create Excel with EPPlus
3. Set up headers (Name, Pick 1-6)
4. Populate user data
5. Format cells appropriately
6. Return as MemoryStream

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~ExcelServiceTests"
```

✅ **Success Criteria**: Generated Excel matches expected format, all tests pass

### 2.3: Integration Test with Real Files
**Goal**: End-to-end test of parsing and generation

**Create**: `src/AgainstTheSpread.Tests/Integration/ExcelRoundTripTests.cs`
```csharp
[Fact]
public async Task FullWorkflow_ParseLinesAndGeneratePicks_Works()
{
    // Arrange
    var excelService = new ExcelService();
    
    // Act - Parse lines
    using var linesStream = File.OpenRead("../../../../reference-docs/Week 1 Lines.xlsx");
    var lines = excelService.ParseLinesFromExcel(linesStream);
    
    // Pick 6 games from parsed lines
    var selectedGames = lines.Games.Take(6).Select(g => g.Favorite).ToList();
    var picks = new UserPicks
    {
        Name = "Integration Test User",
        Week = lines.Week,
        Picks = selectedGames
    };
    
    // Act - Generate picks
    var picksStream = excelService.GeneratePicksExcel(picks);
    
    // Assert
    picksStream.Should().NotBeNull();
    picksStream.Length.Should().BeGreaterThan(0);
    
    // Optionally save to disk for manual inspection
    var outputPath = "../../../../test-output/generated-picks.xlsx";
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    using var fileStream = File.Create(outputPath);
    picksStream.Position = 0;
    await picksStream.CopyToAsync(fileStream);
}
```

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~ExcelRoundTripTests"
```

✅ **Success Criteria**: Full workflow test passes, generated file can be opened in Excel

---

## Phase 3: Core Library - Storage Service (3-4 hours)

### 3.1: Implement Blob Storage Service
**Goal**: Create service to interact with Azure Blob Storage

**Create**: `src/AgainstTheSpread.Core/Services/BlobStorageService.cs`

**Write Tests First**: `src/AgainstTheSpread.Tests/Services/BlobStorageServiceTests.cs`
```csharp
using Azure.Storage.Blobs;
using Moq;
using FluentAssertions;
using Xunit;

namespace AgainstTheSpread.Tests.Services;

public class BlobStorageServiceTests
{
    // Use Moq to mock BlobServiceClient
    // Test upload, download, list operations
    
    [Fact]
    public async Task UploadLinesAsync_StoresFileInBlob()
    {
        // Arrange
        // Mock BlobServiceClient and BlobContainerClient
        
        // Act
        
        // Assert
    }
}
```

**Implementation Steps**:
1. Create constructor accepting BlobServiceClient
2. Implement UploadLinesAsync (stores Excel + JSON)
3. Implement GetLinesAsync (retrieves and deserializes JSON)
4. Implement GetAvailableWeeksAsync (lists blobs)
5. Add error handling and logging

**Storage Structure**:
```
Container: gamefiles
├── lines/
│   ├── week-1.xlsx
│   ├── week-1.json
│   ├── week-2.xlsx
│   └── week-2.json
```

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~BlobStorageServiceTests"
```

✅ **Success Criteria**: All storage tests pass with mocked dependencies

### 3.2: Local Testing with Azurite
**Goal**: Test storage service with local Azure Storage Emulator

**Setup Azurite**:
```bash
# Install Azurite globally
npm install -g azurite

# Start Azurite
azurite --silent --location /tmp/azurite --debug /tmp/azurite/debug.log
```

**Create Integration Test**: `src/AgainstTheSpread.Tests/Integration/BlobStorageIntegrationTests.cs`
```csharp
public class BlobStorageIntegrationTests : IDisposable
{
    private readonly BlobServiceClient _blobClient;
    private readonly BlobStorageService _sut;
    
    public BlobStorageIntegrationTests()
    {
        // Connect to Azurite
        var connectionString = "UseDevelopmentStorage=true";
        _blobClient = new BlobServiceClient(connectionString);
        _sut = new BlobStorageService(_blobClient);
    }
    
    [Fact]
    public async Task UploadAndRetrieveLines_Works()
    {
        // Test with real Azurite
    }
    
    public void Dispose()
    {
        // Cleanup test containers
    }
}
```

**Checkpoint**:
```bash
# Start Azurite
azurite &

# Run integration tests
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~BlobStorageIntegrationTests"
```

✅ **Success Criteria**: Integration tests pass against Azurite

---

## Phase 4: Azure Functions API (6-8 hours)

### 4.1: Setup Function App Project
**Goal**: Configure Azure Functions with proper DI and settings

**Update**: `src/AgainstTheSpread.Functions/Program.cs`
```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using Azure.Storage.Blobs;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Register services
        services.AddScoped<IExcelService, ExcelService>();
        
        // Register Blob Storage
        services.AddSingleton(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });
        
        services.AddScoped<IStorageService, BlobStorageService>();
    })
    .Build();

host.Run();
```

**Create**: `src/AgainstTheSpread.Functions/local.settings.json`
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "Host": {
    "CORS": "*"
  }
}
```

**Checkpoint**:
```bash
cd src/AgainstTheSpread.Functions
func start
```

✅ **Success Criteria**: Function app starts without errors

### 4.2: Implement Admin Endpoints
**Goal**: Create endpoints for uploading and managing lines

**Create**: `src/AgainstTheSpread.Functions/Functions/AdminFunctions.cs`

**Endpoints**:
- `POST /api/admin/lines/{week}` - Upload lines
- `GET /api/admin/lines` - List all weeks

**Write Tests First**: `src/AgainstTheSpread.Tests/Functions/AdminFunctionsTests.cs`
```csharp
using Moq;
using FluentAssertions;
using Xunit;
using Microsoft.Azure.Functions.Worker.Http;

namespace AgainstTheSpread.Tests.Functions;

public class AdminFunctionsTests
{
    private readonly Mock<IStorageService> _mockStorage;
    private readonly Mock<IExcelService> _mockExcel;
    private readonly AdminFunctions _sut;
    
    public AdminFunctionsTests()
    {
        _mockStorage = new Mock<IStorageService>();
        _mockExcel = new Mock<IExcelService>();
        _sut = new AdminFunctions(_mockStorage.Object, _mockExcel.Object);
    }
    
    [Fact]
    public async Task UploadLines_WithValidExcel_ReturnsSuccess()
    {
        // Arrange
        // Mock request with Excel file
        
        // Act
        var response = await _sut.UploadLines(mockRequest, 1);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Implementation Steps**:
1. Write failing tests
2. Implement UploadLines function
3. Add validation (file type, size)
4. Parse Excel and store in Blob
5. Return appropriate responses
6. Add error handling

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~AdminFunctionsTests"

# Test locally
curl -X POST http://localhost:7071/api/admin/lines/1 \
  -F "file=@reference-docs/Week 1 Lines.xlsx"
```

✅ **Success Criteria**: Tests pass, can upload via cURL

### 4.3: Implement User Endpoints
**Goal**: Create endpoints for getting lines and submitting picks

**Create**: `src/AgainstTheSpread.Functions/Functions/UserFunctions.cs`

**Endpoints**:
- `GET /api/lines/{week}` - Get games for week
- `GET /api/lines/current` - Get current week's games
- `POST /api/picks` - Submit picks and get Excel

**Write Tests First**: `src/AgainstTheSpread.Tests/Functions/UserFunctionsTests.cs`

**Implementation Steps**:
1. Write failing tests
2. Implement GetLines function
3. Implement SubmitPicks function (validate 6 picks)
4. Generate Excel on-the-fly
5. Return file as downloadable response

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~UserFunctionsTests"

# Test locally
curl http://localhost:7071/api/lines/1

curl -X POST http://localhost:7071/api/picks \
  -H "Content-Type: application/json" \
  -d '{"name":"Test User","week":1,"picks":["Team1","Team2","Team3","Team4","Team5","Team6"]}' \
  --output picks.xlsx
```

✅ **Success Criteria**: All tests pass, can retrieve lines and download picks via cURL

### 4.4: Add CORS and Error Handling
**Goal**: Configure CORS for web app and add global error handling

**Update**: `src/AgainstTheSpread.Functions/host.json`
```json
{
  "version": "2.0",
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  },
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  }
}
```

**Checkpoint**:
```bash
func start
# Test CORS from browser console
```

✅ **Success Criteria**: CORS works, errors return proper status codes

---

## Phase 5: Blazor Web App (8-10 hours)

### 5.1: Setup Blazor Project Structure
**Goal**: Organize Blazor app with pages and components

**Folder Structure**:
```
src/AgainstTheSpread.Web/
├── Pages/
│   ├── Index.razor
│   ├── Admin.razor
│   └── PickGames.razor
├── Components/
│   ├── GameCard.razor
│   └── GameList.razor
├── Services/
│   ├── ApiService.cs
│   └── IApiService.cs
└── wwwroot/
    ├── manifest.json
    ├── icon-192.png
    ├── icon-512.png
    └── service-worker.js
```

**Update**: `src/AgainstTheSpread.Web/Program.cs`
```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AgainstTheSpread.Web;
using AgainstTheSpread.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base URL
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddScoped<IApiService, ApiService>();

await builder.Build().RunAsync();
```

**Checkpoint**:
```bash
cd src/AgainstTheSpread.Web
dotnet run
```

✅ **Success Criteria**: Blazor app runs, shows default template

### 5.2: Create API Service Layer
**Goal**: Create service to communicate with Azure Functions

**Create**: `src/AgainstTheSpread.Web/Services/IApiService.cs`
```csharp
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Web.Services;

public interface IApiService
{
    Task<WeeklyLines?> GetLinesAsync(int week);
    Task<List<int>> GetAvailableWeeksAsync();
    Task<bool> UploadLinesAsync(int week, Stream fileStream, string fileName);
    Task<byte[]> SubmitPicksAsync(UserPicks picks);
}
```

**Create**: `src/AgainstTheSpread.Web/Services/ApiService.cs`

**Implementation Steps**:
1. Implement each method using HttpClient
2. Handle errors gracefully
3. Add loading states
4. Serialize/deserialize JSON

**Write Component Tests**: `src/AgainstTheSpread.Tests/Web/ApiServiceTests.cs`

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~ApiServiceTests"
```

✅ **Success Criteria**: API service tests pass with mocked HttpClient

### 5.3: Build Admin Upload Page
**Goal**: Create UI for uploading weekly lines

**Create**: `src/AgainstTheSpread.Web/Pages/Admin.razor`

**Features**:
- File input for Excel upload
- Week number input
- Upload button with loading state
- Success/error messages
- List of uploaded weeks

**Write Component Tests**: `src/AgainstTheSpread.Tests/Web/AdminPageTests.cs`
```csharp
using Bunit;
using FluentAssertions;
using Xunit;
using AgainstTheSpread.Web.Pages;

public class AdminPageTests : TestContext
{
    [Fact]
    public void Admin_RendersFileUpload()
    {
        // Arrange
        var cut = RenderComponent<Admin>();
        
        // Assert
        cut.Find("input[type='file']").Should().NotBeNull();
    }
}
```

**Checkpoint**:
```bash
# Start Functions locally
cd src/AgainstTheSpread.Functions
func start &

# Start Web app
cd ../AgainstTheSpread.Web
dotnet run

# Test in browser: http://localhost:5000/admin
```

✅ **Success Criteria**: Can upload Excel file, see success message, file appears in Azurite

### 5.4: Build User Pick Games Page
**Goal**: Create mobile-friendly UI for selecting games

**Create**: `src/AgainstTheSpread.Web/Pages/PickGames.razor`

**Features**:
- Display list of games (Favorite vs Underdog with line)
- Touch-friendly selection (tap to select/deselect)
- Show count of selected games (X/6)
- Disable selecting more than 6
- Name input field
- Download button (disabled until 6 selected and name entered)

**Create**: `src/AgainstTheSpread.Web/Components/GameCard.razor`
- Display game info
- Visual indication of selection
- Mobile-optimized styling

**Write Component Tests**: `src/AgainstTheSpread.Tests/Web/PickGamesPageTests.cs`

**Checkpoint**:
```bash
dotnet test src/AgainstTheSpread.Tests --filter "FullyQualifiedName~PickGamesPageTests"

# Test in browser on desktop and mobile
```

✅ **Success Criteria**: Can select 6 games, download works, mobile-friendly

### 5.5: Implement Excel Download
**Goal**: Generate and download Excel file from browser

**Implementation**:
```csharp
private async Task DownloadPicks()
{
    var picks = new UserPicks
    {
        Name = userName,
        Week = currentWeek,
        Picks = selectedGames
    };
    
    var fileBytes = await ApiService.SubmitPicksAsync(picks);
    
    // Trigger download
    await JS.InvokeVoidAsync("downloadFile", 
        $"picks-week-{currentWeek}-{userName}.xlsx", 
        Convert.ToBase64String(fileBytes));
}
```

**Add JavaScript**: `src/AgainstTheSpread.Web/wwwroot/index.html`
```javascript
function downloadFile(filename, base64) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = 'data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,' + base64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
```

**Checkpoint**:
- Test download on desktop
- Test download on mobile Safari/Chrome

✅ **Success Criteria**: Excel file downloads and can be opened

### 5.6: Add PWA Features
**Goal**: Make app installable and work offline

**Update**: `src/AgainstTheSpread.Web/wwwroot/manifest.json`
```json
{
  "name": "Against The Spread",
  "short_name": "ATS",
  "description": "Weekly college football pick'em game",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#004080",
  "icons": [
    {
      "src": "icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ]
}
```

**Update Service Worker**: Configure caching strategies

**Test PWA**:
- Chrome DevTools > Application > Manifest
- Test install prompt
- Test offline functionality

**Checkpoint**:
```bash
# Build for production
dotnet publish -c Release

# Test with local server
cd bin/Release/net8.0/publish/wwwroot
python3 -m http.server 8080

# Test in browser: http://localhost:8080
# Try installing as PWA
```

✅ **Success Criteria**: App can be installed, works offline, shows in app drawer

---

## Phase 6: Infrastructure as Code (4-5 hours)

### 6.1: Create Terraform Configuration
**Goal**: Define all Azure resources in Terraform

**Create**: `infrastructure/terraform/main.tf`

**Resources to create**:
- Resource Group
- Storage Account (for Blob Storage)
- App Service Plan (Consumption)
- Function App
- Static Web App
- Application Insights

**Create**: `infrastructure/terraform/variables.tf`
```hcl
variable "project_name" {
  description = "Project name"
  type        = string
  default     = "against-the-spread"
}

variable "environment" {
  description = "Environment (dev/prod)"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus"
}
```

**Create**: `infrastructure/terraform/outputs.tf`
```hcl
output "function_app_name" {
  value = azurerm_function_app.main.name
}

output "static_web_app_url" {
  value = azurerm_static_site.main.default_hostname
}

output "storage_account_name" {
  value = azurerm_storage_account.main.name
}
```

**Create Environment Files**:
- `infrastructure/terraform/environments/dev.tfvars`
- `infrastructure/terraform/environments/prod.tfvars`

**Checkpoint**:
```bash
cd infrastructure/terraform
terraform init
terraform validate
terraform plan -var-file="environments/dev.tfvars"
```

✅ **Success Criteria**: Terraform plan runs without errors

### 6.2: Deploy Dev Environment
**Goal**: Create dev environment in Azure

**Create**: `infrastructure/scripts/deploy-dev.sh`
```bash
#!/bin/bash
set -e

cd infrastructure/terraform

echo "Deploying to DEV environment..."
terraform apply -var-file="environments/dev.tfvars" -auto-approve

echo "Getting outputs..."
terraform output -json > ../../terraform-outputs.json

echo "Dev environment deployed successfully!"
```

**Deploy**:
```bash
chmod +x infrastructure/scripts/deploy-dev.sh
./infrastructure/scripts/deploy-dev.sh
```

**Verify in Azure Portal**:
- Resource group exists
- Storage account has "gamefiles" container
- Function app is running
- Static web app is deployed

**Checkpoint**:
```bash
# Test function app
curl https://<function-app-name>.azurewebsites.net/api/lines/1

# Test static web app
open https://<static-web-app-url>
```

✅ **Success Criteria**: All resources created, accessible via Azure Portal

### 6.3: Configure Application Settings
**Goal**: Set environment variables in Azure

**Function App Settings**:
- `AzureWebJobsStorage` - Connection string
- `WEBSITE_RUN_FROM_PACKAGE` - 1

**Static Web App Settings**:
- `ApiBaseUrl` - Function app URL

**Update via Terraform or Azure CLI**:
```bash
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group <rg-name> \
  --settings "AzureWebJobsStorage=<connection-string>"
```

**Checkpoint**:
```bash
az functionapp config appsettings list \
  --name <function-app-name> \
  --resource-group <rg-name>
```

✅ **Success Criteria**: All settings configured correctly

---

## Phase 7: CI/CD Pipeline (4-5 hours)

### 7.1: Create Build and Test Workflow
**Goal**: Automate build and test on every PR

**Create**: `.github/workflows/build-test.yml`
```yaml
name: Build and Test

on:
  pull_request:
    branches: [ main ]
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --logger trx --results-directory "TestResults"
    
    - name: Publish Test Results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results
        path: TestResults/*.trx
```

**Checkpoint**:
- Commit and push
- Create a test PR
- Verify workflow runs successfully

✅ **Success Criteria**: Build and test workflow passes on PR

### 7.2: Create Deployment Workflow
**Goal**: Deploy to Azure on merge to main

**Create**: `.github/workflows/deploy.yml`
```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  deploy-infrastructure:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup Terraform
      uses: hashicorp/setup-terraform@v3
    
    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Terraform Init
      run: |
        cd infrastructure/terraform
        terraform init
    
    - name: Terraform Apply
      run: |
        cd infrastructure/terraform
        terraform apply -var-file="environments/dev.tfvars" -auto-approve

  deploy-functions:
    needs: deploy-infrastructure
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Build Functions
      run: |
        cd src/AgainstTheSpread.Functions
        dotnet publish -c Release -o ./output
    
    - name: Deploy to Azure Functions
      uses: Azure/functions-action@v1
      with:
        app-name: ${{ secrets.FUNCTION_APP_NAME }}
        package: src/AgainstTheSpread.Functions/output
        publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}

  deploy-web:
    needs: deploy-infrastructure
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Build Web App
      run: |
        cd src/AgainstTheSpread.Web
        dotnet publish -c Release -o ./output
    
    - name: Deploy to Static Web App
      uses: Azure/static-web-apps-deploy@v1
      with:
        azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
        repo_token: ${{ secrets.GITHUB_TOKEN }}
        action: "upload"
        app_location: "src/AgainstTheSpread.Web/output/wwwroot"
```

**Setup GitHub Secrets**:
```bash
# Get Azure credentials
az ad sp create-for-rbac \
  --name "against-the-spread-github" \
  --role contributor \
  --scopes /subscriptions/<subscription-id>/resourceGroups/<rg-name> \
  --sdk-auth

# Add to GitHub secrets:
# - AZURE_CREDENTIALS (JSON output from above)
# - AZURE_SUBSCRIPTION_ID
# - FUNCTION_APP_NAME
# - AZURE_FUNCTIONAPP_PUBLISH_PROFILE (download from Azure Portal)
# - AZURE_STATIC_WEB_APPS_API_TOKEN (get from Static Web App)
```

**Checkpoint**:
- Add all required secrets to GitHub
- Merge a test change to main
- Verify deployment workflow runs
- Check Azure Portal for updated resources

✅ **Success Criteria**: Deployment workflow succeeds, app is live in Azure

### 7.3: Add Code Coverage Reporting
**Goal**: Track test coverage over time

**Update**: `.github/workflows/build-test.yml`
```yaml
    - name: Test with Coverage
      run: dotnet test /p:CollectCoverage=true /p:CoverageReportsDir=./coverage /p:CoverageReportFormat=cobertura
    
    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        files: ./coverage/coverage.cobertura.xml
```

**Setup Codecov**:
- Sign up at codecov.io with GitHub account
- Enable repository
- Badge will be generated automatically

**Checkpoint**:
- Check Codecov dashboard for coverage reports
- Add badge to README.md

✅ **Success Criteria**: Coverage reports visible on Codecov

---

## Phase 8: Testing and Refinement (4-6 hours)

### 8.1: End-to-End Testing
**Goal**: Test complete user flows in dev environment

**Test Scenarios**:

1. **Admin Upload Flow**:
   - Navigate to admin page
   - Upload Week 1 Lines.xlsx
   - Verify success message
   - Check Blob Storage for files

2. **User Pick Flow (Desktop)**:
   - Navigate to pick games page
   - Verify games load
   - Select 6 games
   - Enter name
   - Download picks
   - Open Excel and verify format

3. **User Pick Flow (Mobile)**:
   - Open app on iPhone/Android
   - Install PWA
   - Complete pick flow
   - Download and open Excel

4. **Error Handling**:
   - Try uploading invalid file
   - Try selecting 7 games
   - Try submitting without name
   - Test with no internet (PWA offline)

**Create Test Checklist**: `docs/testing-checklist.md`

**Checkpoint**:
```bash
# Run all tests
dotnet test

# Manual testing in dev environment
```

✅ **Success Criteria**: All test scenarios pass on desktop and mobile

### 8.2: Performance Testing
**Goal**: Ensure app performs well under load

**Test Cases**:
- Large Excel file (100+ games)
- Multiple concurrent users
- Mobile on slow 3G connection

**Tools**:
- Chrome DevTools (Lighthouse)
- Azure Application Insights

**Optimize**:
- Reduce bundle size
- Add loading states
- Optimize images
- Enable compression

**Checkpoint**:
```bash
# Run Lighthouse audit
# Target: 90+ scores for Performance, Accessibility, PWA
```

✅ **Success Criteria**: Lighthouse scores 90+ across the board

### 8.3: Security Review
**Goal**: Ensure app follows security best practices

**Checklist**:
- [ ] HTTPS enforced
- [ ] CORS properly configured
- [ ] Input validation on all endpoints
- [ ] File upload size limits
- [ ] Rate limiting on APIs
- [ ] No sensitive data in client
- [ ] CSP headers configured

**Checkpoint**:
```bash
# Run security scan
npm install -g snyk
snyk test
```

✅ **Success Criteria**: No critical security issues

### 8.4: Documentation
**Goal**: Complete all documentation

**Update README.md**:
- Project description
- Features
- Tech stack
- Prerequisites
- Setup instructions
- Usage guide
- Contributing guidelines
- License

**Create Additional Docs**:
- `docs/architecture.md` - System architecture
- `docs/deployment.md` - Deployment guide
- `docs/development.md` - Development setup
- `docs/api.md` - API documentation

**Checkpoint**:
- Review all documentation
- Ask someone else to follow setup instructions

✅ **Success Criteria**: Documentation is complete and accurate

---

## Phase 9: Launch Preparation (2-3 hours)

### 9.1: Production Deployment
**Goal**: Deploy to production environment

**Create Production Terraform**:
```bash
cd infrastructure/terraform
terraform workspace new prod
terraform apply -var-file="environments/prod.tfvars"
```

**Update GitHub Workflow**:
- Add production deployment (manual trigger)
- Require approval for prod deployments

**Checkpoint**:
```bash
# Deploy to production
gh workflow run deploy.yml --ref main -f environment=prod
```

✅ **Success Criteria**: Production environment is live

### 9.2: User Acceptance Testing
**Goal**: Test with real users

**Invite Beta Users**:
- Share production URL
- Provide test data (sample Excel)
- Collect feedback

**Monitor**:
- Application Insights for errors
- User feedback
- Performance metrics

**Checkpoint**:
- At least 5 users complete full flow
- No critical issues reported

✅ **Success Criteria**: Beta users successfully use the app

### 9.3: Launch
**Goal**: Announce the app and go live

**Pre-Launch Checklist**:
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Monitoring configured
- [ ] Backup strategy in place
- [ ] Support plan defined

**Launch**:
- Announce to user group
- Share installation instructions
- Monitor for first 24 hours

**Post-Launch**:
- Gather feedback
- Track usage metrics
- Plan future enhancements

✅ **Success Criteria**: App is live and users are actively using it

---

## Maintenance and Future Enhancements

### Ongoing Tasks
- Monitor Application Insights weekly
- Review and merge PRs
- Update dependencies monthly
- Backup Blob Storage data
- Review and optimize costs

### Future Features (Post-MVP)
1. **User Authentication** (2-3 weeks)
   - Azure AD B2C integration
   - User profiles
   - Pick history

2. **Automated Scoring** (3-4 weeks)
   - Integrate with sports data API
   - Parse game results
   - Calculate winners automatically
   - Generate leaderboards

3. **Email Notifications** (1-2 weeks)
   - Remind users to submit picks
   - Announce weekly winners
   - Use Azure Communication Services

4. **Bowl Games & Playoffs** (2-3 weeks)
   - Confidence points system
   - Bracket management
   - Separate scoring logic

5. **Mobile Apps** (4-6 weeks)
   - Native iOS/Android with .NET MAUI
   - Push notifications
   - Offline support

6. **Admin Dashboard** (2-3 weeks)
   - View all submissions
   - Generate reports
   - Manage users
   - Update game results manually

7. **Social Features** (3-4 weeks)
   - Comments on picks
   - Trash talk board
   - Share picks on social media

---

## Success Metrics

### Technical Metrics
- Build success rate: 100%
- Test coverage: >80%
- API response time: <500ms p95
- Page load time: <2s
- Zero critical bugs in production

### Business Metrics
- User adoption: 50+ weekly users
- Mobile usage: 70%+ of picks submitted via mobile
- User satisfaction: 4.5/5 stars
- Cost: <$10/month

### Development Metrics
- Time to MVP: 40-50 hours
- Number of deployments: 20+
- PR review time: <24 hours
- Bug fix time: <48 hours

---

## Risk Mitigation

### Technical Risks
- **Excel parsing fails**: Extensive testing with real files, fallback to manual entry
- **Azure costs exceed budget**: Set up cost alerts, optimize resource usage
- **Performance issues**: Load testing, caching, CDN
- **Security breach**: Security review, penetration testing, monitoring

### Process Risks
- **Scope creep**: Strict MVP focus, feature requests go to backlog
- **Timeline delays**: Regular checkpoints, adjust scope if needed
- **Testing gaps**: Automated tests, manual testing checklist

---

## Conclusion

This implementation plan provides a comprehensive, step-by-step guide to building the Against The Spread PWA. Each phase includes:

- Clear goals and deliverables
- Test-driven development approach
- Frequent validation checkpoints
- Success criteria

By following this plan and using AI agent assistance, you'll build a robust, well-tested application that solves your immediate problem (mobile picks) while laying the foundation for future enhancements.

**Remember**: Commit frequently, test often, and don't skip the checkpoints. Good luck! 🏈
