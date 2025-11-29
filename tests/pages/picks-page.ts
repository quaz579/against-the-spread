import { Page, Locator } from '@playwright/test';

/**
 * Page object model for the Picks page
 * Handles the user flow for selecting games and downloading picks
 */
export class PicksPage {
  readonly page: Page;
  readonly nameInput: Locator;
  readonly yearSelect: Locator;
  readonly weekSelect: Locator;
  readonly continueButton: Locator;
  readonly downloadButton: Locator;
  readonly backButton: Locator;
  readonly loadingSpinner: Locator;
  readonly errorAlert: Locator;
  readonly selectedPicksInfo: Locator;

  constructor(page: Page) {
    this.page = page;
    this.nameInput = page.locator('#userName');
    this.yearSelect = page.locator('#year');
    this.weekSelect = page.locator('#week');
    this.continueButton = page.getByRole('button', { name: /Continue to Picks/i });
    this.downloadButton = page.getByRole('button', { name: /Generate Your Picks/i });
    this.backButton = page.getByRole('button', { name: /Back to Week Selection/i });
    this.loadingSpinner = page.locator('.spinner-border');
    this.errorAlert = page.locator('.alert-danger');
    this.selectedPicksInfo = page.locator('.alert-info');
  }

  async goto(): Promise<void> {
    await this.page.goto('/picks');
  }

  /**
   * Enter the user's name
   */
  async enterName(name: string): Promise<void> {
    await this.nameInput.waitFor({ state: 'visible' });
    await this.nameInput.clear();
    await this.nameInput.fill(name);
  }

  /**
   * Select year and week for picks
   */
  async selectWeek(year: number, week: number): Promise<void> {
    // Select year
    await this.yearSelect.selectOption(year.toString());
    
    // Wait for weeks to load
    await this.page.waitForTimeout(1000);
    
    // Select week
    await this.weekSelect.selectOption(week.toString());
  }

  /**
   * Click continue to load games
   */
  async clickContinue(): Promise<void> {
    await this.continueButton.click();
    
    // Wait for games to load
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 30000 });
  }

  /**
   * Select games by clicking on team buttons.
   * Each game card contains two team buttons - one for favorite and one for underdog.
   * 
   * @param gameCount - Number of games to select (defaults to 6)
   */
  async selectGames(gameCount: number = 6): Promise<void> {
    // Wait for team buttons to be visible
    const teamButtons = this.page.locator('.card .btn:not([disabled])');
    await teamButtons.first().waitFor({ state: 'visible', timeout: 10000 });

    // Get all clickable team buttons (not disabled)
    const buttons = await teamButtons.all();
    
    if (buttons.length < gameCount) {
      throw new Error(`Not enough games available. Found ${buttons.length} buttons but need ${gameCount * 2} (2 per game)`);
    }

    // Click on buttons to select teams
    // We need to select from different games, so we'll click every other pair
    let selected = 0;
    let buttonIndex = 0;
    
    while (selected < gameCount && buttonIndex < buttons.length) {
      const button = buttons[buttonIndex];
      
      // Check if button is enabled
      const isDisabled = await button.isDisabled();
      if (!isDisabled) {
        await button.click();
        selected++;
        
        // Wait a bit between clicks for UI to update
        await this.page.waitForTimeout(200);
        
        // Skip the opponent button in the same game (we want different games)
        buttonIndex += 2;
      } else {
        buttonIndex++;
      }
    }

    if (selected < gameCount) {
      throw new Error(`Could only select ${selected} games out of ${gameCount} required`);
    }
  }

  /**
   * Click the download/generate picks button
   */
  async clickDownload(): Promise<void> {
    await this.downloadButton.waitFor({ state: 'visible' });
    await this.downloadButton.click();
  }

  /**
   * Get the current selection count from the info banner
   */
  async getSelectedCount(): Promise<number> {
    const text = await this.selectedPicksInfo.textContent();
    const match = text?.match(/Selected:\s*(\d+)/);
    return match ? parseInt(match[1], 10) : 0;
  }

  /**
   * Check if download button is enabled (6 picks selected)
   */
  async isDownloadEnabled(): Promise<boolean> {
    return this.downloadButton.isEnabled();
  }

  /**
   * Wait for games to load
   */
  async waitForGamesToLoad(): Promise<void> {
    // Wait for loading spinner to disappear
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 30000 }).catch(() => {});
    
    // Wait for cards to appear
    await this.page.locator('.card').first().waitFor({ state: 'visible', timeout: 30000 });
  }

  /**
   * Check if an error occurred
   */
  async hasError(): Promise<boolean> {
    return this.errorAlert.isVisible();
  }

  /**
   * Get error message if present
   */
  async getErrorMessage(): Promise<string | null> {
    if (await this.hasError()) {
      return this.errorAlert.textContent();
    }
    return null;
  }
}
