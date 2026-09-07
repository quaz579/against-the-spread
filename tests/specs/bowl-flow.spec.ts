import { expect, test } from '@playwright/test';
import * as fs from 'fs';
import { cleanupDownloads, getDefaultDownloadDir, waitForDownloadAndSave } from '../helpers/download-helper';
import { validateBowlPicksExcel } from '../helpers/excel-validator';
import { seedAzuriteFixtures } from '../helpers/seed-azurite-fixtures';
import { BowlPicksPage } from '../pages/bowl-picks-page';

const TEST_NAME = 'Bowl E2E Test User';
const TEST_YEAR = new Date().getUTCFullYear();
const DOWNLOAD_DIR = getDefaultDownloadDir();

test.describe('Complete bowl user flow', () => {
  test.beforeAll(async () => {
    await seedAzuriteFixtures();
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test.beforeEach(() => cleanupDownloads(DOWNLOAD_DIR));
  test.afterAll(() => cleanupDownloads(DOWNLOAD_DIR));

  test('loads seeded bowl lines, makes picks, and downloads a valid workbook', async ({ page }) => {
    const bowlPicksPage = new BowlPicksPage(page);
    await bowlPicksPage.goto();
    await bowlPicksPage.waitForLoadingComplete();
    await bowlPicksPage.enterName(TEST_NAME);
    await bowlPicksPage.selectYearAndContinue(TEST_YEAR);

    expect(await bowlPicksPage.areGamesLoaded()).toBe(true);
    const gameCount = await bowlPicksPage.getGameCount();
    expect(gameCount).toBeGreaterThanOrEqual(3);

    const expectedPicks = await bowlPicksPage.makeAllPicks(gameCount);
    expect(expectedPicks).toHaveLength(gameCount);
    expect(await bowlPicksPage.isConfidenceSumValid()).toBe(true);
    expect(await bowlPicksPage.isDownloadButtonEnabled()).toBe(true);

    const downloadPath = await waitForDownloadAndSave(
      page,
      () => bowlPicksPage.clickDownload(),
      DOWNLOAD_DIR
    );
    expect(fs.existsSync(downloadPath)).toBe(true);

    const validation = await validateBowlPicksExcel(downloadPath, TEST_NAME, expectedPicks);
    expect(validation.errors).toEqual([]);
    expect(validation.isValid).toBe(true);
  });

  test('rejects duplicate confidence values without skipping', async ({ page }) => {
    const bowlPicksPage = new BowlPicksPage(page);
    await bowlPicksPage.goto();
    await bowlPicksPage.enterName('Validation Test User');
    await bowlPicksPage.selectYearAndContinue(TEST_YEAR);
    expect(await bowlPicksPage.getGameCount()).toBeGreaterThanOrEqual(2);

    await bowlPicksPage.selectSpreadPick(1, true);
    await bowlPicksPage.selectConfidence(1, 1);
    await bowlPicksPage.selectOutrightWinner(1, true);
    await bowlPicksPage.selectSpreadPick(2, false);
    await bowlPicksPage.selectConfidence(2, 1);
    await bowlPicksPage.selectOutrightWinner(2, false);

    expect(await bowlPicksPage.hasDuplicateConfidenceWarning()).toBe(true);
    expect(await bowlPicksPage.isDownloadButtonEnabled()).toBe(false);
  });
});
