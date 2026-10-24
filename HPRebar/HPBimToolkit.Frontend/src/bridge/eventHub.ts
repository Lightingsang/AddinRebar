import type { Unsubscribe } from './bridgeClient';
import type { EventMap, EventName } from './protocol';

type Handler = (payload: never) => void;

/** Event subscriptions shared by the real and the mock bridge. A throwing handler never stops the others. */
export class EventHub {
  private readonly handlers = new Map<string, Set<Handler>>();

  on<E extends EventName>(event: E, handler: (payload: EventMap[E]) => void): Unsubscribe {
    const set = this.handlers.get(event) ?? new Set<Handler>();
    set.add(handler as Handler);
    this.handlers.set(event, set);
    return () => set.delete(handler as Handler);
  }

  emit(event: string, payload: unknown): void {
    for (const handler of this.handlers.get(event) ?? []) {
      try {
        (handler as (value: unknown) => void)(payload);
      } catch (error) {
        console.error(`handler for ${event} failed`, error);
      }
    }
  }
}
