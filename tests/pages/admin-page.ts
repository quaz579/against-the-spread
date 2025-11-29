import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for the Admin page
 * Handles file upload for weekly game lines
 */
export class AdminPage {
  readonly page: Page;
  readonly weekInput: Locator;
  readonly yearInput: Locator;
  readonly fileInput: Locator;
  readonly uploadButton: Locator;
  readonly successMessage: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.weekInput = page.locator('#weekInput');
    this.yearInput = page.locator('#yearInput');
    this.fileInput = page.locator('#fileInput');
    this.uploadButton = page.locator('button:has-text("Upload Lines")');
    this.successMessage = page.locator('.alert-success');
    this.errorMessage = page.locator('.alert-danger');
  }

  /**
   * Navigate to admin page
   */
  async goto(): Promise<void> {
    await this.page.goto('/admin');
  }

  /**
   * Upload a lines file for a specific week and year
   * @param filePath Full path to the Excel file to upload
   * @param week Week number (1-17)
   * @param year Year (e.g., 2025)
   */
  async uploadLinesFile(
    filePath: string,
    week: number,
    year: number
  ): Promise<void> {
    // Navigate to admin page
    await this.goto();

    // Enter week number
    await this.weekInput.fill(week.toString());

    // Enter year
    await this.yearInput.fill(year.toString());

    // Select file
    await this.fileInput.setInputFiles(filePath);

    // Click upload button
    await this.uploadButton.click();

    // Wait for success message
    await this.successMessage.waitFor({ timeout: 30000 });
  }
}
