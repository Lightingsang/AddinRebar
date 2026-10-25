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

  // Real input events in page coordinates (CSS pixels), as the user's mouse would send them.
  const mouseEvent = (type, x, y, extra = {}) => send('Input.dispatchMouseEvent', { type, x, y, ...extra });
  const buttons = { left: 1, middle: 4 };

  return {
    url: target.url,
    async evaluate(expression) {
      const result = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
      if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description ?? 'evaluation failed');
      return result.result.value;
    },
    mouse: {
      /** deltaY < 0 scrolls up (zooms in on the elevation). */
      wheel: (x, y, deltaY) => mouseEvent('mouseWheel', x, y, { deltaX: 0, deltaY }),
      async click(x, y, button = 'left') {
        await mouseEvent('mousePressed', x, y, { button, buttons: buttons[button], clickCount: 1 });
        await mouseEvent('mouseReleased', x, y, { button, buttons: 0, clickCount: 1 });
      },
      async drag(x1, y1, x2, y2, { button = 'left', steps = 8 } = {}) {
        await mouseEvent('mousePressed', x1, y1, { button, buttons: buttons[button], clickCount: 1 });
        for (let i = 1; i <= steps; i++) {
          await mouseEvent('mouseMoved', x1 + ((x2 - x1) * i) / steps, y1 + ((y2 - y1) * i) / steps, { button, buttons: buttons[button] });
        }
        await mouseEvent('mouseReleased', x2, y2, { button, buttons: 0, clickCount: 1 });
      },
    },
    keyboard: {
      /** A key press to the focused element; `key` as KeyboardEvent.key ('Delete', 'Escape', 'z'), Ctrl when asked. */
      async press(key, { ctrl = false } = {}) {
        const codes = { Delete: 46, Escape: 27, z: 90 };
        const base = { key, code: key.length === 1 ? `Key${key.toUpperCase()}` : key, windowsVirtualKeyCode: codes[key] ?? 0, modifiers: ctrl ? 2 : 0 };
        await send('Input.dispatchKeyEvent', { type: 'keyDown', ...base });
        await send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
      },
    },
    close: () => socket.close(),
  };
}

export const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
