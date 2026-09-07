import { expect, test } from '@playwright/test';
import * as fs from 'fs';
import { cleanupDownloads, getDefaultDownloadDir, waitForDownloadAndSave } from '../helpers/download-helper';
import { validatePicksExcel } from '../helpers/excel-validator';
import { seedAzuriteFixtures } from '../helpers/seed-azurite-fixtures';
import { PicksPage } from '../pages/picks-page';

const TEST_NAME = 'E2E Test User';
const TEST_YEAR = new Date().getUTCFullYear();
const DOWNLOAD_DIR = getDefaultDownloadDir();

test.describe('Complete weekly user flow', () => {
  test.beforeAll(async () => {
    await seedAzuriteFixtures();
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test.beforeEach(() => cleanupDownloads(DOWNLOAD_DIR));
  test.afterAll(() => cleanupDownloads(DOWNLOAD_DIR));

  for (const week of [11, 12]) {
    test(`Week ${week}: loads seeded lines and downloads a valid workbook`, async ({ page }) => {
      const picksPage = new PicksPage(page);
      await picksPage.goto();
      await picksPage.waitForLoadingComplete();
      await picksPage.enterName(TEST_NAME);
      await picksPage.selectWeek(TEST_YEAR, week);

      expect((await picksPage.getTeamButtons()).length).toBeGreaterThanOrEqual(12);
      await picksPage.selectGames(6);
      expect(await picksPage.getSelectedPickCount()).toBe(6);
      expect(await picksPage.isDownloadButtonEnabled()).toBe(true);

      const downloadPath = await waitForDownloadAndSave(
        page,
        () => picksPage.clickDownload(),
        DOWNLOAD_DIR
      );
      expect(fs.existsSync(downloadPath)).toBe(true);

      const validation = await validatePicksExcel(downloadPath, TEST_NAME, 6);
      expect(validation.errors).toEqual([]);
      expect(validation.isValid).toBe(true);
    });
  }
});
