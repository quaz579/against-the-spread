import { Page, Download } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Wait for a download to complete and save it to the specified directory.
 * 
 * @param page - Playwright page object
 * @param downloadTriggerAction - Function that triggers the download (e.g., clicking a button)
 * @param saveDir - Directory to save the downloaded file (defaults to /tmp/playwright-downloads)
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

  // Set up download listener before triggering action
  const downloadPromise = page.waitForEvent('download');
  
  // Trigger the download
  await downloadTriggerAction();
  
  // Wait for download to start
  const download = await downloadPromise;
  
  // Wait for download to complete and get the path
  const suggestedFilename = download.suggestedFilename();
  const savePath = path.join(saveDir, suggestedFilename);
  
  // Save the file
  await download.saveAs(savePath);
  
  // Verify file was saved
  if (!fs.existsSync(savePath)) {
    throw new Error(`Download failed: file not found at ${savePath}`);
  }
  
  return savePath;
}

/**
 * Clean up downloaded files in the specified directory
 * 
 * @param directory - Directory to clean (defaults to /tmp/playwright-downloads)
 */
export function cleanupDownloads(directory: string = '/tmp/playwright-downloads'): void {
  if (fs.existsSync(directory)) {
    const files = fs.readdirSync(directory);
    for (const file of files) {
      const filePath = path.join(directory, file);
      fs.unlinkSync(filePath);
    }
  }
}
