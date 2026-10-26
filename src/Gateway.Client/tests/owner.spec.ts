import { test, expect, type Page } from '@playwright/test';

const owner = '22222222-2222-4222-8222-222222222222';
const a = '33333333-3333-4333-8333-333333333333';
const documentId = '44444444-4444-4444-8444-444444444444';
const draft = () => ({ id: documentId, title: 'Synthetic owner draft', purpose: 'Synthetic review', revision: 1, aclRevision: 1,
  eTag: '"revision-1"', requesterObjectIds: [a], status: 'Draft', sourceOwnershipStatus: 'NoSourceUploaded', canPublish: false });

// Browser behavior uses intercepted synthetic API responses. This is not Entra/SQL/live-intake evidence.
async function workspace(page: Page, existing = false) {
  const state = { doc: draft(), exists: existing, conflict: false, persona: 'owner', uploadEnabled: false, mutations: [] as { method: string; headers: Record<string, string>; body: Record<string, unknown> }[] };
  await page.route('**/auth/session', route => route.fulfill({ json: { tenantId: 'test-tenant', objectId: state.persona === 'owner' ? owner : a,
    displayName: state.persona === 'owner' ? 'Synthetic Owner' : 'Synthetic Reader', roles: state.persona === 'owner' ? ['DocumentPublisher'] : ['LibraryReader'], mode: 'Simulated', canManageOwnDocuments: state.persona !== 'guest', canUploadToLocalQuarantine: state.uploadEnabled } }));
  await page.route('**/auth/csrf', route => route.fulfill({ json: { token: 'synthetic-csrf' } }));
  await page.route('**/api/document-requester-options', route => route.fulfill({ json: [{ objectId: owner, displayName: 'Synthetic Owner' }, { objectId: a, displayName: 'Synthetic A' }] }));
  await page.route('**/api/my/documents?*', route => route.fulfill({ json: { items: state.exists && state.persona === 'owner' ? [state.doc] : [], total: state.exists && state.persona === 'owner' ? 1 : 0, skip: 0, pageSize: 25 } }));
  await page.route('**/api/documents/**', async route => {
    if (route.request().url().endsWith('/audit')) return route.fulfill({ json: [{ eventType: 'DocumentDraftCreated', documentRevision: 1, at: '2026-10-20T08:00:00Z', correlationId: 'test-correlation' }] });
    if (route.request().method() === 'GET') return route.fulfill({ json: state.doc });
    const req = route.request(); const body = req.postDataJSON(); state.mutations.push({ method: req.method(), headers: req.headers(), body });
    if (state.conflict) return route.fulfill({ status: 412, json: { code: 'REVISION_CONFLICT' } });
    state.doc = { ...state.doc, ...body, revision: state.doc.revision + 1, eTag: '"revision-2"' };
    return route.fulfill({ json: state.doc });
  });
  await page.route('**/api/documents', async route => {
    const req = route.request(); const body = req.postDataJSON(); state.mutations.push({ method: req.method(), headers: req.headers(), body });
    state.exists = true; state.doc = { ...state.doc, ...body };
    return route.fulfill({ status: 201, json: state.doc });
  });
  return state;
}

test('owner creates a private draft with explicit ACL and no owner override', async ({ page }) => {
  const state = await workspace(page);
  await page.goto('/my-documents');
  await expect(page.getByRole('heading', { name: 'Create draft' })).toBeVisible();
  await expect(page.getByLabel('Synthetic A', { exact: true })).not.toBeChecked();
  await page.getByRole('button', { name: 'Save draft' }).click();
  await expect(page.getByText('Enter a title.')).toBeVisible(); expect(state.mutations).toHaveLength(0);
  await page.getByLabel('Title', { exact: true }).fill('Synthetic new draft');
  await page.getByLabel('Purpose', { exact: true }).fill('Synthetic purpose');
  await page.getByLabel('Synthetic A', { exact: true }).check();
  await page.getByRole('button', { name: 'Save draft' }).click();
  await expect(page.getByText('Draft saved.', { exact: true }).first()).toBeVisible();
  expect(state.mutations).toHaveLength(1); expect(state.mutations[0].body).toEqual({ title: 'Synthetic new draft', purpose: 'Synthetic purpose', requesterObjectIds: [a] });
  expect(state.mutations[0].headers['x-csrf-token']).toBe('synthetic-csrf');
  await expect(page.getByRole('button', { name: 'Publish', exact: true })).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Upload version', exact: true })).toBeDisabled();
  await page.reload(); await page.getByRole('button', { name: /Synthetic new draft/ }).click();
  await expect(page.getByLabel('Synthetic A', { exact: true })).toBeChecked();
  await page.screenshot({ path: 'test-results/owner-workspace-desktop.png', fullPage: true });
});

