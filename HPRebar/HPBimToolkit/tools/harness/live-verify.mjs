// Live verification of the HP BIM Toolkit dashboard in a Revit started by start-revit-with-toolkit.ps1.
// Drives the real page over the WebView2 DevTools port: protocol checks (requests posted from inside the page, so they
// pass the origin check like the app's own calls) and UI checks on the rendered DOM. Read-only towards the model,
// except for changing the selection. Usage: node live-verify.mjs [port] [expectedTitle]
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9333);
const expectedTitle = process.argv[3] ?? '';
const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const page = await connectToToolkitPage(port, { timeoutMs: 90000, urlPart: 'toolkit.hpbim.local' });
check('page served from the embedded virtual host', page.url.startsWith('https://toolkit.hpbim.local/'), page.url);

// Collect every event the page receives from now on.
await page.evaluate(`window.__events = []; chrome.webview.addEventListener('message', e => { if (e.data?.kind === 'event') window.__events.push(e.data); }); true`);

const post = (message) =>
  page.evaluate(`new Promise((resolve) => {
    const message = ${message};
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === message.id) { chrome.webview.removeEventListener('message', onMessage); resolve(e.data); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage(message);
    setTimeout(() => resolve({ timedOut: true }), 40000);
  })`);
const call = (command, payload) =>
  post(`({ v: 1, kind: 'request', id: 'lv-' + crypto.randomUUID(), command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} })`);
const text = () => page.evaluate('document.body.innerText');

// --- protocol ---
const hello = await call('app.hello');
check('app.hello answers with Revit version and commands', hello.ok && /^2026/.test(hello.payload.revitVersion) &&
  hello.payload.capabilities.includes('dashboard.getStatistics'), JSON.stringify(hello.payload));

const documentInfo = await call('dashboard.getDocumentInfo');
check('getDocumentInfo returns the open model copy', documentInfo.ok && documentInfo.payload.hasDocument &&
  (!expectedTitle || documentInfo.payload.title === expectedTitle), `${documentInfo.payload?.title} / view ${documentInfo.payload?.activeView?.name}`);

const model = await call('dashboard.getStatistics', { scope: 'model' });
const categorySum = model.payload?.categories.reduce((sum, c) => sum + c.count, 0);
check('getStatistics(model) total equals the sum of its categories', model.ok && model.payload.total > 0 && categorySum === model.payload.total,
  `total ${model.payload?.total}, ${model.payload?.categories.length} categories, ${model.payload?.elapsedMs} ms`);
check('model metrics are filled', model.ok && model.payload.metrics.levels > 0 && model.payload.metrics.views > 0, JSON.stringify(model.payload?.metrics));

check('statistics carry the document key', model.payload?.documentKey === (documentInfo.payload.path ?? documentInfo.payload.title) && model.payload.viewId === null,
  `${model.payload?.documentKey} / viewId ${model.payload?.viewId}`);
const bookkeeping = model.payload.categories.filter((c) => [-2000700, -2000045, -2000262, -2009609, -2003101].includes(c.categoryId));
check('Revit bookkeeping categories are not counted (materials, sketch, constraints, sun path, project info)', bookkeeping.length === 0,
  bookkeeping.map((c) => c.name).join(', '));

const view = await call('dashboard.getStatistics', { scope: 'activeView' });
check('activeView statistics name the counted view', view.ok && view.payload.viewId === documentInfo.payload.activeView?.id, `viewId ${view.payload?.viewId}`);
check('getStatistics(activeView) is a subset of the model', view.ok && view.payload.total <= model.payload.total,
  view.ok ? `total ${view.payload.total}, ${view.payload.elapsedMs} ms` : JSON.stringify(view.error));

const top = model.payload.categories[0];
const shown = await call('dashboard.showElements', { categoryId: top.categoryId, mode: 'select', scope: 'model' });
check('showElements selects exactly the counted elements of a category', shown.ok && shown.payload.shown === top.count, `${top.name}: shown ${shown.payload?.shown} of ${top.count}`);

const selection = await call('dashboard.getSelection');
check('getSelection reflects the new selection (ids capped at 200)', selection.ok && selection.payload.count === top.count &&
  selection.payload.ids.length === Math.min(200, top.count), `count ${selection.payload?.count}, ids ${selection.payload?.ids.length}`);

await sleep(800);
const events = await page.evaluate('window.__events');
const selectionEvent = events.filter((e) => e.event === 'selection.changed').at(-1);
check('selection.changed event pushed with the new count (coalesced)', selectionEvent?.payload.count === top.count,
  `${events.filter((e) => e.event === 'selection.changed').length} selection events, last ${selectionEvent?.payload.count}`);

const zoom = await call('dashboard.showElements', { currentSelection: true, mode: 'zoom' });
const afterZoom = await call('dashboard.getSelection');
check('zoom to current selection keeps the selection', zoom.ok && zoom.payload.shown === top.count && afterZoom.payload.count === top.count,
  `shown ${zoom.payload?.shown}, selection after ${afterZoom.payload?.count}`);

const unknown = await call('dashboard.nope');
check('unknown command answers UNKNOWN_COMMAND', unknown.ok === false && unknown.error.code === 'UNKNOWN_COMMAND');

const badTool = await call('dashboard.openTool', { toolId: 'rm -rf' });
check('unknown tool id answers BAD_REQUEST', badTool.ok === false && badTool.error.code === 'BAD_REQUEST');

const badScope = await call('dashboard.getStatistics', { scope: 'everything' });
check('invalid payload answers BAD_REQUEST', badScope.ok === false && badScope.error.code === 'BAD_REQUEST', badScope.error?.message);

const badVersion = await post(`({ v: 2, kind: 'request', id: 'lv-v2', command: 'app.hello' })`);
check('wrong protocol version is rejected keeping the id', badVersion.ok === false && badVersion.id === 'lv-v2' && badVersion.error.code === 'BAD_REQUEST');

// --- UI (the page's own client rendered these) ---
await page.evaluate("location.reload(); true");
await sleep(5000);
const body = await text();
check('UI shows the document title', !!documentInfo.payload.title && body.includes(documentInfo.payload.title));
check('UI shows real data, not the mock', !body.includes('dữ liệu mẫu') && body.includes('Revit 2026'));
check('UI lists the top category', body.includes(top.name));
const selectionText = await page.evaluate("document.querySelector('[data-testid=selection-count]')?.textContent");
check('UI selection card shows the Revit selection', Number(selectionText?.replace(/\./g, '')) === top.count, `card says ${selectionText}`);

const clear = await call('dashboard.showElements', { ids: [], mode: 'select' });
check('empty id list without category is BAD_REQUEST', clear.ok === false && clear.error.code === 'BAD_REQUEST');

page.close();
const failed = results.filter((r) => !r.ok).length;
console.log(`\n${results.length - failed}/${results.length} PASS`);
process.exit(failed ? 1 : 0);
