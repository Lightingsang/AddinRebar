// Spike check: clicks "Ping Revit" in the toolkit page and expects the pong with the open document's title.
import { connectToToolkitPage, sleep } from './cdp-session.mjs';

const port = Number(process.argv[2] ?? 9333);
const page = await connectToToolkitPage(port, { timeoutMs: 60000 });
console.log(`page: ${page.url}`);
await page.evaluate("document.getElementById('ping').click()");
await sleep(2000);
const out = await page.evaluate("document.getElementById('out').textContent");
console.log(out);
page.close();
const reply = JSON.parse(out);
if (reply.kind !== 'pong' || !reply.documentTitle) {
  console.log('FAIL round trip');
  process.exit(1);
}
console.log(`PASS round trip, document "${reply.documentTitle}"`);