test('owner edit sends If-Match and shows stale revision without overwriting', async ({ page }) => {
  const state = await workspace(page, true); state.conflict = true;
  await page.goto('/my-documents'); await page.getByRole('button', { name: /Synthetic owner draft/ }).click();
  await page.getByLabel('Title', { exact: true }).fill('Unsaved stale title');
  await page.getByRole('button', { name: 'Save draft' }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'another session' })).toBeVisible();
  expect(state.mutations[0].method).toBe('PATCH'); expect(state.mutations[0].headers['if-match']).toBe('"revision-1"');
  await expect(page.getByRole('button', { name: 'Save draft' })).toBeDisabled();
  state.doc = { ...state.doc, title: 'Server revision', eTag: '"revision-3"', revision: 3 };
  await page.getByRole('button', { name: 'Reload draft' }).click();
  await expect(page.getByLabel('Title', { exact: true })).toHaveValue('Server revision');
  await expect(page.getByRole('button', { name: 'Save draft' })).toBeEnabled();
});

test('identity boundary clears unsaved member form when membership is unverified', async ({ page }) => {
  const state = await workspace(page);
  await page.goto('/my-documents'); await page.getByLabel('Title', { exact: true }).fill('Private unsaved title');
  state.persona = 'guest'; await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect(page.getByText('A verified internal member session is required. Sign in again or contact your administrator.')).toBeVisible();
  await expect(page.getByLabel('Title', { exact: true })).toHaveCount(0); expect(state.mutations).toHaveLength(0);
});

test('Reader member has a workspace and switching members clears the prior draft and form', async ({ page }) => {
  const state = await workspace(page, true);
  await page.goto('/my-documents'); await page.getByRole('button', { name: /Synthetic owner draft/ }).click();
  await expect(page.getByLabel('Title', { exact: true })).toHaveValue('Synthetic owner draft');
  state.persona = 'reader'; await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect(page.getByText('Synthetic Reader', { exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: /My documents/ })).toBeVisible();
  await expect(page.getByRole('button', { name: /Synthetic owner draft/ })).toHaveCount(0);
  await expect(page.getByLabel('Title', { exact: true })).toHaveValue('');
  await page.getByLabel('Title', { exact: true }).fill('A private draft');
  await page.getByLabel('Purpose', { exact: true }).fill('Synthetic member check');
  await page.getByRole('button', { name: 'Save draft', exact: true }).click();
  await expect(page.getByText('Draft saved.', { exact: true }).first()).toBeVisible();
  expect(state.mutations).toHaveLength(1);
  expect(state.mutations[0].body).toEqual({ title: 'A private draft', purpose: 'Synthetic member check', requesterObjectIds: [] });
});

test('owner workspace reflows on mobile with keyboard access', async ({ page }) => {
  await workspace(page, true); await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/my-documents'); await page.getByRole('button', { name: /Synthetic owner draft/ }).click();
  await expect(page.getByLabel('Title', { exact: true })).toBeVisible();
  await page.getByLabel('Synthetic A', { exact: true }).focus(); await page.keyboard.press('Space');
  await expect(page.getByLabel('Synthetic A', { exact: true })).not.toBeChecked();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.screenshot({ path: 'test-results/owner-workspace-mobile.png', fullPage: true });
});

