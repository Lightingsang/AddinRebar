// Live verification of the Kata Settings window (Revit 2026, the harness's own Revit on a model copy, started with
// HPREBAR_KATA_DEBUG_PORT): opened from HPRebar ▸ Rebar ▸ Kata Settings by UI Automation, driven over CDP.
// The settings file %AppData%\HPRebar\KataSettings.json is the user's: it is copied aside first and put back (or
// removed, when there was none) before the script ends, whatever happens.
// Usage: node live-verify-kata-settings.mjs <revitPid> [port] [screenshotDir]
import { spawn } from 'node:child_process';
import { copyFileSync, existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const revitPid = Number(process.argv[2]);
const port = Number(process.argv[3] ?? 9334);
const shots = process.argv[4];
if (!revitPid) throw new Error('usage: node live-verify-kata-settings.mjs <revitPid> [port] [screenshotDir]');

const here = dirname(fileURLToPath(import.meta.url));
const settingsFile = join(process.env.APPDATA, 'HPRebar', 'KataSettings.json');
const backup = `${settingsFile}.live-verify-${Date.now()}`;
const hadFile = existsSync(settingsFile);
if (hadFile) copyFileSync(settingsFile, backup);
let restored = false;
function restoreSettings() {
  if (restored) return;
  restored = true;
  if (hadFile) {
    copyFileSync(backup, settingsFile);
    rmSync(backup, { force: true });
  } else rmSync(settingsFile, { force: true });
  console.log(hadFile ? 'settings file restored' : 'settings file removed (there was none before)');
}
process.on('exit', restoreSettings);
process.on('SIGINT', () => process.exit(130));

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const settingsPages = async () => {
  try {
    const pages = await (await fetch(`http://127.0.0.1:${port}/json`)).json();
    return pages.filter((p) => p.type === 'page' && p.url.includes('kata.hprebar.local/settings.html'));
  } catch {
    return [];
  }
};

// A plain child (not detached: a detached console child sometimes never ran its walk); its lines are echoed so a
// ribbon that could not be clicked says so.
const uia = (args) => {
  const child = spawn('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', join(here, 'revit-uia.ps1'), '-RevitPid', String(revitPid), '-Action', 'invoke', ...args], {
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
  });
  const echo = (data) => String(data).split(/\r?\n/).filter(Boolean).forEach((line) => console.log(`  uia: ${line}`));
  child.stdout.on('data', echo);
  child.stderr.on('data', echo);
  return child;
};

/**
 * HPRebar ▸ Rebar ▸ Kata Settings, with the HPRebar tab already selected (start-revit-with-toolkit.ps1 leaves it so):
 * clicking the tab that is already selected minimises Revit's ribbon. The click is not awaited, because it returns
 * only when the modal window closes; one UIA walk of Revit's ribbon takes 10–30 s.
 */
async function openWindow() {
  uia(['-Name', 'Kata Settings', '-Nth', '1']);
}

/** The settings page; a window still open from an earlier step is reused instead of clicking the (disabled) ribbon. */
async function openPage() {
  if ((await settingsPages()).length > 0) {
    const stale = await connectToToolkitPage(port, { urlPart: 'kata.hprebar.local/settings.html' });
    // A window with no edits is used as it is: opening one again through Revit's ribbon costs up to minutes.
    const clean = await stale.evaluate(`!!document.getElementById('field-detail-maxBarLengthMm') && !document.body.textContent.includes('mục đã sửa')`).catch(() => false);
    if (clean) {
      await stale.emulateFocus();
      return stale;
    }
    await stale
      .evaluate(
        `(async () => { const b = (t) => [...document.querySelectorAll('button')].find((x) => x.textContent.trim() === t);
          b('Thoát')?.click(); await new Promise((r) => setTimeout(r, 400)); b('Bỏ thay đổi')?.click(); return true; })()`,
      )
      .catch(() => {});
    stale.close();
    await closedWithin(8000);
  }
  await openWindow();
  const page = await connectToToolkitPage(port, { timeoutMs: 240000, urlPart: 'kata.hprebar.local/settings.html' });
  await page.emulateFocus();
  for (let i = 0; i < 40 && !(await page.evaluate(`!!document.getElementById('field-detail-maxBarLengthMm')`)); i++) await sleep(250);
  // A window that has just opened is still being laid out and activated by Revit: the first click waits for it.
  await sleep(1500);
  // Every pointer and key event the page sees, read back when a check fails.
  await page.evaluate(
    `window.__harnessLog = []; for (const t of ['pointerdown', 'click', 'keydown']) document.addEventListener(t, (e) => window.__harnessLog.push(t + ':' + String(e.target.textContent || e.target.id || e.target.tagName).trim().slice(0, 24) + (e.key ? ':' + e.key : '')), true); true`,
  );
  return page;
}

