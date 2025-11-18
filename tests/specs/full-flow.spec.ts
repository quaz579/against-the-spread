import { test, expect } from '@playwright/test';
import { AdminPage } from '../pages/admin-page';
import { PicksPage } from '../pages/picks-page';
import { waitForDownloadAndSave } from '../helpers/download-helper';
import { validatePicksExcel } from '../helpers/excel-validator';
import * as path from 'path';
import * as http from 'http';

/**
 * Complete user flow E2E test
 * 
 * Prerequisites:
 * - Services must be running (use ./start-local.sh from repository root)
 * - Azurite on port 10000
 * - Azure Functions on port 7071
 * - Blazor Web App on port 5158
 */
test.describe('Complete User Flow', () => {
  
  test.beforeAll(async () => {
    // Verify services are running by checking base URLs
    console.log('Verifying services are running...');
    
    const servicesToCheck = [
      { name: 'Web App', url: 'http://localhost:5158' },
      { name: 'Functions API', url: 'http://localhost:7071/api/weeks?year=2025' }
    ];

    for (const service of servicesToCheck) {
      const isRunning = await checkService(service.url);
      if (!isRunning) {
        throw new Error(
          `${service.name} is not running at ${service.url}. ` +
          `Please start services using './start-local.sh' before running tests.`
        );
      }
      console.log(`✓ ${service.name} is running`);
    }
  });

  test('should complete admin upload and user picks flow for Week 11', async ({ page }) => {
    const testName = 'E2E Test User';
    const week = 11;
    const year = 2025;
    let downloadPath: string = '';
    
    // === ADMIN FLOW ===
    await test.step('Upload Week 11 lines as admin', async () => {
      const adminPage = new AdminPage(page);
      await adminPage.goto();
      
      const linesFile = path.resolve(__dirname, '../../reference-docs/Week 11 Lines.xlsx');
      await adminPage.uploadLinesFile(linesFile, week, year);
      
      // Verify success message
      await expect(adminPage.successMessage).toBeVisible();
    });
    
    // === USER FLOW ===
    await test.step('Navigate to picks page and enter name', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.goto();
      await picksPage.enterName(testName);
      
      // Verify name was entered
      await expect(picksPage.nameInput).toHaveValue(testName);
    });
    
    await test.step('Select year and week', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.selectWeek(year, week);
      
      // Verify games loaded - check that we're now on the game selection view
      await expect(picksPage.backButton).toBeVisible();
      await expect(picksPage.selectedPicksInfo).toContainText('0 / 6 picks');
    });
    
    await test.step('Select 6 games', async () => {
      const picksPage = new PicksPage(page);
      
      // Select 6 games
      await picksPage.selectGames(6);
      
      // Verify download button is visible and enabled
      await expect(picksPage.downloadButton).toBeVisible();
      await expect(picksPage.downloadButton).toBeEnabled();
      
      // Verify selected picks count
      await expect(picksPage.selectedPicksInfo).toContainText('6 / 6 picks');
    });
    
    await test.step('Download Excel file', async () => {
      const picksPage = new PicksPage(page);
      
      downloadPath = await waitForDownloadAndSave(
        page,
        async () => await picksPage.clickDownloadButton(),
        '/tmp/playwright-downloads'
      );
      
      console.log(`Downloaded picks file to: ${downloadPath}`);
    });
    
    await test.step('Validate Excel file structure', async () => {
      const validation = await validatePicksExcel(downloadPath, testName, 6);
      
      // Log any errors for debugging
      if (!validation.isValid) {
        console.error('Excel validation errors:', validation.errors);
      }
      
      // Assert validation passed
      expect(validation.isValid, `Excel validation failed: ${validation.errors.join(', ')}`).toBe(true);
      expect(validation.errors).toHaveLength(0);
    });
  });
});

/**
 * Helper function to check if a service is running
 */
async function checkService(url: string): Promise<boolean> {
  return new Promise<boolean>((resolve) => {
    const req = http.get(url, (res) => {
      // Service is responding if we get any valid HTTP response
      if (res.statusCode && (res.statusCode < 500 || res.statusCode === 404)) {
        resolve(true);
      } else {
        resolve(false);
      }
      res.resume(); // Drain response
    });

    req.on('error', () => {
      resolve(false);
    });

    req.setTimeout(5000, () => {
      req.destroy();
      resolve(false);
    });
  });
}
