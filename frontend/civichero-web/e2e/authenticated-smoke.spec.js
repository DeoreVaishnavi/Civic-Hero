import { expect, test } from '@playwright/test';

const email = process.env.CIVICHERO_E2E_EMAIL;
const password = process.env.CIVICHERO_E2E_PASSWORD;

test('authenticated portal smoke test', async ({ page }) => {
  test.skip(!email || !password, 'Set CIVICHERO_E2E_EMAIL and CIVICHERO_E2E_PASSWORD to run authenticated smoke testing.');
  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page).toHaveURL(/\/(citizen|officer|supervisor|admin)(\/|$)/);
  await expect(page.getByText(/CivicHero/i).first()).toBeVisible();
});
