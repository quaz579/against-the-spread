import { Page, Locator } from '@playwright/test';
import * as path from 'path';

/**
 * Page object model for the Admin page
 * Handles file upload functionality for weekly lines
 */
export class AdminPage {
  readonly page: Page;
  readonly weekInput: Locator;
  readonly yearInput: Locator;
  readonly fileInput: Locator;
  readonly uploadButton: Locator;
  readonly successMessage: Locator;
  readonly errorMessage: Locator;
  readonly loginButton: Locator;
  readonly signedInInfo: Locator;

  constructor(page: Page) {
    this.page = page;
    this.weekInput = page.locator('#weekInput');
    this.yearInput = page.locator('#yearInput');
    this.fileInput = page.locator('#fileInput');
    this.uploadButton = page.getByRole('button', { name: /Upload Lines/i });
    this.successMessage = page.locator('.alert-success');
    this.errorMessage = page.locator('.alert-danger');
    this.loginButton = page.getByRole('button', { name: /Sign in with Google/i });
    this.signedInInfo = page.locator('.alert-info');
  }

  async goto(): Promise<void> {
    await this.page.goto('/admin');
  }

  /**
   * Upload a lines file for a specific week and year.
   * Note: This requires authentication in production. For local testing,
   * authentication may be bypassed or mocked.
   */
  async uploadLinesFile(
    filePath: string,
    week: number,
    year: number
  ): Promise<void> {
    // Navigate to admin page
    await this.goto();

    // Wait for page to load
    await this.page.waitForLoadState('networkidle');

    // Check if we need to authenticate (login button visible)
    const loginVisible = await this.loginButton.isVisible().catch(() => false);
    
    if (loginVisible) {
      // For local testing, we might need to handle authentication differently
      // In a real scenario, we'd use test authentication
      throw new Error('Admin page requires authentication. For local testing, ensure authentication is configured.');
    }

    // Wait for the form to be visible
    await this.weekInput.waitFor({ state: 'visible', timeout: 10000 });

    // Fill in the week number
    await this.weekInput.clear();
    await this.weekInput.fill(week.toString());

    // Fill in the year
    await this.yearInput.clear();
    await this.yearInput.fill(year.toString());

    // Upload the file
    await this.fileInput.setInputFiles(filePath);

    // Click upload button
    await this.uploadButton.click();

    // Wait for response (success or error)
    await Promise.race([
      this.successMessage.waitFor({ state: 'visible', timeout: 30000 }),
      this.errorMessage.waitFor({ state: 'visible', timeout: 30000 })
    ]);
  }

  /**
   * Check if upload was successful
   */
  async isUploadSuccessful(): Promise<boolean> {
    return this.successMessage.isVisible();
  }

  /**
   * Get error message if upload failed
   */
  async getErrorMessage(): Promise<string | null> {
    const isVisible = await this.errorMessage.isVisible();
    if (isVisible) {
      return this.errorMessage.textContent();
    }
    return null;
  }
}
