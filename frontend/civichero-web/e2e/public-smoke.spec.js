import { expect, test } from '@playwright/test';

test('public landing page and login page are reachable', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: /Civic infrastructure/i })).toBeVisible();

  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'Login to CivicHero' })).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
});

test('unknown route returns to the public landing page', async ({ page }) => {
  await page.goto('/not-a-real-page');
  await expect(page.getByRole('heading', { name: /Civic infrastructure/i })).toBeVisible();
});
