// Live verification of the lifecycle paths: Revit's UI theme flipped and restored (theme.changed, page colours), the
// document closed (empty state, NO_DOCUMENT) and reopened (document.changed, numbers again). Drives only the harness's
// own Revit: CDP for the page, window messages for Options/Open (revit-options.ps1, revit-window.ps1 fill-dialog) and
// keys only through revit-window.ps1, which refuses unless that Revit is in front. The theme is a per-user Revit setting
// shared with any other Revit of the user: that step runs only with --theme (ask the user first) and always sets the
// theme back, even when a check fails. Assumes an en-US Revit (dialog, page and button names).
// Usage: node live-verify-lifecycle.mjs <port> <revitPid> <modelCopyPath> [--theme]
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const [port, revitPid] = [Number(process.argv[2] ?? 9333), Number(process.argv[3])];
const model = process.argv[4] ? path.win32.resolve(process.argv[4]) : '';
const withTheme = process.argv.includes('--theme');
if (!revitPid || !model) throw new Error('usage: node live-verify-lifecycle.mjs <port> <revitPid> <modelCopyPath> [--theme]');
const here = path.dirname(fileURLToPath(import.meta.url));
const ps = (script, ...args) =>
  execFileSync('pwsh', ['-NoProfile', '-File', path.join(here, script), '-RevitPid', String(revitPid), ...args], { encoding: 'utf8' }).trim();

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 90000, urlPart: 'toolkit.hpbim.local' });
await page.evaluate(`window.__events = []; chrome.webview.addEventListener('message', e => { if (e.data?.kind === 'event') window.__events.push(e.data); }); true`);
const call = (command, payload) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'lc-' + crypto.randomUUID();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), 150000);
  })`);
const text = async () => (await page.evaluate('document.body.innerText')).replace(/\s+/g, ' ');
const waitEvent = async (name, matches, timeoutMs) => {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const found = await page.evaluate(`window.__events.filter(e => e.event === ${JSON.stringify(name)}).map(e => e.payload)`);
    const hit = found.find(matches);
    if (hit) return hit;
    await sleep(500);
  }
  return null;
};

// --- 1. theme: flip Revit's UI theme in Options ▸ Colors, then set it back ---
const tryPs = (...args) => {
  try {
    ps(...args);
    return true;
  } catch {
    return false;
  }
};
const openOptions = async () => {
  // The File menu renders a moment after the click; when it never shows the Options button, click File again.
  for (let menu = 0; menu < 3; menu++) {
    ps('revit-uia.ps1', '-Action', 'invoke', '-Name', 'ID_ApplicationMenuButton');
    for (let poll = 0; poll < 6; poll++) {
      await sleep(1000);
      if (tryPs('revit-uia.ps1', '-Action', 'invoke', '-Name', 'Options', '-Nth', '1')) return;
    }
  }
  throw new Error('File ▸ Options not found');
};
const setTheme = async (revitName) => {
  await openOptions();
  if (ps('revit-window.ps1', '-Action', 'wait-dialog', '-Title', 'Options', '-TimeoutSec', '15') !== 'FOUND') throw new Error('Options did not open');
  ps('revit-options.ps1', '-Action', 'page', '-Page', 'Colors');
  ps('revit-options.ps1', '-Action', 'combo', '-Id', '3596', '-Item', revitName);
  ps('revit-options.ps1', '-Action', 'button', '-Button', 'OK');
};
const revitName = (theme) => (theme === 'dark' ? 'Dark' : 'Light');
if (withTheme) {
  const original = (await call('app.hello')).payload.theme;
  const flipped = original === 'dark' ? 'light' : 'dark';
  await page.evaluate('window.__events = []');
  try {
    await setTheme(revitName(flipped));
    const toFlipped = await waitEvent('theme.changed', (p) => p.theme === flipped, 15000);
    const darkClass = await page.evaluate("document.documentElement.classList.contains('dark')");
    check(`theme ${original} → ${flipped} pushes theme.changed and repaints the page`, !!toFlipped && darkClass === (flipped === 'dark'),
      `event ${JSON.stringify(toFlipped)}, html.dark=${darkClass}`);
    check('app.hello reports the new theme', (await call('app.hello')).payload?.theme === flipped);
  } finally {
    await setTheme(revitName(original));
    const restored = await waitEvent('theme.changed', (p) => p.theme === original, 15000);
    check(`theme set back to ${original}`, !!restored && (await call('app.hello')).payload?.theme === original);
  }
} else {
  console.log('SKIP theme (pass --theme after asking the user: it changes a per-user Revit setting)');
}

// --- 2. close the document: closing its last view window leaves Revit on Home ---
await page.evaluate('window.__events = []');
let closed = null;
for (let attempt = 0; attempt < 8 && !closed; attempt++) {
  ps('revit-window.ps1', '-Action', 'keys', '-Keys', '^{F4}');
  // A modified copy (Dynamo, a test edit) asks to be saved: it is a throw-away copy, never saved.
  ps('revit-window.ps1', '-Action', 'close-dialog', '-Title', 'Save File', '-Button', 'No', '-TimeoutSec', '2');
  closed = await waitEvent('document.changed', (p) => p.hasDocument === false, 4000);
}
check('closing the document pushes document.changed {hasDocument:false}', !!closed);
const noDocument = await call('dashboard.getDocumentInfo');
const noStats = await call('dashboard.getStatistics', { scope: 'model' });
check('with no document: getDocumentInfo answers hasDocument false, statistics NO_DOCUMENT',
  noDocument.ok && noDocument.payload.hasDocument === false && noStats.ok === false && noStats.error.code === 'NO_DOCUMENT');
check('page shows its empty state', (await text()).includes('Chưa mở tài liệu'));

// --- 3. reopen the copy through Open (path set by WM_SETTEXT, never typed) ---
await page.evaluate('window.__events = []');
ps('revit-window.ps1', '-Action', 'keys', '-Keys', '^o');
const filled = ps('revit-window.ps1', '-Action', 'fill-dialog', '-Title', 'Open', '-Text', model, '-Button', 'Open', '-TimeoutSec', '15');
const reopened = await waitEvent('document.changed', (p) => p.hasDocument && !!p.path && path.win32.resolve(p.path).toLowerCase() === model.toLowerCase(), 120000);
check('reopening pushes document.changed with the model', filled === 'FILLED Open' && !!reopened, filled);
await sleep(3000);
const stats = await call('dashboard.getStatistics', { scope: 'model' });
check('statistics work again and the page shows the document', stats.ok && stats.payload.total > 0 && (await text()).includes(reopened?.title ?? '?'),
  stats.ok ? `${stats.payload.total} elements` : JSON.stringify(stats.error));

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
