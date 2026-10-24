// Small CDP helper for manual live steps: `start` installs an event recorder in the toolkit page, `read` prints and
// clears what it recorded. Usage: node page-events.mjs <port> start|read
import { connectToToolkitPage } from './cdp-session.mjs';

const [port, mode] = [Number(process.argv[2] ?? 9333), process.argv[3] ?? 'read'];
const page = await connectToToolkitPage(port, { urlPart: 'toolkit.hpbim.local' });
if (mode === 'start') {
  await page.evaluate(`window.__recorded = []; if (!window.__recorderInstalled) { window.__recorderInstalled = true;
    chrome.webview.addEventListener('message', e => { if (e.data?.kind === 'event') window.__recorded.push({ at: Date.now(), event: e.data.event, payload: e.data.payload }); }); } true`);
  console.log('recorder started');
} else {
  console.log(JSON.stringify(await page.evaluate('(() => { const r = window.__recorded ?? []; window.__recorded = []; return r; })()')));
  console.log('visible:', await page.evaluate('document.visibilityState'));
}
page.close();
