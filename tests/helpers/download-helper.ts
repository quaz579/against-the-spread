import { Page, Download } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Wait for a download to start, complete, and save it to a directory
 * 
 * @param page Playwright page instance
 * @param downloadTriggerAction Function that triggers the download
 * @param saveDir Directory to save the downloaded file (default: /tmp/playwright-downloads)
 * @returns Full path to the saved file
 */
export async function waitForDownloadAndSave(
  page: Page,
  downloadTriggerAction: () => Promise<void>,
  saveDir: string = '/tmp/playwright-downloads'
): Promise<string> {
  // Ensure save directory exists
  if (!fs.existsSync(saveDir)) {
    fs.mkdirSync(saveDir, { recursive: true });
  }

  // Set up download event listener
  const downloadPromise = page.waitForEvent('download');

  // Trigger the download action
  await downloadTriggerAction();

  // Wait for download to complete
  const download = await downloadPromise;

  // Get the suggested filename
  const suggestedFilename = download.suggestedFilename();
  const savePath = path.join(saveDir, suggestedFilename);

  // Save the file
  await download.saveAs(savePath);

  console.log(`Download saved to: ${savePath}`);

  return savePath;
}
