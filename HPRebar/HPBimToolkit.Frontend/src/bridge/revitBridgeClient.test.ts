import { afterEach, describe, expect, it, vi } from 'vitest';
import { BridgeError } from './bridgeClient';
import { RevitBridgeClient, type WebViewPort } from './revitBridgeClient';

class FakePort implements WebViewPort {
  readonly sent: unknown[] = [];
  private listener: ((event: { data: unknown }) => void) | null = null;

  postMessage(message: unknown): void {
    this.sent.push(message);
  }

  addEventListener(_type: 'message', listener: (event: { data: unknown }) => void): void {
    this.listener = listener;
  }

  deliver(data: unknown): void {
    this.listener?.({ data });
  }
}

function setup() {
  const port = new FakePort();
  let counter = 0;
  const client = new RevitBridgeClient(port, () => `id-${++counter}`);
  return { port, client };
}

afterEach(() => vi.useRealTimers());

describe('RevitBridgeClient', () => {
  it('posts a v1 request and resolves with the matching response payload', async () => {
    const { port, client } = setup();

    const pending = client.request('dashboard.getStatistics', { scope: 'activeView' });
    port.deliver({ v: 1, kind: 'response', id: 'id-1', ok: true, payload: { total: 7 } });

    await expect(pending).resolves.toEqual({ total: 7 });
    expect(port.sent[0]).toEqual({ v: 1, kind: 'request', id: 'id-1', command: 'dashboard.getStatistics', payload: { scope: 'activeView' } });
  });

  it('correlates out-of-order responses by id', async () => {
    const { port, client } = setup();

    const first = client.request('dashboard.getSelection');
    const second = client.request('dashboard.getDocumentInfo');
    port.deliver({ v: 1, kind: 'response', id: 'id-2', ok: true, payload: 'doc' });
    port.deliver({ v: 1, kind: 'response', id: 'id-1', ok: true, payload: 'selection' });

    await expect(first).resolves.toBe('selection');
    await expect(second).resolves.toBe('doc');
  });

  it('rejects with the host error code and message', async () => {
    const { port, client } = setup();

    const pending = client.request('dashboard.getSelection');
    port.deliver({ v: 1, kind: 'response', id: 'id-1', ok: false, error: { code: 'NO_DOCUMENT', message: 'none open' } });

    await expect(pending).rejects.toMatchObject({ name: 'BridgeError', code: 'NO_DOCUMENT', message: 'none open' });
  });

  it('rejects with TIMEOUT when no answer arrives and ignores a late answer', async () => {
    vi.useFakeTimers();
    const { port, client } = setup();

    const pending = client.request('dashboard.getStatistics', { scope: 'model' }, { timeoutMs: 1000 });
    vi.advanceTimersByTime(1000);
    port.deliver({ v: 1, kind: 'response', id: 'id-1', ok: true, payload: 'late' });

    await expect(pending).rejects.toBeInstanceOf(BridgeError);
    await expect(pending).rejects.toMatchObject({ code: 'TIMEOUT' });
  });

  it('forwards events to subscribers until they unsubscribe', () => {
    const { port, client } = setup();
    const counts: number[] = [];

    const unsubscribe = client.on('selection.changed', ({ count }) => counts.push(count));
    port.deliver({ v: 1, kind: 'event', event: 'selection.changed', payload: { count: 3 } });
    unsubscribe();
    port.deliver({ v: 1, kind: 'event', event: 'selection.changed', payload: { count: 9 } });

    expect(counts).toEqual([3]);
  });

  it.each([null, 'text', 42, { v: 2, kind: 'response', id: 'id-1', ok: true }, { v: 1, kind: 'response', ok: true }])(
    'ignores the malformed message %j',
    async (message) => {
      const { port, client } = setup();
      const pending = client.request('app.hello');

      port.deliver(message);
      port.deliver({ v: 1, kind: 'response', id: 'id-1', ok: true, payload: 'hello' });

      await expect(pending).resolves.toBe('hello');
    },
  );
});
