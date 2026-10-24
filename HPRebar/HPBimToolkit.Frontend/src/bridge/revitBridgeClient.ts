import { BridgeError, type BridgeClient, DEFAULT_TIMEOUT_MS, type RequestOptions } from './bridgeClient';
import { EventHub } from './eventHub';
import type { CommandMap, CommandName, RequestEnvelope } from './protocol';
import { PROTOCOL_VERSION } from './protocol';

/** The part of WebView2's `window.chrome.webview` this client uses. */
export interface WebViewPort {
  postMessage(message: unknown): void;
  addEventListener(type: 'message', listener: (event: { data: unknown }) => void): void;
}

interface Pending {
  resolve: (value: unknown) => void;
  reject: (error: BridgeError) => void;
  timer: ReturnType<typeof setTimeout>;
}

/**
 * Talks to the add-in through WebView2: posts v1 requests, correlates responses by id, rejects calls that get no
 * answer in time, and forwards events. Messages that are not v1 responses or events are ignored.
 */
export class RevitBridgeClient implements BridgeClient {
  readonly isMock = false;
  private readonly pending = new Map<string, Pending>();
  private readonly events = new EventHub();
  private readonly port: WebViewPort;
  private readonly newId: () => string;

  constructor(port: WebViewPort, newId: () => string = () => crypto.randomUUID()) {
    this.port = port;
    this.newId = newId;
    port.addEventListener('message', (event) => this.receive(event.data));
  }

  request<C extends CommandName>(
    command: C,
    payload?: CommandMap[C]['request'],
    options?: RequestOptions,
  ): Promise<CommandMap[C]['response']> {
    const id = this.newId();
    const timeoutMs = options?.timeoutMs ?? DEFAULT_TIMEOUT_MS;
    const envelope: RequestEnvelope = { v: PROTOCOL_VERSION, kind: 'request', id, command, payload };

    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        this.pending.delete(id);
        reject(new BridgeError({ code: 'TIMEOUT', message: `Revit không phản hồi sau ${Math.round(timeoutMs / 1000)} giây.` }));
      }, timeoutMs);
      this.pending.set(id, { resolve: resolve as (value: unknown) => void, reject, timer });
      this.port.postMessage(envelope);
    });
  }

  on: BridgeClient['on'] = (event, handler) => this.events.on(event, handler);

  private receive(data: unknown): void {
    if (!isObject(data) || data.v !== PROTOCOL_VERSION) {
      return;
    }

    if (data.kind === 'event' && typeof data.event === 'string') {
      this.events.emit(data.event, data.payload);
      return;
    }

    if (data.kind !== 'response' || typeof data.id !== 'string') {
      return;
    }

    const call = this.pending.get(data.id);
    if (!call) {
      return; // already timed out, or an id-less rejection of a message this page never sent
    }

    this.pending.delete(data.id);
    clearTimeout(call.timer);
    if (data.ok === true) {
      call.resolve(data.payload);
    } else {
      const error = isObject(data.error) ? data.error : {};
      call.reject(
        new BridgeError({
          code: typeof error.code === 'string' ? (error.code as BridgeError['code']) : 'INTERNAL',
          message: typeof error.message === 'string' ? error.message : 'Lỗi không xác định.',
        }),
      );
    }
  }
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
