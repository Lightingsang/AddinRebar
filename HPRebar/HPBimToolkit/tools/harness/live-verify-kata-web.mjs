// Live verification of HPRebar's Kata Export web window (Revit 2026, HPREBAR_KATA_UI=web) in the harness's own Revit
// on a model copy: the page served from kata.hprebar.local, the state built from the WPF view model, the elevation drawn
// by the WPF canvas and received as SVG, and the window's controls going back to the view model. Read-only towards the
// model and Excel (no export, no rebar generation). Usage: node live-verify-kata-web.mjs [port] [expectedBeamCount]
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9334);
const expectedBeams = process.argv[3] ? Number(process.argv[3]) : null;
const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 60000, urlPart: 'kata.hprebar.local' });
check('page served from the embedded virtual host', page.url.startsWith('https://kata.hprebar.local/kata.html'), page.url);

const call = (command, payload, timeoutMs = 30000) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'kw-' + crypto.randomUUID(); const started = performance.now();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve({ ...e.data, ms: Math.round(performance.now() - started) }); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), ${timeoutMs});
  })`);
const revision = () => page.evaluate(`Number(document.querySelector('[data-testid=kata-elevation]')?.dataset.revision ?? 0)`);
const svgText = () => page.evaluate(`document.querySelector('[data-testid=kata-elevation] svg')?.outerHTML ?? ''`);
const waitForRevisionAfter = async (before, ms = 8000) => {
  const deadline = Date.now() + ms;
  while (Date.now() < deadline) {
    const now = await revision();
    if (now > before) return now;
    await sleep(100);
  }
  return before;
};

const state = await call('kata.getState');
const s = state.payload;
check('kata.getState answers the view model state', state.ok && !!s?.header && Array.isArray(s.columns),
  state.ok ? `${s.header.beamName} · ${s.header.elementCount} · ${s.columns.length} columns · ${state.ms} ms` : state.error?.message);
if (expectedBeams !== null) check('the run read before the window has the selected beams', s?.header.elementCount.startsWith(`${expectedBeams} `), s?.header.elementCount);
check('the table columns are lettered from C', s?.columns[0]?.letter === 'C' && s.columns.every((c, i) => c.letter.length > 0 && (i === 0 || c.letter !== s.columns[i - 1].letter)));
check('the page shows the header and the table', await page.evaluate(`document.body.innerText.includes(${JSON.stringify(s?.header.beamName ?? '?')}) && !!document.querySelector('table')`));

await sleep(1500);
const svg = await svgText();
check('the elevation arrives as SVG painted by the WPF canvas', svg.startsWith('<svg') && svg.includes('<path') && svg.includes('<text'),
  `${svg.length} chars, ${(svg.match(/<path/g) ?? []).length} paths, ${(svg.match(/<text/g) ?? []).length} texts`);
const box = await page.evaluate(`(() => { const r = document.querySelector('[data-testid=kata-elevation]').getBoundingClientRect(); return { w: Math.round(r.width), h: Math.round(r.height) }; })()`);
const viewBox = /viewBox="0 0 (\d+(?:\.\d+)?) (\d+(?:\.\d+)?)"/.exec(svg);
check('the drawing is painted at the drawing area size', viewBox && Math.abs(Number(viewBox[1]) - box.w) <= 2 && Math.abs(Number(viewBox[2]) - box.h) <= 2,
  `area ${box.w}x${box.h}, svg ${viewBox?.[1]}x${viewBox?.[2]}`);

// Column selection, from the table (view model) and from a click on the drawing (WPF canvas hit test).
const target = Math.min(1, (s?.columns.length ?? 1) - 1);
let before = await revision();
const selected = await call('kata.update', { selectedColumn: target });
check('selecting a column updates the view model', selected.ok && selected.payload.selectedColumn === target, selected.payload?.selectionText);
check('the selection repaints the drawing', (await waitForRevisionAfter(before)) > before);
await sleep(300);
check('the page shows the selection line', await page.evaluate(`document.querySelector('[data-testid=kata-selection]').innerText.includes(${JSON.stringify(s?.columns[target]?.letter ?? '?')})`));

const cleared = await call('kata.update', { selectedColumn: -1 });
const clicked = await call('kata.canvas', { kind: 'click', x: Math.round(box.w / 2), y: Math.round(box.h / 2) });
await sleep(400);
const afterClick = (await call('kata.getState')).payload;
check('a click on the drawing selects the column under it (WPF hit test)', cleared.ok && clicked.ok && afterClick.selectedColumn >= 0,
  `column ${afterClick.selectedColumn} · ${afterClick.selectionText}`);

before = await revision();
const zoomed = await call('kata.canvas', { kind: 'zoom', x: Math.round(box.w / 2), y: Math.round(box.h / 2), notches: 2 });
check('wheel zoom repaints the drawing', zoomed.ok && zoomed.payload.handled && (await waitForRevisionAfter(before)) > before, `${zoomed.ms} ms`);
before = await revision();
const panned = await call('kata.canvas', { kind: 'pan', x: 100, y: 100, dx: 80, dy: 0 });
check('pan repaints the drawing', panned.ok && (await waitForRevisionAfter(before)) > before);
before = await revision();
await call('kata.run', { action: 'frameAll' });
check('Toàn dải frames the run again', (await waitForRevisionAfter(before)) > before);

// Span numbers follow the direction (the first span is "Nhịp 1" either way), so kinds are compared without them.
const kinds = (columns) => columns.map((c) => c.kind.replace(/\d+/, 'n'));
const reversedBefore = kinds((await call('kata.getState')).payload.columns);
const reversed = await call('kata.update', { reverse: true });
const reversedKinds = kinds(reversed.payload?.columns ?? []);
check('Đảo chiều rebuilds the sheet reversed', reversed.ok && reversed.payload.isReverse && JSON.stringify(reversedKinds) === JSON.stringify([...reversedBefore].reverse()));
await call('kata.update', { reverse: false });

// Without bars read from Excel the layers change nothing on the drawing (nothing is re-sent); the flags still round-trip.
const layers = await call('kata.update', { showRebar: false, showBarTags: false, showSection: false });
check('layer toggles reach the view model', layers.ok && !layers.payload.showRebar && !layers.payload.showBarTags && !layers.payload.showSection);
await call('kata.update', { showRebar: true, showBarTags: true, showSection: true });

const badColumn = await call('kata.update', { selectedColumn: 999 });
check('a column outside the table is BAD_REQUEST', badColumn.ok === false && badColumn.error.code === 'BAD_REQUEST');
const badAction = await call('kata.run', { action: 'formatDisk' });
check('an unknown action is BAD_REQUEST', badAction.ok === false && badAction.error.code === 'BAD_REQUEST');
const key = await call('kata.canvas', { kind: 'key', key: 'Escape' });
check('Esc with nothing open is not handled (the window keeps it)', key.ok && key.payload.handled === false);

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
