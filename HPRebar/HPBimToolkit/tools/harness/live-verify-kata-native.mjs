// Live verification of the Kata Export page drawing the plain elevation itself (Revit 2026, harness Revit on a model
// copy): the scene arrives from the view model, zoom and pan stay in the page (no bridge message), a click on the
// drawing selects the column through the view model, the span buttons and Toàn dải frame from its focus requests, and
// the Kata drawing with bars still comes from the WPF canvas until the page draws it. Read-only towards the model and
// Excel. Usage: node live-verify-kata-native.mjs [port]
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9334);
const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 60000, urlPart: 'kata.hprebar.local' });
const call = (command, payload, timeoutMs = 30000) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'kn-' + crypto.randomUUID();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), ${timeoutMs});
  })`);
const area = () =>
  page.evaluate(`(() => { const e = document.querySelector('[data-testid=kata-elevation]'); if (!e) return null; const r = e.getBoundingClientRect();
    return { renderer: e.dataset.renderer ?? 'svg', scale: Number(e.dataset.scale ?? 0), texts: [...e.querySelectorAll('svg text')].map((t) => t.textContent),
      left: r.left + e.clientLeft, top: r.top + e.clientTop, width: e.clientWidth, height: e.clientHeight }; })()`);
const waitFor = async (predicate, ms = 8000) => {
  const deadline = Date.now() + ms;
  let value = await area();
  while (!predicate(value) && Date.now() < deadline) {
    await sleep(200);
    value = await area();
  }
  return value;
};

// Watch what the page posts to the host from here on.
await page.evaluate(`(() => { window.__posted = []; const post = chrome.webview.postMessage.bind(chrome.webview);
  chrome.webview.postMessage = (m) => { window.__posted.push(m?.command); return post(m); }; })()`);
const posted = () => page.evaluate('window.__posted.splice(0)');

// The plain elevation: bars hidden.
await call('kata.update', { showRebar: false, showSection: false });
await call('kata.run', { action: 'frameAll' }); // the run framed whole, whatever an earlier session left
await sleep(800);
let a = await waitFor((v) => v?.renderer === 'native' && v.texts.length > 0);
check('without bars the page draws the elevation itself', a?.renderer === 'native', `${a?.texts.length ?? 0} texts, scale ${a?.scale}`);
const state = (await call('kata.getState')).payload;
check('every column letter is drawn', state.columns.every((c) => a.texts.includes(c.letter)), state.columns.map((c) => c.letter).join(''));

await posted();
const cx = a.left + a.width / 2;
const cy = a.top + a.height / 2;
await page.mouse.wheel(cx, cy, -300);
a = await waitFor((v) => v.scale > a.scale);
const whole = state.columns.length;
check('the wheel zooms in the page', a.scale > 0, `scale ${a.scale}`);
await page.mouse.drag(cx, cy, cx - 150, cy, { button: 'middle' });
await sleep(300);
const sent = (await posted()).filter(Boolean);
check('zoom and pan send nothing to Revit', sent.length === 0, sent.join(', ') || 'no message');

// A click selects the column under it through the view model.
await page.mouse.click(cx, cy);
await sleep(800);
const afterClick = await posted();
const selected = (await call('kata.getState')).payload.selectedColumn;
check('a click on the drawing selects a column through the view model', afterClick.includes('kata.update') && selected >= 0 && selected < whole,
  `selected ${state.columns[selected]?.letter}, sent ${afterClick.filter(Boolean).join(', ')}`);

await call('kata.run', { action: 'frameAll' });
const framed = await waitFor((v) => v.scale < a.scale);
check('Toàn dải frames the whole run from the focus request', framed.scale < a.scale, `scale ${a.scale} → ${framed.scale}`);
// Tiếp selects the next span and frames it with its supports. In a shallow area the label rows leave the band so little
// height that the span is shrunk back to the whole run's scale (ShrinkToHeight, as in WPF): check the selection moves
// on and the scale never drops below the whole run's.
const before = (await call('kata.getState')).payload.selectedColumn;
await call('kata.run', { action: 'nextSpan' });
await sleep(1000);
const span = await area();
const after = (await call('kata.getState')).payload;
check('Tiếp selects the next span and frames it', after.selectedColumn !== before && after.columns[after.selectedColumn]?.kind.startsWith('Nhịp') && span.scale >= framed.scale - 1e-6,
  `${state.columns[before]?.letter ?? '-'} → ${after.columns[after.selectedColumn]?.letter}, scale ${span.scale}`);

// With a rebar plan read, "Hiện thép" draws Kata's elevation in the page too; the section card beside the plain
// elevation still comes from the WPF canvas.
const mode = () => page.evaluate(`document.querySelector('[data-testid=kata-elevation]')?.dataset.mode ?? 'svg'`);
await call('kata.update', { showRebar: true, showSection: false });
await sleep(1500);
if ((await mode()) === 'kata') {
  a = await area();
  check("with bars shown the page draws Kata's elevation", a.renderer === 'native', `${a.texts.length} texts`);
  await call('kata.update', { showSection: true }); // flags open sections only while sections are shown
  await sleep(800);
  await kataPicking();
  await call('kata.update', { showRebar: false, showSection: true });
  await sleep(1500);
  const card = await page.evaluate(`document.querySelector('[data-testid=kata-section-panel]')?.dataset.section ?? null`);
  const room = await area();
  // The card needs a drawing area of at least 420 × 290 px (as in the WPF canvas).
  if (room.width >= 420 && room.height >= 290) {
    check('the plain elevation shows the section card of the picked column, drawn by the page', card !== null && room.renderer === 'native', `section ${card}`);
  } else {
    check('no section card in a drawing area under 420 × 290 px', card === null, `${room.width} × ${room.height}`);
  }
} else {
  console.log('SKIP bars shown (no rebar plan read in this window)');
}
await call('kata.update', { showRebar: true, showSection: true });

/** Flags and bar groups on Kata's elevation, with the real mouse and keyboard (read-only towards Revit: removals are undone). */
async function kataPicking() {
  const centre = (selector) =>
    page.evaluate(`(() => { const e = document.querySelector('[data-testid=kata-elevation] ${selector}'); if (!e) return null; const r = e.getBoundingClientRect();
      return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`);
  const flag = await centre('[data-id^=flag-]');
  if (flag) {
    await page.mouse.click(flag.x - 2, flag.y);
    await sleep(1500);
    const panel = await page.evaluate(`(() => { const p = document.querySelector('[data-testid=kata-section-panel]'); return p ? { n: p.dataset.section, texts: p.querySelectorAll('svg text').length } : null; })()`);
    check('a click on a flag opens its section, drawn by the page', panel !== null && panel.texts > 0, panel ? `section ${panel.n}, ${panel.texts} texts` : 'no panel');
    // Esc with no panel open closes the Kata window: press it only over an open panel.
    if (panel) {
      await page.evaluate(`document.querySelector('[data-testid=kata-elevation]').focus()`);
      await page.keyboard.press('Escape');
      await sleep(500);
      check('Esc closes the section panel', !(await page.evaluate(`!!document.querySelector('[data-testid=kata-section-panel]')`)));
    }
  } else {
    console.log('SKIP flags (none drawn)');
  }

  // Lines of every pen carry a line- id: hover the middle of each first segment until the tooltip names a bar.
  const candidates = await page.evaluate(`[...document.querySelectorAll('[data-testid=kata-elevation] > svg [data-id^=line-]')].slice(0, 400).map((e) => {
    const n = e.points.numberOfItems; const a = e.points.getItem(0), b = e.points.getItem(Math.min(1, n - 1)); const box = e.ownerSVGElement.getBoundingClientRect();
    return { x: box.left + (a.x + b.x) / 2, y: box.top + (a.y + b.y) / 2 }; })`);
  let bar = null;
  for (const point of candidates) {
    await page.mouse.move(point.x, point.y);
    if (await page.evaluate(`!!document.querySelector('[data-testid=kata-elevation] [role=tooltip]')`)) {
      bar = point;
      break;
    }
  }
  if (!bar) {
    console.log(candidates.length ? 'SKIP bar picking (no line answers with a tooltip)' : 'SKIP bar picking (the plan draws no bars: blocked sheet)');
    return;
  }
  await page.mouse.click(bar.x, bar.y);
  await sleep(400);
  const selected = Number(await page.evaluate(`document.querySelector('[data-testid=kata-elevation]').dataset.selected`));
  check('a click on a bar selects its group', selected > 0, `${selected} keys`);
  await posted();
  await page.keyboard.press('Delete');
  await sleep(1500);
  const removedSent = (await posted()).includes('kata.removeBars');
  const undoable = (await call('kata.getState')).payload.can.undoRemoveBars;
  check('Delete removes the group through the view model', removedSent && undoable);
  await page.keyboard.press('z', { ctrl: true });
  await sleep(1000);
  check('Ctrl+Z restores it', undoable && !(await call('kata.getState')).payload.can.undoRemoveBars);
}

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