async function closedWithin(ms) {
  const deadline = Date.now() + ms;
  while (Date.now() < deadline) {
    if ((await settingsPages()).length === 0) return true;
    await sleep(250);
  }
  return false;
}

function helpers(page) {
  const q = (expression) => page.evaluate(expression);
  const centre = (selector) =>
    q(`(() => { const e = document.querySelector(${JSON.stringify(selector)}); if (!e) return null; e.scrollIntoView({ block: 'center' }); const r = e.getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`);
  const click = async (selector) => {
    const at = await centre(selector);
    if (at) await page.mouse.click(at.x, at.y);
    return at !== null;
  };
  const clickText = async (role, text) => {
    const at = await q(
      `(() => { const e = [...document.querySelectorAll(${JSON.stringify(role)})].find((b) => b.textContent.trim() === ${JSON.stringify(text)}); if (!e) return null; e.scrollIntoView({ block: 'center' }); const r = e.getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`,
    );
    if (at) await page.mouse.click(at.x, at.y);
    return at !== null;
  };
  const value = (id) => q(`document.getElementById(${JSON.stringify(id)})?.value ?? null`);
  const errorOf = (id) => q(`document.getElementById(${JSON.stringify(`${id}-error`)})?.textContent ?? ''`);
  const typeInto = async (id, text) => {
    await click(`#${id}`);
    await page.keyboard.press('a', { ctrl: true });
    await page.keyboard.type(text);
    const now = await value(id);
    if (now !== text) throw new Error(`#${id} holds '${now}' after typing '${text}'`);
  };
  const shoot = async (name) => {
    if (!shots) return;
    const png = await Promise.race([page.screenshot(), sleep(5000).then(() => null)]);
    if (png) writeFileSync(join(shots, name), Buffer.from(png, 'base64'));
  };
  return { q, click, clickText, value, errorOf, typeInto, shoot };
}

let viewerLang = null;
const readFile = () => (existsSync(settingsFile) ? readFileSync(settingsFile, 'utf8') : null);

