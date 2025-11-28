# Against The Spread - Playwright E2E Tests

This directory contains end-to-end (E2E) tests for the Against The Spread application using Playwright and TypeScript.

## Overview

The E2E tests validate the complete user flow:
1. **Data Upload**: Uploads Week 11 and Week 12 test data to Azurite blob storage
2. **User Flow**: Simulates a user making picks for both weeks
3. **Validation**: Downloads and validates the generated Excel files

**Key architectural decision**: Services (Azurite, Azure Functions, Web App) must be started manually before running tests using the existing `start-local.sh` script. This approach:
- Makes debugging easier
- Avoids port conflicts and timing issues
- Allows running tests multiple times without restarting services
- Keeps Playwright tests focused on browser automation

## Prerequisites

- **Node.js** (v18 or later)
- **npm** (comes with Node.js)
- **.NET 8 SDK**
- **Azure Functions Core Tools** (v4)
- **Azurite** (install globally: `npm install -g azurite`)

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

Before running tests, start all services using the provided script from the repository root:

```bash
# From repository root
./start-local.sh
```

Wait for the services to initialize (about 10-15 seconds). Verify they're running:
- **Azurite**: http://localhost:10000
- **Functions API**: http://localhost:7071/api/weeks?year=2025
- **Web App**: http://localhost:5158

## Running Tests

### Run all tests
```bash
cd tests
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

### Run with Playwright UI mode
```bash
npm run test:ui
```

### View HTML test report
```bash
npm run test:report
```

## Stopping Services

After testing, stop all services:

```bash
# From repository root
./stop-local.sh
```

## Project Structure

```
tests/
├── helpers/
│   ├── test-environment.ts    # Test environment utilities (file uploads)
│   ├── excel-validator.ts     # Validates Excel file structure
│   ├── download-helper.ts     # Handles file downloads
│   └── index.ts               # Exports all helpers
├── pages/
│   └── picks-page.ts          # Page Object Model for picks page
├── specs/
│   └── full-flow.spec.ts      # Complete user flow tests (Week 11 & 12)
├── playwright.config.ts       # Playwright configuration
├── package.json               # Dependencies
├── tsconfig.json              # TypeScript configuration
└── README.md                  # This file
```

## Test Flow

Each test follows this flow:

1. **Upload lines** to Azurite blob storage using TestEnvironment helper
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

- **Base URL**: `http://localhost:5158`
- **Browser**: Chromium
- **Retries**: 2 on CI, 0 locally
- **Workers**: 1 (sequential execution)
- **Traces**: On first retry
- **Screenshots**: On failure
- **Video**: On failure
- **Timeouts**: 15s for actions, 30s for navigation

## CI/CD Integration

The tests run automatically on every pull request via GitHub Actions:

- Workflow file: `.github/workflows/e2e-tests.yml`
- Installs all dependencies (Azure Functions Core Tools, Azurite, Playwright)
- Starts services using `start-local.sh`
- Runs tests in CI environment
- Uploads test results and traces as artifacts

## Troubleshooting

### Services not running

Ensure you've started services before running tests:
```bash
./start-local.sh
```

Wait for all services to be ready before running tests.

### Port conflicts

If you get port conflicts, check if services are already running:
```bash
lsof -i :7071  # Azure Functions
lsof -i :5158  # Web App
lsof -i :10000 # Azurite
```

Stop any conflicting processes or use `./stop-local.sh`.

### Tests timeout waiting for elements

- Verify services are running and responding
- Check browser console for JavaScript errors
- Run tests in headed mode to see what's happening: `npm run test:headed`

### Excel validation fails

- Check that the downloaded file exists in `/tmp/playwright-downloads`
- Verify the Excel structure matches `reference-docs/Weekly Picks Example.xlsx`
- Look at the test output for specific validation errors

### Week not appearing in dropdown

- Ensure the lines file was uploaded to Azurite
- Check Functions logs for upload errors
- Verify the year matches (default is current year)

## Development

### Adding New Tests

1. Create a new file in `specs/` directory
2. Import test utilities from `@playwright/test`
3. Use `test.describe` and `test` to structure your tests
4. Follow the existing test patterns in `full-flow.spec.ts`

### Creating Page Objects

1. Create a new file in `pages/` directory
2. Define locators for page elements
3. Add methods for common interactions
4. See `picks-page.ts` for an example

### Custom Validation

Edit `helpers/excel-validator.ts` to add custom Excel validation logic.

## Reference Files

Test data files are in `reference-docs/`:
- `Week 11 Lines.xlsx` - Week 11 game lines
- `Week 12 Lines.xlsx` - Week 12 game lines
- `Weekly Picks Example.xlsx` - Expected output format

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Playwright TypeScript Guide](https://playwright.dev/docs/test-typescript)
- [Against The Spread Repository](https://github.com/quaz579/against-the-spread)
