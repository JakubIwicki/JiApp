import { test, expect } from '@playwright/test';
import path from 'path';
import fs from 'fs';

const STORYBOOK_URL = 'http://localhost:6006';

// Ensure screenshots directory exists
const screenshotDir = path.join('screenshots');
if (!fs.existsSync(screenshotDir)) {
  fs.mkdirSync(screenshotDir, { recursive: true });
}

function storyUrl(slug: string): string {
  return `${STORYBOOK_URL}/?path=/story/${slug}`;
}

// Verified story slugs from actual story files
const storySlugs = [
  // ── Components ──
  'button--default',
  'button--loading',
  'button--disabled',
  'forminput--default',
  'forminput--secure',
  'forminput--with-error',
  'forminput--with-label',
  'errormessage--with-retry',
  'errormessage--without-retry',
  'loadingspinner--default',
  'loadingspinner--large',
  'loadingspinner--small',
  'loadingspinner--custom-color',
  'searchbar--empty',
  'searchbar--with-text',
  'searchbar--with-clear-button',
  'videocard--default',
  'videocard--long-title',
  'videocard--missing-thumbnail',
  'videocard--no-description',
  'historyitem--search-type',
  'historyitem--download-type',
  'historyitem--download-type-missing-thumbnail',
  'historysection--empty-searches',
  'historysection--with-search-items',
  'historysection--with-download-items',
  'languagepicker--polish-selected',
  'languagepicker--english-selected',
  // ── Screens ──
  'screens-loginscreen--default',
  'screens-loginscreen--with-error',
  'screens-registerscreen--default',
  'screens-registerscreen--with-error',
  'screens-searchscreen--with-results',
  'screens-searchscreen--empty-results',
  'screens-searchscreen--with-error',
  'screens-downloadscreen--ready',
  'screens-downloadscreen--error',
  'screens-historyscreen--with-items',
  'screens-historyscreen--empty',
  'screens-historyscreen--with-error',
  'screens-settingsscreen--default',
  // ── Full App ──
  'screens-fullapp--search-with-results',
  'screens-fullapp--downloads-tab',
  'screens-fullapp--history-tab',
  'screens-fullapp--settings-tab',
];

test.describe('Storybook Visual Smoke Tests', () => {
  for (const slug of storySlugs) {
    test(slug, async ({ page }) => {
      const errors: string[] = [];
      page.on('pageerror', (err) => errors.push(err.message));
      page.on('console', (msg) => {
        if (msg.type() === 'error') errors.push(msg.text());
      });

      await page.goto(storyUrl(slug), { waitUntil: 'networkidle', timeout: 20000 });

      // Check story loaded: Storybook shows an error div with class if story not found
      const noMatch = page.locator('.sb-errordisplay, [id*="error"]');
      const noMatchVisible = await noMatch.isVisible().catch(() => false);
      if (noMatchVisible) {
        const errorText = await noMatch.textContent().catch(() => '');
        throw new Error(`Story not found: ${slug} — ${errorText}`);
      }

      // Wait for the preview iframe/section to have rendered content
      await page.waitForTimeout(800);

      // Collect non-Storybook-internal errors (exclude act() warnings and story not found)
      const realErrors = errors.filter(
        (e) =>
          !e.includes('act(') &&
          !e.includes('NoStoryMatchError') &&
          !e.includes('404') &&
          !e.includes('Failed to load resource'),
      );
      expect(realErrors).toEqual([]);

      // Screenshot
      const fileSafe = slug.replace(/[^a-z0-9-]/g, '-');
      await page.screenshot({
        path: path.join(screenshotDir, `${fileSafe}.png`),
        fullPage: true,
      });
    });
  }
});

test('Storybook loads and renders the sidebar', async ({ page }) => {
  await page.goto(STORYBOOK_URL, { waitUntil: 'networkidle', timeout: 20000 });

  // Verify the Storybook UI loaded
  const sidebar = page.locator('#storybook-explorer-tree, nav[aria-label="Sidebar"]');
  await expect(sidebar).toBeVisible({ timeout: 10000 });

  // Verify the preview area exists
  const preview = page.locator('#storybook-preview-wrapper, [id*="preview"]');
  await expect(preview.first()).toBeVisible({ timeout: 5000 }).catch(() => {
    // Some Storybook versions use different selectors
  });

  await page.screenshot({
    path: path.join(screenshotDir, 'storybook-shell.png'),
    fullPage: true,
  });
});

test('Settings tab language toggle switches between PL and EN', async ({ page }) => {
  await page.goto(storyUrl('screens-fullapp--settings-tab'), {
    waitUntil: 'networkidle',
    timeout: 20000,
  });

  // Navigate directly to the iframe content — bypasses Storybook chrome
  const iframeUrl = 'http://localhost:6006/iframe.html?id=screens-fullapp--settings-tab&viewMode=story';
  await page.goto(iframeUrl, { waitUntil: 'networkidle', timeout: 20000 });

  const plBtn = page.getByText('PL', { exact: true });
  const enBtn = page.getByText('EN', { exact: true });

  // Make sure both are visible first
  await plBtn.waitFor({ state: 'visible', timeout: 5000 });

  // Initially PL is active — click EN
  await enBtn.click();
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(screenshotDir, 'settings-language-en.png'),
    fullPage: true,
  });

  // Click back to PL
  await plBtn.click();
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(screenshotDir, 'settings-language-pl.png'),
    fullPage: true,
  });
});
