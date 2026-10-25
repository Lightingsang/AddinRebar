// Live verification of the web-only tools over the Kata Export drawing (Revit 2026, harness Revit on a model copy):
// minimap, layers, measure, find a bar, and the export menu (the last step only opens the host's Save dialog; the
// PowerShell caller checks it appeared and closes it, nothing is written). Read-only towards the model and Excel.
// Usage: node live-verify-kata-tools.mjs [port] [--export]
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9334);
const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 60000, urlPart: 'kata.hprebar.local' });
const call = (command, payload) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'kt-' + crypto.randomUUID();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), 30000);
  })`);
const q = (expression) => page.evaluate(expression);
const centre = (selector) =>
  q(`(() => { const e = document.querySelector(${JSON.stringify(selector)}); if (!e) return null; const r = e.getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2, w: r.width }; })()`);
const clickText = (text) =>
  q(`(() => { const b = [...document.querySelectorAll('[data-testid=kata-tools] button')].find((e) => e.textContent.trim().startsWith(${JSON.stringify(text)})); if (!b) return false; b.click(); return true; })()`);

await call('kata.update', { showRebar: true, showSection: false });
await call('kata.run', { action: 'frameAll' });
await sleep(1500);
const mode = await q(`document.querySelector('[data-testid=kata-elevation]')?.dataset.mode`);
check("Kata's elevation is shown (a rebar plan was read)", mode === 'kata', mode);

// Minimap: a click on it moves the view (drawing offset changes, scale kept).
const map = await centre('[data-testid=kata-minimap]');
const viewBefore = await q(`document.querySelector('[data-testid=kata-minimap-view]')?.getAttribute('x')`);
if (map) {
  await page.mouse.wheel(map.x - 400, map.y - 120, -600); // zoom in first, so there is somewhere to go
  await sleep(500);
  await page.mouse.click(map.x + map.w / 2 - 12, map.y);
  await sleep(500);
  const viewAfter = await q(`document.querySelector('[data-testid=kata-minimap-view]')?.getAttribute('x')`);
  check('the minimap shows the view and a click moves it there', viewAfter !== viewBefore, `x ${viewBefore} → ${viewAfter}`);
} else {
  check('the minimap shows the view and a click moves it there', false, 'no minimap');
}
await call('kata.run', { action: 'frameAll' });
await sleep(800);

// Layers: turning the concrete off removes its lines.
const lines = () => q(`document.querySelectorAll('[data-testid=kata-elevation] > svg polyline').length`);
const before = await lines();
await clickText('Lớp');
await q(`[...document.querySelectorAll('[data-testid=kata-layers] label')].find((l) => l.textContent.includes('Bê tông'))?.querySelector('input').click()`);
await sleep(500);
const after = await lines();
check('the layer panel hides the concrete lines', after < before, `${before} → ${after} polylines`);
await q(`[...document.querySelectorAll('[data-testid=kata-layers] label')].find((l) => l.textContent.includes('Bê tông'))?.querySelector('input').click()`);
await clickText('Lớp');

// Measure: two clicks, a reading in millimetres.
const area = await centre('[data-testid=kata-elevation]');
await clickText('Đo');
await page.mouse.click(area.x - 200, area.y);
await page.mouse.click(area.x + 200, area.y);
await sleep(400);
const reading = await q(`[...document.querySelectorAll('[data-testid=kata-elevation] > svg text')].map((t) => t.textContent).find((t) => t.startsWith('Δx')) ?? ''`);
check('the measure tool reads Δx, Δz and the length', /^Δx [\d.]+ · Δz [\d.]+ · L [\d.]+ mm$/.test(reading), reading);
await q(`document.querySelector('[data-testid=kata-elevation]').focus()`);
await page.keyboard.press('Escape');
await page.keyboard.press('Escape');
check('Esc leaves the measure tool (and keeps the window)', !(await q(`!!document.querySelector('[data-testid=kata-elevation]')?.dataset.measuring`)) && !!(await centre('[data-testid=kata-elevation]')));

// Find: a bar number of a tag on screen, or none when the plan draws no tags.
const number = await q(`(() => { const t = [...document.querySelectorAll('[data-testid=kata-elevation] > svg text')].map((e) => e.textContent).find((s) => /^\\d+$/.test(s)); return t ?? null; })()`);
if (number) {
  await q(`(() => { const i = document.querySelector('[aria-label="Tìm số hiệu thanh"]'); const set = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set; set.call(i, ${JSON.stringify(number)}); i.dispatchEvent(new Event('input', { bubbles: true })); i.focus(); })()`);
  await page.keyboard.press('Enter');
  await sleep(600);
  const result = await q(`document.querySelector('[data-testid=kata-find-result]')?.textContent ?? ''`);
  check('finding a bar number frames its tags', /^\d+\/\d+$/.test(result) || result.startsWith('Không có'), `${number}: ${result}`);
} else {
  console.log('SKIP find (no bar numbers drawn: blocked sheet)');
}

// Export: only opens the host's Save dialog (the caller checks it and closes it).
if (process.argv.includes('--export')) {
  await clickText('Xuất hình');
  await sleep(200);
  const started = await q(`(() => { const b = [...document.querySelectorAll('[data-testid=kata-tools] button')].find((e) => e.textContent.startsWith('SVG')); if (!b) return false; b.click(); return true; })()`);
  check('the export menu offers SVG and starts the download', started);
}

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
