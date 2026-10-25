// Live verification of HPRebar's Kata Export web window (Revit 2026) in the harness's own Revit on a model copy: the
// page served from kata.hprebar.local, the state built from the view model, the elevation drawn by the page from the
// scene, and the window's controls and gestures going back to the view model. Read-only towards the model and Excel (no
// export, no rebar generation). Usage: node live-verify-kata-web.mjs [port] [expectedBeamCount]
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
const view = () => page.evaluate(`(() => { const e = document.querySelector('[data-testid=kata-elevation]'); if (!e) return null; const r = e.getBoundingClientRect();
  return { left: r.left, top: r.top, width: r.width, height: r.height, renderer: e.dataset.renderer, scale: Number(e.dataset.scale ?? 0),
    texts: e.querySelectorAll(':scope > svg text').length, shapes: e.querySelectorAll(':scope > svg *').length }; })()`);
const waitFor = async (test, ms = 8000) => {
  const deadline = Date.now() + ms;
  let v = await view();
  while (!test(v) && Date.now() < deadline) {
    await sleep(100);
    v = await view();
  }
  return v;
};

const state = await call('kata.getState');
const s = state.payload;
check('kata.getState answers the view model state', state.ok && !!s?.header && Array.isArray(s.columns),
  state.ok ? `${s.header.beamName} · ${s.header.elementCount} · ${s.columns.length} columns · ${state.ms} ms` : state.error?.message);
if (expectedBeams !== null) check('the run read before the window has the selected beams', s?.header.elementCount.startsWith(`${expectedBeams} `), s?.header.elementCount);
check('the table columns are lettered from C', s?.columns[0]?.letter === 'C' && s.columns.every((c, i) => c.letter.length > 0 && (i === 0 || c.letter !== s.columns[i - 1].letter)));
check('the page shows the cells B3:B10 and no table of sheet cells', await page.evaluate(`document.querySelector('[aria-label="Thông số Kata"]')?.innerText.includes(${JSON.stringify(s?.header.beamName ?? '?')}) && !document.querySelector('table')`));

let v = await waitFor((x) => x?.renderer === 'native' && x.scale > 0 && x.texts > 0);
check('the page draws the elevation from the scene the host sent', v?.renderer === 'native' && v.texts > 0, `${v?.shapes ?? 0} shapes, ${v?.texts ?? 0} texts, scale ${v?.scale}`);
const removed = await call('kata.canvas', { kind: 'click', x: 10, y: 10 });
check('the WPF canvas commands are gone (kata.canvas is UNKNOWN_COMMAND)', removed.ok === false && removed.error.code === 'UNKNOWN_COMMAND');

// Column selection, from the table (view model) and from a click on the drawing (the page's hit test).
const target = Math.min(1, (s?.columns.length ?? 1) - 1);
const selected = await call('kata.update', { selectedColumn: target });
check('selecting a column updates the view model', selected.ok && selected.payload.selectedColumn === target, selected.payload?.selectionText);
await sleep(300);
check('the page shows the selection line', await page.evaluate(`document.querySelector('[data-testid=kata-selection]').innerText.includes(${JSON.stringify(s?.columns[target]?.letter ?? '?')})`));

await call('kata.update', { selectedColumn: -1 });
await page.mouse.click(v.left + v.width / 2, v.top + v.height / 2);
await sleep(500);
const afterClick = (await call('kata.getState')).payload;
check('a click on the drawing selects the column under it', afterClick.selectedColumn >= 0, `column ${afterClick.selectedColumn} · ${afterClick.selectionText}`);

const whole = (await view()).scale;
await page.mouse.wheel(v.left + v.width / 2, v.top + v.height / 2, -300);
v = await waitFor((x) => x.scale > whole * 1.2, 3000);
check('the wheel zooms the drawing in the page (no round trip)', v.scale > whole * 1.2, `scale ${whole} → ${v.scale}`);
await page.mouse.drag(v.left + v.width / 2, v.top + v.height / 2, v.left + v.width / 2 + 80, v.top + v.height / 2, { button: 'middle' });
await call('kata.run', { action: 'frameAll' });
v = await waitFor((x) => Math.abs(x.scale - whole) < whole * 0.01, 3000);
check('Toàn dải frames the run again', Math.abs(v.scale - whole) < whole * 0.01, `scale ${v.scale}`);

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

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
