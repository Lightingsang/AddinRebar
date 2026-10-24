// Protocol v1 between the page and the Revit add-in. The payload types, ErrorCode, CommandMap and EventMap are generated
// from HPBimToolkit.Core/Contracts (contracts.generated.ts); only the envelopes are written here. Field names are camelCase,
// element ids are numbers (ElementId.Value).

import type { ErrorCode } from './contracts.generated';

export type * from './contracts.generated';

export const PROTOCOL_VERSION = 1;

export interface BridgeErrorInfo {
  code: ErrorCode;
  message: string;
  details?: unknown;
}

export interface RequestEnvelope {
  v: 1;
  kind: 'request';
  id: string;
  command: string;
  payload?: unknown;
}

export type ResponseEnvelope =
  | { v: 1; kind: 'response'; id: string; ok: true; payload: unknown; error?: null }
  | { v: 1; kind: 'response'; id: string; ok: false; payload?: null; error: BridgeErrorInfo };

export interface EventEnvelope {
  v: 1;
  kind: 'event';
  event: string;
  payload: unknown;
}
