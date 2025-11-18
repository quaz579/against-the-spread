import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for the Picks page
 * Handles user name entry, week selection, game picks, and download
 */
export class PicksPage {
  readonly page: Page;
  readonly nameInput: Locator;
  readonly yearSelect: Locator;
  readonly weekSelect: Locator;
  readonly continueButton: Locator;
  readonly downloadButton: Locator;
  readonly backButton: Locator;
  readonly selectedPicksInfo: Locator;

  constructor(page: Page) {
    this.page = page;
    this.nameInput = page.locator('#userName');
    this.yearSelect = page.locator('#year');
    this.weekSelect = page.locator('#week');
    this.continueButton = page.locator('button:has-text("Continue to Picks")');
    this.downloadButton = page.locator('button:has-text("Generate Your Picks")');
    this.backButton = page.locator('button:has-text("Back to Week Selection")');
    this.selectedPicksInfo = page.locator('.alert-info');
  }

  /**
   * Navigate to picks page
   */
  async goto(): Promise<void> {
    await this.page.goto('/picks');
    // Wait for page to be fully loaded
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Enter user name
   * @param name User's full name
   */
  async enterName(name: string): Promise<void> {
    await this.nameInput.fill(name);
  }

  /**
   * Select year and week, then continue to game selection
   * @param year Year (e.g., 2025)
   * @param week Week number (1-17)
   */
  async selectWeek(year: number, week: number): Promise<void> {
    // Select year
    await this.yearSelect.selectOption(year.toString());

    // Wait a bit for available weeks to load
    await this.page.waitForTimeout(2000);

    // Select week
    await this.weekSelect.selectOption(week.toString());

    // Click continue button
    await this.continueButton.click();

    // Wait for games to load
    await this.page.waitForSelector('.card .btn', { timeout: 30000 });
  }

  /**
   * Select games by clicking team buttons
   * Selects the first available team button in each game until we have the required number
   * @param count Number of games to select (default: 6)
   */
  async selectGames(count: number = 6): Promise<void> {
    // Find all team buttons that are not disabled
    const teamButtons = this.page.locator('.card .btn:not([disabled])');
    
    // Wait for buttons to be available
    await teamButtons.first().waitFor({ timeout: 10000 });

    // Click the first 'count' buttons
    for (let i = 0; i < count; i++) {
      await teamButtons.nth(i).click();
      // Small delay to allow UI to update
      await this.page.waitForTimeout(300);
    }

    // Verify we have the right number of picks
    const picksInfo = await this.selectedPicksInfo.textContent();
    if (!picksInfo?.includes(`${count} / 6`)) {
      throw new Error(`Expected ${count} picks, but UI shows: ${picksInfo}`);
    }
  }

  /**
   * Click the download button to generate picks Excel file
   */
  async clickDownloadButton(): Promise<void> {
    await this.downloadButton.waitFor({ state: 'visible', timeout: 5000 });
    await this.downloadButton.click();
  }

  /**
   * Get the currently selected picks count from the UI
   */
  async getSelectedPicksCount(): Promise<number> {
    const text = await this.selectedPicksInfo.textContent();
    const match = text?.match(/Selected: (\d+) \/ 6/);
    return match ? parseInt(match[1]) : 0;
  }
}
