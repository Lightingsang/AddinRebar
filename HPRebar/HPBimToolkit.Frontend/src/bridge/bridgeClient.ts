import type { BridgeErrorInfo, CommandMap, CommandName, ErrorCode, EventMap, EventName } from './protocol';

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

/** What the UI talks to: the real WebView bridge inside Revit, or a mock in a normal browser. */
export interface BridgeClient {
  readonly isMock: boolean;
  request<C extends CommandName>(
    command: C,
    payload?: CommandMap[C]['request'],
    options?: RequestOptions,
  ): Promise<CommandMap[C]['response']>;
  on<E extends EventName>(event: E, handler: (payload: EventMap[E]) => void): Unsubscribe;
}

/**
 * Longer than the host's own 30 s limit, so a slow Revit normally answers with the host's TIMEOUT (which explains
 * itself) before this fallback fires.
 */
export const DEFAULT_TIMEOUT_MS = 35_000;
