// Live verification of Kata Export's 3D tab (Revit 2026, harness Revit on a model copy): the plan's bars and concrete
// in three.js, every bar the plan counts, orbiting, the view presets, hovering and picking a bar group, Delete and
// Ctrl+Z through the view model (the plan only — nothing reaches the model or Excel), back to the elevation.
// Usage: node live-verify-kata-3d.mjs [port] [screenshot.png]
import { writeFileSync } from 'node:fs';
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9334);
const shot = process.argv[3];
const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 60000, urlPart: 'kata.hprebar.local' });
const call = (command, payload, timeoutMs = 60000) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'k3-' + crypto.randomUUID();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), ${timeoutMs});
  })`);
const q = (expression) => page.evaluate(expression);
const view = () =>
  q(`(() => { const e = document.querySelector('[data-testid=kata-3d]'); if (!e) return null; const r = e.getBoundingClientRect();
    return { groups: Number(e.dataset.groups), instances: Number(e.dataset.instances), selected: Number(e.dataset.selected), renderMs: Number(e.dataset.renderMs),
      canvas: !!e.querySelector('canvas'), alert: e.querySelector('[role=alert]')?.textContent ?? null, tooltip: document.querySelector('[data-testid=kata-3d-tooltip]')?.textContent ?? null,
      left: r.left, top: r.top, width: r.width, height: r.height }; })()`);
const waitView = async (predicate, ms = 15000) => {
  const deadline = Date.now() + ms;
  let v = await view();
  while (!(v && predicate(v)) && Date.now() < deadline) {
    await sleep(250);
    v = await view();
  }
  return v;
};
const clickTab = (name) => q(`(() => { const t = [...document.querySelectorAll('[role=tab]')].find((b) => b.textContent.trim() === ${JSON.stringify(name)}); if (!t || t.disabled) return false; t.click(); return true; })()`);
const clickTool = (label) => q(`(() => { const b = [...document.querySelectorAll('[data-testid=kata-3d-tools] button')].find((e) => e.textContent.trim() === ${JSON.stringify(label)}); if (!b) return false; b.click(); return true; })()`);

await call('kata.update', { showRebar: true });
await sleep(800);
const model = await call('kata.model3d');
check('the host builds the plan in 3D', model.ok === true && model.payload.groups.length > 0, model.ok ? `${model.payload.groups.length} groups, ${model.payload.barCount} bars, ${model.payload.concrete.length} boxes` : JSON.stringify(model.error));
if (!model.ok) process.exit(1);
const barCount = model.payload.barCount;

check('the 3D tab opens', await clickTab('3D'));
let v = await waitView((x) => x.canvas || x.alert);
check('three.js draws in WebView2 (WebGL)', !!v?.canvas && !v.alert, v?.alert ?? `${v?.width}×${v?.height}`);
check('every bar the plan counts is drawn', v?.instances === barCount, `${v?.instances}/${barCount}`);

// Orbit: 20 drags, the render time of each frame read back.
const cx = v.left + v.width / 2;
const cy = v.top + v.height / 2;
const times = [];
for (let i = 0; i < 20; i++) {
  await page.mouse.drag(cx, cy, cx + 30, cy + (i % 2 ? 8 : -8), { steps: 3 });
  times.push((await view()).renderMs);
}
times.sort((a, b) => a - b);
const median = times[Math.floor(times.length / 2)];
check('orbiting renders each frame in under 16 ms (CPU side, median)', median < 16, `median ${median.toFixed(1)} ms, max ${times.at(-1).toFixed(1)} ms`);

for (const preset of ['Mặt đứng', 'Mặt bằng', 'Đầu dầm', 'Phối cảnh']) {
  const ok = await clickTool(preset);
  await sleep(200);
  check(`preset ${preset}`, ok);
}

// Hover then pick: sweep the front view's middle rows until a tooltip names a bar group.
await clickTool('Mặt đứng');
await sleep(300);
v = await view();
let picked = null;
outer: for (const fy of [0.5, 0.45, 0.55, 0.4, 0.6, 0.35, 0.65]) {
  for (let fx = 0.1; fx <= 0.9; fx += 0.02) {
    const x = v.left + v.width * fx;
    const y = v.top + v.height * fy;
    await page.mouse.move(x, y);
    const t = (await view()).tooltip;
    if (t && /Thép chủ|gia cường|Đai kín/.test(t)) {
      picked = { x, y, tooltip: t };
      break outer;
    }
  }
}
check('hovering a bar names it', !!picked, picked?.tooltip ?? 'no tooltip on the sampled rows');
if (picked) {
  await page.mouse.click(picked.x, picked.y);
  v = await waitView((x) => x.selected > 0, 3000);
  check('a click selects its group', v.selected > 0, `${v.selected} key(s)`);
  await page.keyboard.press('Delete');
  v = await waitView((x) => x.instances < barCount, 15000);
  check('Delete strikes the group off the plan (3D rebuilt from the new plan)', v.instances < barCount, `${barCount} → ${v.instances}`);
  await page.keyboard.press('z', { ctrl: true });
  v = await waitView((x) => x.instances === barCount, 15000);
  check('Ctrl+Z brings it back', v.instances === barCount, `${v.instances}/${barCount}`);
}

if (shot) {
  await clickTool('Phối cảnh');
  await page.mouse.move(v.left + 5, v.top + 5);
  await sleep(500);
  writeFileSync(shot, Buffer.from(await page.screenshot(), 'base64'));
  console.log(`screenshot ${shot}`);
  // A close-up of the first support: bars, stirrups and the column through the translucent concrete.
  await page.mouse.wheel(v.left + v.width * 0.2, v.top + v.height * 0.6, -1500);
  await sleep(500);
  const detail = shot.replace(/\.png$/, '-detail.png');
  writeFileSync(detail, Buffer.from(await page.screenshot(), 'base64'));
  console.log(`screenshot ${detail}`);
}

await q(`document.querySelector('[data-testid=kata-3d]').focus()`);
check('back to the elevation', await clickTab('2D'));
await sleep(800);
check('the elevation is drawn again', await q(`!!document.querySelector('[data-testid=kata-elevation]')`));

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
