import { expect, test } from '@playwright/test';

test('public landing page and login page are reachable', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: /Report civic issues/i })).toBeVisible();

  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
});

test('unknown route shows the not-found page', async ({ page }) => {
  await page.goto('/not-a-real-page');
  await expect(page.getByRole('heading', { name: '404' })).toBeVisible();
});
