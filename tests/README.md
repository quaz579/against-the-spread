# Against The Spread - Playwright E2E Tests

This directory contains end-to-end tests for the Against The Spread application using Playwright and TypeScript.

## Overview

The E2E tests validate the complete user flow:
1. **Data Setup**: Uploads test data (Week 11 and Week 12) to Azurite blob storage
2. **User Flow**: Simulates a user making picks for both weeks
3. **Validation**: Downloads and validates the generated Excel files

## Architecture

The test architecture follows a **separation of concerns** approach:
- **Service Management**: Services are started/stopped using the repository's `start-local.sh` and `stop-local.sh` scripts
- **Test Execution**: Playwright tests focus only on browser automation and validation
- **No Global Setup Complexity**: Tests assume services are already running

## Prerequisites

- **Node.js** (v18 or later)
- **npm** (comes with Node.js)
- **.NET 8 SDK**
- **Azure Functions Core Tools** (v4)
- **Azurite** (installed via npm globally or as part of the project)

## Installation

1. Install dependencies:
   ```bash
   cd tests
   npm install
   ```

2. Install Playwright browsers:
   ```bash
   npx playwright install chromium
   ```

## Running Tests

### Step 1: Start Services

From the **repository root**, start all services:
```bash
./start-local.sh
```

Wait for all services to be ready:
- Azurite (Storage): http://localhost:10000
- Azure Functions: http://localhost:7071
- Web App: http://localhost:5158

### Step 2: Run Tests

From the **tests** directory:
```bash
# Run all tests
npm test

# Run with visible browser
npm run test:headed

# Debug tests interactively
npm run test:debug

# Run with Playwright UI mode
npm run test:ui

# View test report
npm run test:report
```

### Step 3: Stop Services

From the **repository root**:
```bash
./stop-local.sh
```

## Project Structure

```
tests/
├── helpers/
│   ├── download-helper.ts    # Download handling utilities
│   ├── excel-validator.ts    # Excel file validation
│   ├── index.ts              # Helper exports
│   └── test-environment.ts   # Test data upload to Azurite
├── pages/
│   ├── admin-page.ts         # Admin page object model
│   └── picks-page.ts         # Picks page object model
├── specs/
│   └── full-flow.spec.ts     # Main E2E test specification
├── package.json              # Dependencies and scripts
├── playwright.config.ts      # Playwright configuration
├── tsconfig.json             # TypeScript configuration
└── README.md                 # This file
```

## Test Flow

Each test follows this flow:

1. **Setup**: Upload test data to Azurite (Week 11 and Week 12 lines)
2. **Navigate** to the picks page
3. **Enter** user name
4. **Select** year (2025) and week (11 or 12)
5. **Click** Continue to load games
6. **Select** 6 games by clicking team buttons
7. **Download** the Excel file
8. **Validate** Excel structure:
   - Row 1: Empty
   - Row 2: Empty
   - Row 3: Headers (Name, Pick 1-6)
   - Row 4: Data (user name and 6 picks)

## Configuration

The tests are configured in `playwright.config.ts`:

| Setting | Value | Description |
|---------|-------|-------------|
| Base URL | `http://localhost:5158` | Web app URL |
| Browser | Chromium | Single browser for simplicity |
| Retries | 2 (CI), 0 (local) | Auto-retry on CI |
| Workers | 1 | Sequential test execution |
| Traces | On first retry | Debug information |
| Screenshots | On failure | Capture failures |
| Video | On failure | Record failures |

## CI/CD Integration

The tests run automatically on pull requests via GitHub Actions:

- Workflow file: `.github/workflows/e2e-tests.yml`
- Installs all dependencies (Azure Functions Core Tools, Azurite, Playwright)
- Starts services using `start-local.sh`
- Runs tests in CI environment
- Uploads test results and traces as artifacts

## Troubleshooting

### Services not starting

1. Check if ports are already in use:
   ```bash
   lsof -i :7071  # Functions
   lsof -i :5158  # Web
   lsof -i :10000 # Azurite
   ```

2. Kill existing processes:
   ```bash
   ./stop-local.sh
   ```

3. Rebuild if needed:
   ```bash
   dotnet build
   ```

### Tests timing out

1. Increase timeouts in `playwright.config.ts`:
   ```typescript
   actionTimeout: 30000,
   navigationTimeout: 60000,
   ```

2. Verify services are responding:
   ```bash
   curl http://localhost:7071/api/weeks?year=2025
   curl http://localhost:5158
   ```

### Excel validation fails

1. Check downloaded file exists in `/tmp/playwright-downloads/`
2. Verify the Excel structure matches expected format
3. Review test output for specific validation errors

### No weeks available

1. Ensure test data was uploaded to Azurite
2. Check Azurite is running and accessible
3. Verify the Functions can connect to Azurite

## Development

### Adding New Tests

1. Create a new file in `specs/` directory
2. Import test utilities from `@playwright/test`
3. Use page objects from `pages/` directory
4. Follow existing test patterns

### Modifying Page Objects

Edit files in `pages/` directory to:
- Add new locators for UI elements
- Create new interaction methods
- Update selectors if UI changes

### Custom Excel Validation

Edit `helpers/excel-validator.ts` to add custom validation logic.

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Playwright TypeScript Guide](https://playwright.dev/docs/test-typescript)
- [Against The Spread Repository](https://github.com/quaz579/against-the-spread)
