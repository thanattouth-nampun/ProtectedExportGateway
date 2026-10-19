import { test, expect } from '@playwright/test';

test('searchable list, owner filtering and HTML details', async ({ page }) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await page.goto('/spike.html');
  await expect(page.getByText('3 visible documents')).toBeVisible();
  await expect(page.locator('canvas')).toHaveCount(0);
  await expect(page.getByRole('button', { name: '3D', exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: /Project delivery plan/ }).click();
  await expect(page.getByRole('complementary', { name: 'Document details' }).getByRole('heading', { name: 'Project delivery plan' })).toBeVisible();
  await page.getByLabel('Business owner').selectOption('Owner O2');
  await expect(page.getByText('1 visible document', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: /Project delivery plan/ })).toHaveCount(0);
  await page.getByLabel('Search documents').fill('missing');
  await expect(page.getByRole('heading', { name: 'No matching documents' })).toBeVisible();
  await page.getByLabel('Search documents').fill('budget');
  await page.getByRole('button', { name: /Budget working file/ }).click();
  await expect(page.getByRole('heading', { name: 'Budget working file' })).toBeVisible();
  await expect(page.getByRole('button', { name: /Download my copy/ })).toHaveCount(0);
  await page.getByLabel('Business owner').selectOption('');
  await page.getByLabel('Search documents').fill('');
  expect(errors).toEqual([]);
  await page.screenshot({ path: 'test-results/workspace-desktop.png', fullPage: true });
});

test('identity change clears selection, old queries and authorized metadata', async ({ page }) => {
  let identity = 'A';
  await page.route('**/auth/session', route => route.fulfill({ json: { tenantId:'tenant',objectId:identity,displayName:`Requester ${identity}`,roles:['LibraryReader'],mode:'Simulated' } }));
  await page.route('**/api/documents', route => route.fulfill({ json: { total:1,items:[{id:`doc-${identity}`,title:`Document for ${identity}`,purpose:'Synthetic',owner:`Owner ${identity}`,filename:'fixture.xlsx',version:'v1',classification:'Unknown',status:'Fixture',canRequestCopy:false}] } }));
  await page.goto('/library'); await page.getByRole('button', { name: /Document for A/ }).click();
  identity = 'B'; await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect(page.getByRole('button', { name: /Document for B/ })).toBeVisible();
  await expect(page.getByText('Owner A', { exact:true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: /Document for A/ })).toHaveCount(0);
  await expect(page.getByRole('heading', { name:'A copy of your own' })).toBeVisible();
});

