import { expect, test } from '@playwright/test';

test('customer completes the purchase flow and sees the order', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Purchase laboratory' })).toBeVisible();

  await page.getByRole('button', { name: 'Create product' }).click();
  await expect(page.getByText('Product created. Add stock before starting checkout.')).toBeVisible();
  await expect(page.getByText('Mechanical Keyboard', { exact: true })).toBeVisible();

  await page.getByRole('button', { name: 'Add stock' }).click();
  await expect(page.getByText('Stock added. The product is ready for checkout.')).toBeVisible();
  await expect(page.getByText('10 available', { exact: true })).toBeVisible();

  await page.getByRole('button', { name: 'Start checkout' }).click();
  await expect(page.getByText('Checkout started. Stock is reserved for 15 minutes.')).toBeVisible();
  await expect(page.getByText('2 unit(s) reserved', { exact: true })).toBeVisible();
  await expect(page.getByText('8 available', { exact: true })).toBeVisible();

  await page.getByRole('button', { name: 'Complete', exact: true }).click();

  await expect(page.getByText(
    'Checkout completed. The order now preserves the commercial snapshot.'
  )).toBeVisible();
  await expect(page.getByText('Completed', { exact: true })).toBeVisible();
  await expect(page.getByText('$1,000.00', { exact: true })).toBeVisible();
  await expect(page.getByText('2 x $500.00', { exact: true })).toBeVisible();
});
