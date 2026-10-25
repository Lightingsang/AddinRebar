// Live verification of the Kata Export page's own choices (Revit 2026, harness Revit on a model copy): the footer
// commands in order with no Close button, the VI/EN switch and the light/dark switch. Both choices are kept in the
// WebView2 profile the user's own Revit shares, so the script starts from Vietnamese + Revit's theme and puts the
// user's choices back (and reloads) before it ends.
// Read-only towards the model and Excel (it never presses a footer command).
// Usage: node live-verify-kata-prefs.mjs [port] [screenshotDir]
import { writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9334);
const shots = process.argv[3];
const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const KEYS = ['hp.kata.lang', 'hp.kata.theme'];
const first = await connectToToolkitPage(port, { timeoutMs: 60000, urlPart: 'kata.hprebar.local' });
const saved = await first.evaluate(`${JSON.stringify(KEYS)}.map((k) => localStorage.getItem(k))`);
await first.evaluate(`${JSON.stringify(KEYS)}.forEach((k) => localStorage.removeItem(k)); location.reload(); true`).catch(() => {});
first.close();
await sleep(3000);
const page = await connectToToolkitPage(port, { timeoutMs: 30000, urlPart: 'kata.hprebar.local' });
const q = (expression) => page.evaluate(expression);
const clickAt = async (selector) => {
  const at = await q(
    `(() => { const e = document.querySelector(${JSON.stringify(selector)}); if (!e) return null; const r = e.getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`,
  );
  if (at) await page.mouse.click(at.x, at.y);
  return at !== null;
};
const shoot = async (name) => shots && writeFileSync(join(shots, name), Buffer.from(await page.screenshot(), 'base64'));
const heading = () => q(`document.querySelector('h1')?.textContent ?? ''`);
const actions = () => q(`[...document.querySelectorAll('[data-testid=kata-actions] button')].map((b) => b.textContent.trim())`);
const dark = () => q(`document.documentElement.classList.contains('dark')`);

try {
  const vi = await actions();
  check('the footer reads Xuất Excel, Đọc thép Excel, Tạo thép Revit from left to right', vi.join(' | ') === 'Xuất Excel | Đọc thép Excel | Tạo thép Revit', vi.join(' | '));
  const extra = await q(`[...document.querySelectorAll('button')].filter((b) => b.textContent.trim() === 'Đóng').length + (document.body.textContent.includes('HPRebar Kata Pro') ? 10 : 0)`);
  check('no Đóng button and no product badge', extra === 0, String(extra));

  const startDark = await dark();
  const fillOf = () => q(`document.querySelector('[data-testid=kata-elevation]')?.style.background ?? ''`);
  const startFill = await fillOf();
  await clickAt('[data-testid=kata-theme-toggle]');
  await sleep(500);
  check('the theme switch turns the page to the other theme', (await dark()) === !startDark, `dark ${startDark} → ${await dark()}`);
  const fill = await fillOf();
  check('the drawing follows the page theme', fill !== '' && fill !== startFill, `${startFill} → ${fill}`);

  await clickAt('[role=group][aria-label="Ngôn ngữ"] button:nth-of-type(2)');
  await sleep(500);
  const en = await actions();
  check('EN turns the page into English', (await heading()) === 'BEAM EXPORT TO KATA EXCEL' && en.join(' | ') === 'Export to Excel | Read rebar from Excel | Create rebar in Revit', `${await heading()} · ${en.join(' | ')}`);
  await shoot('live-v6-en.png');

  await clickAt('[role=group][aria-label="Language"] button:nth-of-type(1)');
  await clickAt('[data-testid=kata-theme-toggle]');
  await sleep(500);
  check('VI and the theme switch bring the page back', (await heading()) === 'XUẤT DẦM SANG KATA EXCEL' && (await dark()) === startDark, await heading());
  await shoot('live-v6-vi.png');
} finally {
  // The WebView2 profile is the user's: put back what they had chosen (nothing, when they had chosen nothing).
  const restore = KEYS.map((k, i) => (saved[i] === null ? `localStorage.removeItem('${k}')` : `localStorage.setItem('${k}', ${JSON.stringify(saved[i])})`)).join('; ');
  await q(`${restore}; location.reload(); true`).catch(() => {});
  await sleep(3000);
}

page.close();
const after = await connectToToolkitPage(port, { timeoutMs: 30000, urlPart: 'kata.hprebar.local' });
const left = await after.evaluate(`${JSON.stringify(KEYS)}.map((k) => localStorage.getItem(k))`);
check("the user's own choices are back as they were", JSON.stringify(left) === JSON.stringify(saved), `${JSON.stringify(saved)} → ${JSON.stringify(left)}`);
after.close();
await sleep(200); // let the sockets close before exiting (libuv asserts on a handle closing at exit)

const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
