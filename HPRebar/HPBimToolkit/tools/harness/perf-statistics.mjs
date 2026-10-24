// Measures the dashboard's costs on the open model: statistics (model and active view, host time and round trip,
// three runs each), document info, and the selection event latency. Read-only except for the selection.
// Usage: node perf-statistics.mjs [port]
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9333);
const page = await connectToToolkitPage(port, { timeoutMs: 120000, urlPart: 'toolkit.hpbim.local' });
const call = (command, payload) =>
  page.evaluate(`new Promise((resolve) => {
    const id = 'perf-' + crypto.randomUUID();
    const started = performance.now();
    const onMessage = (e) => { if (e.data?.kind === 'response' && e.data.id === id) { chrome.webview.removeEventListener('message', onMessage); resolve({ ...e.data, roundTripMs: Math.round(performance.now() - started) }); } };
    chrome.webview.addEventListener('message', onMessage);
    chrome.webview.postMessage({ v: 1, kind: 'request', id, command: ${JSON.stringify(command)}, payload: ${JSON.stringify(payload ?? null)} });
    setTimeout(() => resolve({ timedOut: true }), 140000);
  })`);

const info = await call('dashboard.getDocumentInfo');
console.log(`document ${info.payload?.title}, view ${info.payload?.activeView?.name}, round trip ${info.roundTripMs} ms`);

for (const scope of ['model', 'activeView']) {
  for (let run = 1; run <= 3; run++) {
    const r = await call('dashboard.getStatistics', { scope });
    console.log(r.ok
      ? `${scope} #${run}: ${r.payload.total} elements, ${r.payload.categories.length} categories, host ${r.payload.elapsedMs} ms, round trip ${r.roundTripMs} ms`
      : `${scope} #${run}: ${r.error?.code ?? 'no answer'} ${r.error?.message ?? ''}`);
  }
}

const model = await call('dashboard.getStatistics', { scope: 'model' });
const biggest = model.payload.categories[0];
await page.evaluate(`window.__sel = null; chrome.webview.addEventListener('message', e => { if (e.data?.event === 'selection.changed' && window.__sel === null) window.__sel = performance.now(); }); true`);
const t0 = await page.evaluate('performance.now()');
const shown = await call('dashboard.showElements', { categoryId: biggest.categoryId, mode: 'select', scope: 'model' });
await sleep(1500);
const firstEvent = await page.evaluate('window.__sel');
console.log(`select ${biggest.name}: ${shown.payload?.shown} elements, round trip ${shown.roundTripMs} ms, selection.changed after ${firstEvent ? Math.round(firstEvent - t0) : 'none'} ms`);
await call('dashboard.showElements', { ids: [], mode: 'select' }).catch(() => {});
page.close();
