# Against The Spread - Playwright E2E Tests

This directory contains end-to-end tests for the Against The Spread application using Playwright and TypeScript.

## Overview

The E2E tests validate the complete user flow:
1. **Admin Flow**: Upload weekly game lines via the admin interface
2. **User Flow**: Select week, make 6 game picks, and download Excel file
3. **Validation**: Verify Excel file structure and content matches expected format

## Prerequisites

- **Node.js** (v18 or later)
- **npm** (comes with Node.js)
- **.NET 8 SDK**
- **Azure Functions Core Tools** (v4)
- **Azurite** (Azure Storage Emulator)

### Validate Prerequisites

Run the validation script to check if all prerequisites are met:
```bash
./tests/validate-environment.sh
```

This will check:
- Node.js version (v18+)
- npm availability
- .NET 8 SDK
- Azure Functions Core Tools (v4)
- Azurite installation
- Port availability (10000, 7071, 5158)
- Test dependencies status

## Installation

1. Install test dependencies:
   ```bash
   cd tests
   npm install
   ```

2. Install Playwright browsers:
   ```bash
   npx playwright install chromium
   ```

## Starting Services

**IMPORTANT**: Services must be running before executing tests.

From the repository root, start all services:
```bash
./start-local.sh
```

This script starts:
- **Azurite** (port 10000) - Storage emulator
- **Azure Functions** (port 7071) - Backend API
- **Blazor Web App** (port 5158) - Frontend

Wait 10-15 seconds for services to fully initialize before running tests.

## Running Tests

Once services are running, execute tests from the `tests` directory:

### Run all tests
```bash
npm test
```

### Run with headed browser (visible UI)
```bash
npm run test:headed
```

### Debug tests interactively
```bash
npm run test:debug
```

### Run with UI mode (interactive)
```bash
npm run test:ui
```

### View test report
```bash
npm run test:report
```

## Stopping Services

After testing, stop all services from the repository root:
```bash
./stop-local.sh
```

## Project Structure

```
tests/
├── helpers/
│   ├── excel-validator.ts     # Validates Excel file structure
│   └── download-helper.ts     # Handles file downloads
├── pages/
│   ├── admin-page.ts          # Admin page object model
│   └── picks-page.ts          # Picks page object model
├── specs/
│   └── full-flow.spec.ts      # Complete user flow test
├── playwright.config.ts       # Playwright configuration
├── package.json               # Dependencies
├── tsconfig.json              # TypeScript configuration
└── README.md                  # This file
```

## Test Flow

The E2E test validates this complete flow:

1. **Admin uploads lines**: Navigate to `/admin` and upload Week 11 lines Excel file
2. **User enters name**: Navigate to `/picks` and enter user name
3. **User selects week**: Choose year 2025 and week 11
4. **User makes picks**: Select 6 games by clicking team buttons
5. **User downloads Excel**: Click download button to generate picks file
6. **Validate Excel structure**:
   - Row 1: Empty
   - Row 2: Empty
   - Row 3: Headers (Name, Pick 1, Pick 2, ..., Pick 6)
   - Row 4: Data (user name and 6 team picks)

## Configuration

Tests are configured in `playwright.config.ts`:

- **Base URL**: `http://localhost:5158`
- **Browser**: Chromium only
- **Retries**: 2 on CI, 0 locally
- **Workers**: 1 (sequential execution)
- **Traces**: On first retry
- **Screenshots**: On failure
- **Video**: On failure
- **No global setup**: Services must be started manually

## Key Features

### Page Object Models
- **AdminPage**: Handles admin file upload interface
- **PicksPage**: Manages user picks workflow

### Helper Functions
- **excel-validator.ts**: Validates Excel file structure against expected format
- **download-helper.ts**: Manages file downloads during tests

### No Automated Service Startup
Unlike previous versions, these tests do NOT automatically start services. This approach:
- Makes tests simpler and more reliable
- Allows debugging services independently
- Prevents port conflicts and timing issues
- Enables running tests multiple times without restarting services

## Troubleshooting

### Services Not Running
**Error**: "Web App is not running at http://localhost:5158"

**Solution**: Start services before running tests:
```bash
# From repository root
./start-local.sh
```

Wait 10-15 seconds for services to initialize, then run tests.

### Port Already In Use
**Error**: Port 7071, 5158, or 10000 already in use

**Solution**: Stop existing services:
```bash
./stop-local.sh
```

Or manually kill processes:
```bash
lsof -ti:7071 | xargs kill
lsof -ti:5158 | xargs kill
lsof -ti:10000 | xargs kill
```

### Tests Timeout Waiting for Games
**Error**: Tests fail waiting for game buttons to appear

**Solution**: 
- Verify Functions API is accessible: `curl http://localhost:7071/api/weeks?year=2025`
- Check that Week 11 data was uploaded successfully
- Increase timeout in `picks-page.ts` if needed

### Excel Validation Fails
**Error**: Excel structure doesn't match expected format

**Solution**:
- Check the downloaded file exists in `/tmp/playwright-downloads`
- Verify the file manually against `reference-docs/Weekly Picks Example.xlsx`
- Look at test output for specific validation errors

### Build Errors
**Error**: TypeScript compilation errors

**Solution**:
```bash
cd tests
npm install
```

Ensure all dependencies are installed correctly.

## CI/CD Integration

Tests run automatically on pull requests via GitHub Actions.

### Workflow Requirements
The CI workflow must:
1. Install dependencies (.NET 8, Azure Functions Core Tools, Azurite, Playwright)
2. Build the .NET solution
3. **Start services** using `./start-local.sh`
4. Wait for services to be ready
5. Run tests
6. **Stop services** using `./stop-local.sh`

See `.github/workflows/e2e-tests.yml` for the complete workflow configuration.

## Development

### Adding New Tests

1. Create a new spec file in `specs/` directory
2. Import page objects and helpers
3. Use `test.describe` and `test.step` to structure tests
4. Follow existing patterns in `full-flow.spec.ts`

Example:
```typescript
import { test, expect } from '@playwright/test';
import { PicksPage } from '../pages/picks-page';

test.describe('My New Test', () => {
  test('should do something', async ({ page }) => {
    const picksPage = new PicksPage(page);
    await picksPage.goto();
    // Add test steps...
  });
});
```

### Modifying Page Objects

Edit files in `pages/` to update selectors or add new methods when UI changes.

### Custom Excel Validation

Edit `helpers/excel-validator.ts` to add or modify validation rules.

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Playwright TypeScript Guide](https://playwright.dev/docs/test-typescript)
- [Against The Spread Repository](https://github.com/quaz579/against-the-spread)
