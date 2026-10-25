// Live verification of the rebar path of HPRebar's Kata Export web window, in the harness's own Revit on a model copy:
// "Đọc thép Excel" (reads sheet Dam of the active Excel workbook — read only), Kata's elevation with bars and tags,
// a bar group picked on the drawing, removed with Delete and restored with Ctrl+Z, then (with --generate) "Tạo thép
// Revit", which writes rebar into the MODEL COPY. Never presses "Xuất Excel" (it would write into the user's workbook).
// --generate needs --run-folder <dir>: the open document (asked through the toolkit pane on --toolkit-port, default 9333)
// must lie in that folder, or nothing is generated.
// Usage: node live-verify-kata-web-rebar.mjs [port] [--generate --run-folder <dir> [--toolkit-port 9333]]
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9334);
const generate = process.argv.includes('--generate');
const option = (name) => {
  const index = process.argv.indexOf(name);
  return index > 0 ? process.argv[index + 1] : undefined;
};
const runFolder = option('--run-folder');
const toolkitPort = Number(option('--toolkit-port') ?? 9333);
const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 30000, urlPart: 'kata.hprebar.local' });
const call = (command, payload, timeoutMs = 30000) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'kr-' + crypto.randomUUID();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), ${timeoutMs});
  })`);
const state = async () => (await call('kata.getState')).payload;
const svg = () => page.evaluate(`document.querySelector('[data-testid=kata-elevation] > svg')?.outerHTML ?? ''`);
const view = () => page.evaluate(`(() => { const e = document.querySelector('[data-testid=kata-elevation]'); if (!e) return null; const r = e.getBoundingClientRect();
  return { left: r.left, top: r.top, width: r.width, height: r.height, selected: Number(e.dataset.selected ?? 0), tooltip: e.querySelector('[role=tooltip]')?.innerText ?? null }; })()`);
const waitIdle = async (ms) => {
  const deadline = Date.now() + ms;
  let s = await state();
  while (s.isBusy && Date.now() < deadline) {
    await sleep(500);
    s = await state();
  }
  return s;
};

const plainSvg = await svg();
const run = await call('kata.run', { action: 'loadRebar' });
const loaded = await waitIdle(120000);
check('Đọc thép Excel reads sheet Dam and plans the bars', run.ok && !loaded.isBusy && !loaded.hasError, loaded.status);
check('Tạo thép Revit becomes available after a plan without blocking items', loaded.can.generateRebar, `blocking: ${loaded.warnings.filter((w) => w.startsWith('[Chặn]')).length}`);

await sleep(1500);
const barsSvg = await svg();
check("Kata's elevation with bars replaces the plain one", barsSvg.length > plainSvg.length * 1.3 && barsSvg !== plainSvg,
  `${plainSvg.length} → ${barsSvg.length} chars, ${(barsSvg.match(/<text/g) ?? []).length} texts`);

await call('kata.update', { showBarTags: false });
await sleep(800);
const noTags = await svg();
check('Hiện số hiệu off removes the bar tags', noTags.length < barsSvg.length, `${barsSvg.length} → ${noTags.length} chars`);
await call('kata.update', { showBarTags: true });
await sleep(800);

// Hover then pick: sweep the drawing's middle band until the tooltip names a bar group (the page hit-tests its lines).
const area = await view();
let picked = null;
outer: for (let fy = 0.2; fy < 0.8; fy += 0.015) {
  for (const fx of [0.3, 0.5, 0.7]) {
    const x = area.left + area.width * fx;
    const y = area.top + area.height * fy;
    await page.mouse.move(x, y);
    const tooltip = (await view())?.tooltip;
    if (tooltip) {
      picked = { x, y, tooltip };
      break outer;
    }
  }
}
check('hovering a bar names it in a tooltip', picked !== null, picked ? picked.tooltip.replace(/\n/g, ' · ') : 'no bar found in the scanned band');

if (picked) {
  await page.mouse.click(picked.x, picked.y);
  await sleep(300);
  const selected = (await view()).selected;
  check('a click on a bar selects its group', selected > 0, `${selected} key(s)`);
  const before = await state();
  await page.keyboard.press('Delete');
  await sleep(800);
  const after = await state();
  check('Delete removes the selected group through the view model', after.can.undoRemoveBars && after.status.includes('Đã xóa'), after.status);
  await page.keyboard.press('z', { ctrl: true });
  await sleep(800);
  const undone = await state();
  check('Ctrl+Z restores it', !undone.can.undoRemoveBars && undone.status.includes('hoàn tác'), undone.status);
  check('the plan after undo matches the plan before the removal', undone.warnings.length === before.warnings.length);
}

/** Path of the document open in the harness's Revit, from the toolkit pane (null when it cannot be asked). */
async function openDocumentPath() {
  const toolkit = await connectToToolkitPage(toolkitPort, { timeoutMs: 10000, urlPart: 'toolkit.hpbim.local' });
  const info = await toolkit.evaluate(`new Promise((resolve) => {
    const id = 'kp-' + crypto.randomUUID();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: 'dashboard.getDocumentInfo', payload: null });
    setTimeout(() => resolve(null), 10000);
  })`);
  toolkit.close();
  return info?.ok ? info.payload.path : null;
}

const isInRunFolder = (file) =>
  !!runFolder && !!file && file.toLowerCase().startsWith(runFolder.toLowerCase().replace(/[\\/]*$/, '\\'));

if (generate && !isInRunFolder(await openDocumentPath().catch(() => null))) {
  check('Tạo thép Revit only on a model copy in --run-folder', false, `refused: open document not under ${runFolder ?? '(no --run-folder)'}`);
} else if (generate) {
  const started = await call('kata.run', { action: 'generateRebar' });
  const done = await waitIdle(600000);
  check('Tạo thép Revit writes the bars into the model copy', started.ok && !done.hasError, done.status);
} else {
  console.log('SKIP generateRebar (pass --generate on a model copy)');
}

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
