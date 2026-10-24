// Live verification of the event and failure paths the protocol checks cannot reach: a modal Revit dialog during a
// request (TIMEOUT, then recovery), a view switch (document.changed), an edit (model.stale, refresh, undo).
// Drives only the harness's own Revit: toolkit commands over CDP plus revit-window.ps1 scoped to its pid.
// Usage: node live-verify-events.mjs <port> <revitPid>
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const [port, revitPid] = [Number(process.argv[2] ?? 9333), Number(process.argv[3])];
if (!revitPid) throw new Error('usage: node live-verify-events.mjs <port> <revitPid>');
const windowScript = path.join(path.dirname(fileURLToPath(import.meta.url)), 'revit-window.ps1');
const revitWindow = (...args) =>
  execFileSync('pwsh', ['-NoProfile', '-File', windowScript, '-RevitPid', String(revitPid), ...args], { encoding: 'utf8' }).trim();

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 90000, urlPart: 'toolkit.hpbim.local' });
await page.evaluate(`window.__events = []; chrome.webview.addEventListener('message', e => { if (e.data?.kind === 'event') window.__events.push(e.data); }); true`);
const events = (name) => page.evaluate(`window.__events.filter(e => e.event === ${JSON.stringify(name)})`);
const call = (command, payload) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'ev-' + crypto.randomUUID();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), 150000);
  })`);
const text = () => page.evaluate('document.body.innerText');

// --- 1. modal dialog: openTool opens Project Units; a request meanwhile times out; closing it recovers ---
const opened = await call('dashboard.openTool', { toolId: 'revit.projectUnits' });
check('openTool posts a Revit command', opened.ok && opened.payload.opened === true);
const dialog = revitWindow('-Action', 'wait-dialog', '-Title', 'Project Units', '-TimeoutSec', '15');
check('Project Units dialog opened in Revit', dialog === 'FOUND', dialog);

const started = Date.now();
const blocked = await call('dashboard.getSelection');
const blockedMs = Date.now() - started;
check('request during the modal dialog answers TIMEOUT from the host (~30 s)', blocked.ok === false && blocked.error.code === 'TIMEOUT' && blockedMs >= 29000 && blockedMs < 34000,
  `${blocked.error?.code} after ${blockedMs} ms`);

console.log(`  ${revitWindow('-Action', 'close-dialog', '-Title', 'Project Units', '-Button', 'Cancel')}`);
await sleep(1500);
const recovered = await call('dashboard.getSelection');
check('after closing the dialog requests work again', recovered.ok === true, JSON.stringify(recovered.payload ?? recovered.error));

// --- 2. view switch: the "3D mặc định" quick tool activates {3D} (the prepared model is saved in a plan view) ---
const before = await call('dashboard.getDocumentInfo');
const stats = await call('dashboard.getStatistics', { scope: 'model' });
await page.evaluate('window.__events = []');
const to3d = await call('dashboard.openTool', { toolId: 'revit.default3DView' });
await sleep(4000);
const documentEvents = await events('document.changed');
const lastView = documentEvents.at(-1)?.payload.activeView;
check('view switch pushes document.changed with the new active view', to3d.ok && !!lastView && lastView.id !== before.payload.activeView?.id,
  `${before.payload.activeView?.name} → ${lastView?.name} (${documentEvents.length} events)`);
await sleep(1500);
check('UI header shows the new view', !!lastView && (await text()).includes(lastView.name));

// Zoom to elements the active (3D) view cannot show: must answer at once without Revit's modal
// "searching through the closed views… Continue?" dialog, which used to block every request.
const annotation = stats.payload.categories.find((c) => ['Generic Annotations', 'Dimensions', 'Elevations'].includes(c.name));
const zoomStarted = Date.now();
const zoomHidden = await call('dashboard.showElements', { categoryId: annotation?.categoryId, mode: 'zoom' });
const zoomMs = Date.now() - zoomStarted;
const noDialog = revitWindow('-Action', 'wait-dialog', '-Title', 'Revit', '-TimeoutSec', '2');
check('zoom to elements not in the active view selects them without a blocking dialog', zoomHidden.ok && zoomHidden.payload.zoomed === 0 &&
  zoomHidden.payload.shown === annotation.count && zoomMs < 5000 && noDialog === 'NOT_FOUND',
  `${annotation?.name}: shown ${zoomHidden.payload?.shown}, zoomed ${zoomHidden.payload?.zoomed}, ${zoomMs} ms, dialog ${noDialog}`);

// --- 3. edit: delete selected elements in Revit → model.stale → refresh clears it → undo marks stale again ---
await call('dashboard.getStatistics', { scope: 'model' }); // fresh baseline (resets the change tally)
const victim = stats.payload.categories.find((c) => c.name === 'Generic Models') ?? stats.payload.categories.at(-1);
const selected = await call('dashboard.showElements', { categoryId: victim.categoryId, mode: 'select', scope: 'model' });
await page.evaluate('window.__events = []');
console.log(`  ${revitWindow('-Action', 'keys', '-Keys', '{DELETE}')}`);
await sleep(2000);
const stale = (await events('model.stale')).at(-1)?.payload;
check('deleting in Revit pushes model.stale with the deleted count', stale?.deleted >= selected.payload.shown, `${victim.name}: selected ${selected.payload.shown}, stale ${JSON.stringify(stale)}`);
check('UI shows the stale badge after an edit', (await text()).includes('Dữ liệu cũ'));

await page.evaluate("[...document.querySelectorAll('button')].find(b => b.textContent.includes('Làm mới'))?.click(); true");
await sleep(3000);
check('Làm mới clears the stale badge', !(await text()).includes('Dữ liệu cũ'));
const afterDelete = await call('dashboard.getStatistics', { scope: 'model' });
check('refreshed statistics no longer count the deleted category elements',
  !afterDelete.payload.categories.some((c) => c.categoryId === victim.categoryId && c.count >= victim.count), `total ${stats.payload.total} → ${afterDelete.payload.total}`);

await page.evaluate('window.__events = []');
console.log(`  ${revitWindow('-Action', 'keys', '-Keys', '^z')}`);
await sleep(2500);
const undone = (await events('model.stale')).at(-1)?.payload;
check('undo in Revit pushes model.stale again', !!undone && undone.added + undone.modified + undone.deleted > 0, JSON.stringify(undone));
const badge = await text();
check('UI shows the stale badge after the undo', badge.includes('Dữ liệu cũ'));

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
