import { expect, test } from '@playwright/test';

test('GIS credential stays in memory and authorizes only the admin API', async ({ page }) => {
  let observedGoogleToken: string | undefined;
  let observedAuthorization: string | undefined;

  await page.addInitScript(() => {
    let credentialCallback: ((response: { credential: string }) => void) | undefined;

    (window as any).google = {
      accounts: {
        id: {
          initialize: (options: { callback: (response: { credential: string }) => void }) => {
            credentialCallback = options.callback;
          },
          renderButton: (element: HTMLElement) => {
            const button = document.createElement('button');
            button.type = 'button';
            button.textContent = 'Sign in with Google';
            button.addEventListener('click', () => {
              credentialCallback?.({ credential: 'browser-only-test-token' });
            });
            element.replaceChildren(button);
          },
          disableAutoSelect: () => undefined
        }
      }
    };
  });

  await page.route('**/api/current-admin', async route => {
    observedGoogleToken = route.request().headers()['x-google-id-token'];
    observedAuthorization = route.request().headers()['authorization'];
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ email: 'test-admin@example.com' })
    });
  });

  await page.goto('/');
  await page.getByRole('link', { name: /Admin Upload/i }).click();
  await page.getByRole('button', { name: /Sign in with Google/i }).click();

  await expect(page.getByText(/Signed in as:/i)).toContainText('test-admin@example.com');
  await expect(page.locator('#weekInput')).toBeVisible();
  expect(observedGoogleToken).toBe('browser-only-test-token');
  expect(observedAuthorization).toBeUndefined();

  const validationMessage = "Invalid date header at C11: 'Friday, September 19, 2026'. Check that the weekday matches the calendar date.";
  await page.route('**/api/upload-lines?*', route => route.fulfill({
    status: 400,
    contentType: 'application/json',
    body: JSON.stringify({ success: false, error: validationMessage, message: validationMessage })
  }));
  await page.locator('#weekInput').fill('3');
  await page.locator('#fileInput').setInputFiles({
    name: 'invalid-date.xlsx',
    mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    buffer: Buffer.from('mock workbook; the parser is exercised in UploadPipelineAzuriteTests')
  });
  await page.getByRole('button', { name: /Upload Lines/i, exact: false }).click();
  await expect(page.locator('.alert-danger')).toContainText(validationMessage);
  await expect(page.locator('.alert-success')).toHaveCount(0);
  await expect(page.getByText(/Signed in as:/i)).toContainText('test-admin@example.com');

  await page.getByRole('button', { name: /Sign Out/i }).click();
  await expect(page.getByRole('button', { name: /Sign in with Google/i })).toBeVisible();
  await expect(page.locator('#weekInput')).toHaveCount(0);
});
