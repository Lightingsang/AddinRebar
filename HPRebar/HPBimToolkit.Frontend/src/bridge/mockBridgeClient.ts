import { BridgeError, type BridgeClient } from './bridgeClient';
import { EventHub } from './eventHub';
import { mockDocument, mockMetrics, mockModelCategories, mockToolIds } from './mockData';
import type { CommandMap, CommandName, ModelStatistics, StatisticsScope, Theme } from './protocol';

export interface MockOptions {
  latencyMs?: number;
  theme?: Theme;
  hasDocument?: boolean;
}

/**
 * Stands in for Revit when the page runs in a normal browser: answers every v1 command with sample data after a
 * short delay and raises the events Revit would (selection after "show", stale after a simulated edit).
 */
export class MockBridgeClient implements BridgeClient {
  readonly isMock = true;
  private readonly events = new EventHub();
  private readonly latencyMs: number;
  private readonly theme: Theme;
  private hasDocument: boolean;
  private selection: number[] = [];

  constructor(options: MockOptions = {}) {
    this.latencyMs = options.latencyMs ?? 350;
    this.theme = options.theme ?? 'light';
    this.hasDocument = options.hasDocument ?? true;
  }

  on: BridgeClient['on'] = (event, handler) => this.events.on(event, handler);

  /** Lets the dev page and tests flip between "project open" and "no project". */
  setHasDocument(hasDocument: boolean): void {
    this.hasDocument = hasDocument;
    this.events.emit('document.changed', hasDocument ? mockDocument : { ...mockDocument, hasDocument: false, title: null, path: null, activeView: null });
  }

  simulateEdit(): void {
    this.events.emit('model.stale', { added: 3, deleted: 1, modified: 7 });
  }

  async request<C extends CommandName>(
    command: C,
    payload?: CommandMap[C]['request'],
  ): Promise<CommandMap[C]['response']> {
    await new Promise((resolve) => setTimeout(resolve, this.latencyMs));
    return this.answer(command, payload) as CommandMap[C]['response'];
  }

  private answer(command: CommandName, payload: unknown): unknown {
    if (command === 'app.hello') {
      return { version: '0.1.0-mock', revitVersion: '2026 (mock)', theme: this.theme, capabilities: mockToolIds.map((id) => `tool:${id}`) };
    }

    if (command === 'dashboard.getDocumentInfo') {
      return this.hasDocument ? mockDocument : { ...mockDocument, hasDocument: false, title: null, path: null, activeView: null };
    }

    if (!this.hasDocument) {
      throw new BridgeError({ code: 'NO_DOCUMENT', message: 'Chưa mở tài liệu Revit nào.' });
    }

    switch (command) {
      case 'dashboard.getStatistics':
        return statistics((payload as { scope?: StatisticsScope } | undefined)?.scope ?? 'model');
      case 'dashboard.getSelection':
        return { count: this.selection.length, ids: this.selection.slice(0, 200), truncated: this.selection.length > 200 };
      case 'dashboard.showElements':
        return this.show(payload as CommandMap['dashboard.showElements']['request']);
      case 'dashboard.openTool':
        return { opened: true };
      default:
        throw new BridgeError({ code: 'UNKNOWN_COMMAND', message: `Lệnh không tồn tại: ${String(command)}` });
    }
  }

  private show(request: CommandMap['dashboard.showElements']['request']): CommandMap['dashboard.showElements']['response'] {
    if (request.currentSelection) {
      return { shown: this.selection.length, zoomed: request.mode === 'zoom' ? this.selection.length : 0 };
    }
    const count = request.ids?.length ?? mockModelCategories.find((c) => c.categoryId === request.categoryId)?.count ?? 0;
    this.selection = request.ids ?? Array.from({ length: count }, (_, i) => 100000 + i);
    this.events.emit('selection.changed', { count: this.selection.length });
    return { shown: count, zoomed: request.mode === 'zoom' ? count : 0 };
  }
}

function statistics(scope: StatisticsScope): ModelStatistics {
  const categories = scope === 'model'
    ? mockModelCategories
    : mockModelCategories.slice(0, 9).map((c) => ({ ...c, count: Math.max(1, Math.round(c.count / 9)) }));
  return {
    documentKey: mockDocument.path ?? mockDocument.title ?? '',
    viewId: scope === 'activeView' ? (mockDocument.activeView?.id ?? null) : null,
    scope,
    total: categories.reduce((sum, c) => sum + c.count, 0),
    categories,
    metrics: mockMetrics,
    elapsedMs: scope === 'model' ? 412 : 58,
  };
}