try {
  // 1. Opens on the file (Kata's defaults when there is none), in a modal window over Revit.
  let page = await openPage();
  let h = helpers(page);
  check('the ribbon button opens settings.html in the Kata Settings window', page.url.startsWith('https://kata.hprebar.local/settings.html'), page.url);
  // The checks read the Vietnamese page; a viewer's English choice (shared with Kata Export) is put back at the end.
  viewerLang = await h.q(`localStorage.getItem('hp.kata.lang')`);
  if (viewerLang === 'en') {
    await h.q(`localStorage.setItem('hp.kata.lang', 'vi'); location.reload(); true`).catch(() => {});
    page.close();
    await sleep(2000);
    page = await connectToToolkitPage(port, { timeoutMs: 30000, urlPart: 'kata.hprebar.local/settings.html' });
    h = helpers(page);
    await sleep(1500);
  }
  const before = readFile();
  const shown = await h.value('field-detail-maxBarLengthMm');
  if (!hadFile) check('with no settings file the page shows Kata\'s defaults', shown === '11700', shown);
  else check('the page opens on the settings file', shown !== null, shown);
  await h.shoot('settings-01-open.png');

  // 2. A value out of range: the host's own rule names the field, nothing is written, Accept keeps the window open.
  await h.typeInto('field-detail-endZoneFraction', '0.7');
  await sleep(900);
  const error = await h.errorOf('field-detail-endZoneFraction');
  check('the host refuses end zone 0.7 under the field', error.includes('0 … 0.5'), error);
  check('the footer counts one field to fix', (await h.q(`document.body.textContent.includes('1 ô cần sửa')`)) === true);
  await h.click('[data-testid=settings-accept]');
  await sleep(1200);
  check('Accept with a wrong value keeps the window open and writes nothing', (await settingsPages()).length === 1 && readFile() === before);
  await h.shoot('settings-02-error.png');

  // 3. Fixed and edited on two tabs, then Enter in a box: saved, window closed.
  await h.typeInto('field-detail-endZoneFraction', '0.3');
  await h.typeInto('field-detail-maxBarLengthMm', '11000');
  // One scrolling page: the lap table's card is found by its name and its add button marked for the click.
  const card = `document.querySelector('section[aria-label="Bảng neo nối thép"]')`;
  const addLap = await h.q(`${card} !== null`);
  await h.q(`[...${card}.querySelectorAll('button')].find((b) => b.textContent.includes('Thêm dòng')).setAttribute('data-harness', 'add-lap')`);
  await h.click('[data-harness=add-lap]');
  await sleep(300);
  const cells = ['diameter', 'lapCompression', 'lapTension', 'anchorCompression', 'anchorTension'];
  const rowValues = ['16', '480', '640', '400', '500'];
  for (let i = 0; i < cells.length; i++) await h.typeInto(`field-special-lapRows-0--${cells[i]}`, rowValues[i]);
  check('a lap row can be added and filled', addLap && (await h.value('field-special-lapRows-0--anchorTension')) === '500');
  await sleep(700);
  check('the edited form has no error', (await h.q(`document.querySelectorAll('[role=alert]').length`)) === 0);
  await page.keyboard.press('Enter');
  check('Enter accepts: the window closes', await closedWithin(8000));
  page.close();
  const written = readFile();
  const saved = written ? JSON.parse(written) : null;
  const flat = JSON.stringify(saved ?? {});
  check('the settings file holds the new values', /11000/.test(flat) && /0\.3\b/.test(flat) && /16:480,640,400,500/.test(flat), written ? `${written.length} chars` : 'no file');

  // 4. Opened again: the saved values; Defaults replaces them on the page only; Exit after a change asks, discard keeps the file.
  page = await openPage();
  h = helpers(page);
  check('reopened, the page shows what was saved', (await h.value('field-detail-maxBarLengthMm')) === '11000' && (await h.value('field-detail-endZoneFraction')) === '0.3');
  await h.clickText('button', 'Mặc định');
  await sleep(900);
  const afterDefaults = await h.value('field-detail-maxBarLengthMm');
  check('Defaults shows Kata\'s defaults', afterDefaults === '11700', `${afterDefaults}; events ${JSON.stringify(await h.q('window.__harnessLog'))}`);
  if (afterDefaults !== '11700') throw new Error('Defaults did not apply: the steps after it would act on a closed window');
  check('Defaults writes nothing', readFile() === written);
  await h.clickText('button', 'Thoát');
  await sleep(500);
  check('Exit after a change asks first', (await h.q(`!!document.querySelector('[role=alertdialog]')`)) === true);
  await h.shoot('settings-03-discard.png');
  await h.clickText('[role=alertdialog] button', 'Bỏ thay đổi');
  check('Discard closes the window', await closedWithin(8000));
  page.close();
  check('Discard leaves the file as it was', readFile() === written);

  // 5. Escape with nothing changed closes at once.
  page = await openPage();
  h = helpers(page);
  if (viewerLang === 'en') await h.q(`localStorage.setItem('hp.kata.lang', 'en'); true`);
  await h.click('#field-detail-roundLapMm');
  await page.keyboard.press('Escape');
  check('Escape with nothing changed closes the window', await closedWithin(8000));
  page.close();

  const responding = await new Promise((resolve) => {
    const p = spawn('powershell.exe', ['-NoProfile', '-Command', `(Get-Process -Id ${revitPid}).Responding`]);
    let out = '';
    p.stdout.on('data', (d) => (out += d));
    p.on('close', () => resolve(out.trim()));
  });
  check('Revit is still responding', responding === 'True', responding);
} catch (error) {
  check('the run finished', false, error.message);
} finally {
  restoreSettings();
}

const failed = results.filter((r) => !r.ok).length;
console.log(`${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
