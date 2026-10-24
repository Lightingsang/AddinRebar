// Minimal Chrome DevTools Protocol client over Node's built-in WebSocket (Node >= 22): finds the toolkit page on the
// WebView2 debug port and evaluates expressions in it. Used by the live-verify scripts; no npm dependency.

export async function connectToToolkitPage(port, { timeoutMs = 30000, urlPart = '' } = {}) {
  const deadline = Date.now() + timeoutMs;
  let target;
  while (!target) {
    try {
      const pages = await (await fetch(`http://127.0.0.1:${port}/json`)).json();
      target = pages.find((p) => p.type === 'page' && p.url.includes(urlPart));
    } catch {
      // the browser starts only when the pane is first shown; keep polling until the deadline
    }
    if (!target) {
      if (Date.now() > deadline) throw new Error(`no WebView2 page on port ${port} within ${timeoutMs} ms`);
      await new Promise((r) => setTimeout(r, 500));
    }
  }

  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.onopen = resolve;
    socket.onerror = reject;
  });

  let nextId = 1;
  const waiting = new Map();
  socket.onmessage = (event) => {
    const message = JSON.parse(event.data);
    const pending = waiting.get(message.id);
    if (pending) {
      waiting.delete(message.id);
      message.error ? pending.reject(new Error(message.error.message)) : pending.resolve(message.result);
    }
  };

  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      waiting.set(id, { resolve, reject });
      socket.send(JSON.stringify({ id, method, params }));
    });

  return {
    url: target.url,
    async evaluate(expression) {
      const result = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
      if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description ?? 'evaluation failed');
      return result.result.value;
    },
    close: () => socket.close(),
  };
}

export const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
