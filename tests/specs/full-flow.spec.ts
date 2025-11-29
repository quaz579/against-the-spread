import { test, expect } from '@playwright/test';
import { PicksPage } from '../pages/picks-page';
import { waitForDownloadAndSave, cleanupDownloads } from '../helpers/download-helper';
import { validatePicksExcel } from '../helpers/excel-validator';
import { TestEnvironment } from '../helpers/test-environment';
import * as path from 'path';
import * as fs from 'fs';

const DOWNLOAD_DIR = '/tmp/playwright-downloads';
const REPO_ROOT = path.resolve(__dirname, '../..');

test.describe('Complete User Flow', () => {
  let testEnv: TestEnvironment;

  test.beforeAll(async () => {
    // Clean up any previous downloads
    cleanupDownloads(DOWNLOAD_DIR);
    
    // Initialize test environment for uploading test data
    testEnv = new TestEnvironment(REPO_ROOT);
    
    // Upload Week 11 test data to Azurite
    // This assumes services are already running (started via start-local.sh)
    const week11File = path.join(REPO_ROOT, 'reference-docs', 'Week 11 Lines.xlsx');
    if (fs.existsSync(week11File)) {
      await testEnv.uploadLinesFile(week11File, 11, 2025);
    }
    
    // Upload Week 12 test data
    const week12File = path.join(REPO_ROOT, 'reference-docs', 'Week 12 Lines.xlsx');
    if (fs.existsSync(week12File)) {
      await testEnv.uploadLinesFile(week12File, 12, 2025);
    }
  });

  test.afterAll(async () => {
    // Clean up downloads after all tests
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test('should complete picks flow for Week 11 and download valid Excel', async ({ page }) => {
    const testName = 'E2E Test User';
    const week = 11;
    const year = 2025;

    // Navigate to picks page
    const picksPage = new PicksPage(page);
    await picksPage.goto();

    // Wait for page to fully load
    await page.waitForLoadState('networkidle');

    // Enter user name
    await test.step('Enter user name', async () => {
      await picksPage.enterName(testName);
      await expect(picksPage.nameInput).toHaveValue(testName);
    });

    // Select year and week
    await test.step('Select year and week', async () => {
      await picksPage.selectWeek(year, week);
      await expect(picksPage.weekSelect).toHaveValue(week.toString());
    });

    // Click continue to load games
    await test.step('Click Continue to load games', async () => {
      await picksPage.clickContinue();
      await picksPage.waitForGamesToLoad();
      
      // Verify games are displayed
      const gameCards = page.locator('.card');
      await expect(gameCards.first()).toBeVisible();
    });

    // Select 6 games
    await test.step('Select 6 games', async () => {
      await picksPage.selectGames(6);
      
      // Verify 6 picks are selected
      const selectedCount = await picksPage.getSelectedCount();
      expect(selectedCount).toBe(6);
      
      // Verify download button is enabled
      await expect(picksPage.downloadButton).toBeEnabled();
    });

    // Download Excel file
    let downloadPath: string;
    await test.step('Download Excel file', async () => {
      downloadPath = await waitForDownloadAndSave(
        page,
        async () => await picksPage.clickDownload(),
        DOWNLOAD_DIR
      );
      
      // Verify file exists
      expect(fs.existsSync(downloadPath)).toBe(true);
    });

    // Validate Excel file structure
    await test.step('Validate Excel file structure', async () => {
      const validation = await validatePicksExcel(downloadPath!, testName, 6);
      
      if (!validation.isValid) {
        console.log('Validation errors:', validation.errors);
      }
      
      expect(validation.isValid).toBe(true);
      expect(validation.errors).toHaveLength(0);
    });
  });

  test('should complete picks flow for Week 12 and download valid Excel', async ({ page }) => {
    const testName = 'Week 12 Tester';
    const week = 12;
    const year = 2025;

    // Navigate to picks page
    const picksPage = new PicksPage(page);
    await picksPage.goto();

    // Wait for page to fully load
    await page.waitForLoadState('networkidle');

    // Enter user name
    await test.step('Enter user name', async () => {
      await picksPage.enterName(testName);
      await expect(picksPage.nameInput).toHaveValue(testName);
    });

    // Select year and week
    await test.step('Select year and week', async () => {
      await picksPage.selectWeek(year, week);
      await expect(picksPage.weekSelect).toHaveValue(week.toString());
    });

    // Click continue to load games
    await test.step('Click Continue to load games', async () => {
      await picksPage.clickContinue();
      await picksPage.waitForGamesToLoad();
      
      // Verify games are displayed
      const gameCards = page.locator('.card');
      await expect(gameCards.first()).toBeVisible();
    });

    // Select 6 games
    await test.step('Select 6 games', async () => {
      await picksPage.selectGames(6);
      
      // Verify 6 picks are selected
      const selectedCount = await picksPage.getSelectedCount();
      expect(selectedCount).toBe(6);
      
      // Verify download button is enabled
      await expect(picksPage.downloadButton).toBeEnabled();
    });

    // Download Excel file
    let downloadPath: string;
    await test.step('Download Excel file', async () => {
      downloadPath = await waitForDownloadAndSave(
        page,
        async () => await picksPage.clickDownload(),
        DOWNLOAD_DIR
      );
      
      // Verify file exists
      expect(fs.existsSync(downloadPath)).toBe(true);
    });

    // Validate Excel file structure
    await test.step('Validate Excel file structure', async () => {
      const validation = await validatePicksExcel(downloadPath!, testName, 6);
      
      if (!validation.isValid) {
        console.log('Validation errors:', validation.errors);
      }
      
      expect(validation.isValid).toBe(true);
      expect(validation.errors).toHaveLength(0);
    });
  });
});
