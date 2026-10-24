import type { BridgeErrorInfo, CommandMap, ErrorCode, EventMap } from './protocol';

/** A failed call: the host's error, or a client-side TIMEOUT when no answer came. */
export class BridgeError extends Error {
  readonly code: ErrorCode;

  constructor(info: BridgeErrorInfo) {
    super(info.message);
    this.name = 'BridgeError';
    this.code = info.code;
  }
}

export interface RequestOptions {
  timeoutMs?: number;
}

export type Unsubscribe = () => void;

/** Command name → request and response payloads of one page's protocol. */
export type CommandShapes<CM> = { [K in keyof CM]: { request: unknown; response: unknown } };

/** A bridge typed by one page's commands and events (protocol v1 envelopes are the same for every page). */
export interface TypedBridgeClient<CM extends CommandShapes<CM>, EM> {
  readonly isMock: boolean;
  request<C extends keyof CM & string>(command: C, payload?: CM[C]['request'], options?: RequestOptions): Promise<CM[C]['response']>;
  on<E extends keyof EM & string>(event: E, handler: (payload: EM[E]) => void): Unsubscribe;
}

/** What the toolkit's UI talks to: the real WebView bridge inside Revit, or a mock in a normal browser. */
export type BridgeClient = TypedBridgeClient<CommandMap, EventMap>;

/**
 * Longer than the host's own 30 s limit, so a slow Revit normally answers with the host's TIMEOUT (which explains
 * itself) before this fallback fires.
 */
export const DEFAULT_TIMEOUT_MS = 35_000;
