import { expect, test } from '@playwright/test';

test('customer completes the purchase flow and sees the order', async ({ page }) => {
  await page.goto('/admin/products');

  await page.locator('#username').fill('administrator');
  await page.locator('#password').fill('administrator');
  await page.locator('#kc-login').click();

  await expect(page.getByRole('heading', { name: 'Catalog operations' })).toBeVisible();
  await page.getByRole('button', { name: 'Create product' }).click();
  await expect(page.getByText('Product created. Add physical stock before customers can reserve it.')).toBeVisible();
  await page.getByRole('button', { name: 'Add stock' }).click();
  await expect(page.getByText('Physical stock added. Availability is visible in the storefront.')).toBeVisible();

  await page.getByRole('button', { name: 'Sign out' }).click();

  await expect(page.getByRole('heading', { name: 'Choose. Reserve. Complete.' })).toBeVisible();
  const catalogProduct = page.getByRole('button', { name: /Mechanical Keyboard/ });
  await expect(catalogProduct).toBeVisible();
  await expect(catalogProduct).toContainText('10 available');

  await page.getByRole('button', { name: 'Sign in to checkout' }).click();
  await page.locator('#username').fill('customer');
  await page.locator('#password').fill('customer');
  await page.locator('#kc-login').click();

  await page.getByRole('button', { name: 'Start checkout' }).click();
  await expect(page.getByText('Checkout started. Stock is reserved for 15 minutes.')).toBeVisible();
  await expect(page.getByText('2 unit(s) reserved', { exact: true })).toBeVisible();
  await expect(catalogProduct).toContainText('8 available');

  await page.getByRole('button', { name: 'Complete', exact: true }).click();

  await expect(page.getByText(
    'Checkout completed. The order now preserves the commercial snapshot.'
  )).toBeVisible();
  await expect(page.getByText('Completed', { exact: true })).toBeVisible();
  await expect(page.getByText('$1,000.00', { exact: true })).toBeVisible();
  await expect(page.getByText('2 x $500.00', { exact: true })).toBeVisible();
});
