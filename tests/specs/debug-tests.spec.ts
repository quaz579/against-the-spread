import { test, expect } from '@playwright/test';

/**
 * Debug tests to investigate the issue step by step
 */
test.describe('Debug Tests', () => {
  test('Step 1: Check if web app is accessible', async ({ page }) => {
    console.log('=== Step 1: Checking web app accessibility ===');
    await page.goto('http://localhost:5158');
    await page.waitForLoadState('networkidle');
    
    const title = await page.title();
    console.log('Page title:', title);
    expect(title).toBeTruthy();
  });

  test('Step 2: Check if API is accessible', async ({ request }) => {
    console.log('=== Step 2: Checking API accessibility ===');
    const response = await request.get('http://localhost:7071/api/weeks?year=2025');
    console.log('API response status:', response.status());
    
    const data = await response.json();
    console.log('API response data:', JSON.stringify(data, null, 2));
    
    expect(response.status()).toBe(200);
  });

  test('Step 3: Check if picks page loads', async ({ page }) => {
    console.log('=== Step 3: Checking picks page ===');
    
    // Capture browser console logs
    const consoleLogs: string[] = [];
    page.on('console', msg => {
      const text = msg.text();
      consoleLogs.push(text);
      if (text.includes('[ApiService]') || text.includes('[Blazor]')) {
        console.log('Browser console:', text);
      }
    });
    
    await page.goto('http://localhost:5158/picks');
    await page.waitForLoadState('networkidle');
    
    // Wait a bit longer for the page to fully render
    await page.waitForTimeout(3000);
    
    // Take screenshot
    await page.screenshot({ path: '/tmp/picks-page-debug.png' });
    console.log('Screenshot saved to /tmp/picks-page-debug.png');
    
    // Print all console logs
    console.log('=== Browser Console Logs ===');
    consoleLogs.forEach(log => console.log(log));
    
    // Check what's on the page
    const hasError = await page.locator('.alert-danger').count() > 0;
    const hasForm = await page.locator('select#year').count() > 0;
    const isLoading = await page.locator('.spinner-border').count() > 0;
    
    console.log('Is loading:', isLoading);
    console.log('Has error message:', hasError);
    console.log('Has year selector:', hasForm);
    
    if (hasError) {
      const errorText = await page.locator('.alert-danger').textContent();
      console.log('Error message:', errorText);
    }
    
    if (hasForm) {
      console.log('✓ Form is visible! Page loaded successfully.');
      
      // Check if weeks are populated
      const weekOptions = await page.locator('select#week option').count();
      console.log('Week options count:', weekOptions);
    }
  });
});