test('owner dependency failure offers retry without an editable form', async ({ page }) => {
  await workspace(page);
  await page.route('**/api/my/documents?*', route => route.fulfill({ status: 503, json: { code: 'DOCUMENT_STORE_UNAVAILABLE' } }));
  await page.goto('/my-documents'); await expect(page.getByRole('alert').filter({ hasText: 'unavailable' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Retry', exact: true })).toBeVisible(); await expect(page.getByRole('button', { name: 'Save draft' })).toHaveCount(0);
});

test('unconfirmed save blocks automatic duplicate submission and offers reload', async ({ page }) => {
  await workspace(page);
  await page.route('**/api/documents', route => route.fulfill({ status: 503, json: { code: 'DOCUMENT_STORE_UNAVAILABLE' } }));
  await page.goto('/my-documents');
  await page.getByLabel('Title', { exact: true }).fill('Unconfirmed draft');
  await page.getByLabel('Purpose', { exact: true }).fill('Synthetic');
  await page.getByRole('button', { name: 'Save draft' }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'could not be confirmed' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Save draft' })).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Reload workspace' })).toBeVisible();
});

test('quarantine upload requires explicit synthetic acknowledgment and shows pending inspection without release', async ({ page }) => {
  const state = await workspace(page, true); state.uploadEnabled = true;
  let received = false; let headers: Record<string, string> = {};
  const version = { id: 'version', filename: 'synthetic.xlsx', state: 'AwaitingValidation', bytes: 4, sha256: 'A'.repeat(64), uploadedAt: '2026-10-21T08:00:00Z', errorCode: null, storageMode: 'LocalQuarantineSpike', canPublish: false };
  await page.route('**/api/documents/*/uploads', async route => {
    if (route.request().method() === 'GET') return route.fulfill({ json: received ? [version] : [] });
    headers = route.request().headers(); received = true; return route.fulfill({ json: version });
  });
  await page.goto('/my-documents'); await page.getByRole('button', { name: /Synthetic owner draft/ }).click();
  await expect(page.getByRole('button', { name: 'Upload to local quarantine' })).toBeDisabled();
  await page.getByLabel('Workbook file', { exact: true }).setInputFiles({ name: 'synthetic.xlsx', mimeType: 'application/octet-stream', buffer: Buffer.from([1, 2, 3, 4]) });
  await expect(page.getByRole('button', { name: 'Upload to local quarantine' })).toBeDisabled();
  await page.getByRole('checkbox', { name: 'I confirm this workbook contains synthetic data only.' }).check();
  await page.getByRole('button', { name: 'Upload to local quarantine' }).click();
  await expect(page.getByText('File received: AwaitingValidation.', { exact: false })).toBeVisible();
  await expect(page.getByText('Ownership, content scan and structural validation: not verified. Publish blocked.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Upload to local quarantine' })).toBeDisabled(); // A new version requires a fresh deliberate file selection.
  expect(headers['if-match']).toBe('"revision-1"'); expect(headers['x-synthetic-fixture']).toBe('true');
  expect(headers['content-type']).toBe('application/octet-stream'); expect(headers['x-csrf-token']).toBe('synthetic-csrf');
  await expect(page.getByRole('button', { name: 'Publish', exact: true })).toBeDisabled();
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: 'test-results/quarantine-mobile.png', fullPage: true });
});

test('unconfirmed upload retries selected bytes with the same idempotency key', async ({ page }) => {
  const state = await workspace(page, true); state.uploadEnabled = true;
  const keys: string[] = [];
  await page.route('**/api/documents/*/uploads', async route => {
    if (route.request().method() === 'GET') return route.fulfill({ json: [] });
    keys.push(route.request().headers()['idempotency-key']); return route.fulfill({ status: 503, json: { code: 'UPLOAD_NOT_CONFIRMED_RELOAD_STATUS' } });
  });
  await page.goto('/my-documents'); await page.getByRole('button', { name: /Synthetic owner draft/ }).click();
  await page.getByLabel('Workbook file', { exact: true }).setInputFiles({ name: 'synthetic.xlsx', mimeType: 'application/octet-stream', buffer: Buffer.from([1]) });
  await page.getByRole('checkbox', { name: 'I confirm this workbook contains synthetic data only.' }).check();
  await page.getByRole('button', { name: 'Upload to local quarantine' }).click();
  await expect(page.getByText('Upload could not be confirmed.', { exact: false })).toBeVisible();
  await page.getByRole('button', { name: 'Refresh upload status' }).click();
  await page.getByRole('button', { name: 'Upload to local quarantine' }).click();
  await expect.poll(() => keys.length).toBe(2); expect(keys[0]).toBeTruthy(); expect(keys[0]).toBe(keys[1]);
  state.persona = 'guest'; await page.evaluate(() => window.dispatchEvent(new Event('focus')));
  await expect(page.getByLabel('Workbook file', { exact: true })).toHaveCount(0);
});