test('folder scope, breadcrumbs and cross-folder search remain independent of copy rights', async ({ page }) => {
  await page.goto('/spike.html');
  await page.getByRole('treeitem', { name: 'Operations', exact: true }).locator(':scope > div > .folder-row').click();
  await expect(page.getByText('2 visible documents', { exact: true })).toBeVisible();
  await page.getByRole('treeitem', { name: 'Planning', exact: true }).locator(':scope > div > .folder-row').click();
  await expect(page.getByText('1 visible document', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: /Budget working file/ })).toHaveCount(0);
  await page.getByRole('navigation', { name: 'Folder path' }).getByRole('button', { name: 'Operations', exact: true }).click();
  await expect(page.getByText('2 visible documents', { exact: true })).toBeVisible();
  await page.getByLabel('Search documents').fill('budget');
  await expect(page.getByText('Searching all authorized folders.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: /Budget working file/ }).click();
  await expect(page.getByRole('heading', { name: 'Budget working file' })).toBeVisible();
  await expect(page.getByRole('button', { name: /Request my copy|Download my copy/ })).toHaveCount(0);
  await page.getByLabel('Search documents').fill('');
  await expect(page.getByText('2 visible documents', { exact: true })).toBeVisible();
  await page.locator('.folder-panel').getByRole('button', { name:'All documents', exact:true }).click();
  await expect(page.getByText('3 visible documents', { exact: true })).toBeVisible();
});

test('folder tree supports arrows, selection, collapse, parent focus, Home, End and type-ahead', async ({ page }) => {
  await page.goto('/spike.html');
  const root = page.getByRole('treeitem', { name:'Gateway library', exact:true });
  const operations = page.getByRole('treeitem', { name:'Operations', exact:true });
  const planning = page.getByRole('treeitem', { name:'Planning', exact:true });
  const finance = page.getByRole('treeitem', { name:'Finance', exact:true });
  await root.focus(); await page.keyboard.press('ArrowDown'); await expect(operations).toBeFocused();
  await page.keyboard.press('ArrowRight'); await expect(operations).toHaveAttribute('aria-expanded','true');
  await page.keyboard.press('ArrowRight'); await expect(planning).toBeFocused();
  await page.keyboard.press('Enter'); await expect(planning).toHaveAttribute('aria-selected','true');
  await expect(page.getByText('1 visible document', { exact:true })).toBeVisible();
  await page.keyboard.press('ArrowLeft'); await expect(operations).toBeFocused();
  await page.keyboard.press('ArrowLeft'); await expect(operations).toHaveAttribute('aria-expanded','false');
  await page.keyboard.press('End'); await expect(finance).toBeFocused();
  await page.keyboard.press('o'); await expect(operations).toBeFocused();
  await page.keyboard.press('Home'); await expect(root).toBeFocused();
  await page.keyboard.press('ArrowLeft'); await expect(finance).toHaveCount(0);
  await page.keyboard.press('ArrowRight'); await expect(finance).toBeVisible();
});

test('Magic UI folders use upstream icons and controlled pointer expansion without changing selection', async ({ page }) => {
  await page.goto('/spike.html');
  const operations = page.getByRole('treeitem', { name: 'Operations', exact: true });
  await expect(operations.locator(':scope > div > button svg.lucide-folder-open')).toHaveCount(1);
  await operations.locator(':scope > div > button .folder-chevron').click();
  await expect(operations).toHaveAttribute('aria-expanded', 'false');
  await expect(page.getByText('3 visible documents', { exact: true })).toBeVisible();
  await operations.locator(':scope > div > button.folder-row').click();
  await expect(operations).toHaveAttribute('aria-selected', 'true');
  await expect(operations).toHaveAttribute('aria-expanded', 'true');
  await page.locator('.folder-panel').getByRole('button', { name:'All documents', exact:true }).click();
  await expect(operations).toHaveAttribute('aria-selected', 'false');
  await expect(operations.locator(':scope > div > button')).not.toHaveClass(/selected/);
});

test('workspace uses no WebGL even when GPU contexts are unavailable', async ({ page }) => {
  await page.addInitScript(() => {
    const original = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function(this: HTMLCanvasElement, type: string, ...args: unknown[]) {
      if (type === 'webgl' || type === 'webgl2') throw new Error('Unexpected GPU context request');
      return original.call(this, type as never, ...args);
    } as typeof original;
  });
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await page.goto('/spike.html');
  await page.getByRole('button', { name: /Resource allocation/ }).click();
  await expect(page.getByRole('heading', { name: 'Resource allocation' })).toBeVisible();
  await expect(page.locator('canvas')).toHaveCount(0);
  expect(errors).toEqual([]);
});

test('keyboard, reduced motion and narrow viewport preserve tasks', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' }); await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/spike.html');
  await page.getByText('Browse folders', { exact:true }).click();
  await expect(page.getByRole('tree', { name:'Document folders' })).toBeHidden();
  const documentButton = page.getByRole('button', { name: /Budget working file/ }); await documentButton.focus(); await page.keyboard.press('Enter');
  await expect(documentButton).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('heading', { name: 'Budget working file' })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: 'test-results/workspace-mobile.png', fullPage: true });
});

test('production entry never imports fixtures and displays sign-in on expired session', async ({ page }) => {
  await page.route('**/auth/session', route => route.fulfill({ status: 401, json: { code: 'SESSION_REQUIRED' } }));
  await page.goto('/');
  await expect(page.getByRole('link', { name: 'Sign in with Microsoft' })).toBeVisible();
  await expect(page.getByText('Owner O1')).toHaveCount(0);
  await expect(page.locator('canvas')).toHaveCount(0);
});

