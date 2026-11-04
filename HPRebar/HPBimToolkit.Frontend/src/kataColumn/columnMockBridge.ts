import { EventHub } from '../bridge/eventHub';
import type {
  ColumnBridge,
  ColumnCommandMap,
  ColumnEventMap,
  KataColumnMode,
  KataColumnPreview,
  KataColumnRebarSummary,
  KataColumnWorkbook,
  KataColumnWriteRequest,
  KataTheme,
} from './columnContracts';

const READY: KataColumnWorkbook = { ready: true, name: 'Kata.xlsm', error: '' };

/**
 * Stands in for Revit and Excel in a normal browser and in the tests: shows a preview (a fixture written by
 * HPRebar.Core.Tests), keeps what was written and closed. `workbook` is what Excel answers until `activate` changes it.
 */
export class ColumnMockBridge implements ColumnBridge {
  readonly isMock = true;
  readonly calls: { command: keyof ColumnCommandMap; payload: unknown }[] = [];
  written: KataColumnWriteRequest | null = null;
  generated = 0;
  /** Warnings the next creation reports. */
  generateWarnings: string[] = [];
  /** Storeys the sheet has no bar count for after a write. */
  rowsWithoutBars: string[] = [];
  closed = false;

  private readonly events = new EventHub<ColumnEventMap>();
  private readonly preview: KataColumnPreview | null;
  private readonly rebar: KataColumnRebarSummary | null;
  private readonly latencyMs: number;
  private readonly theme: KataTheme;
  private readonly mode: KataColumnMode;
  private workbook: KataColumnWorkbook;

  /** `mode` is the step the page opens on (step 1 unless only a rebar plan is given). */
  constructor(options: { preview?: KataColumnPreview; rebar?: KataColumnRebarSummary; mode?: KataColumnMode; workbook?: KataColumnWorkbook; latencyMs?: number; theme?: KataTheme }) {
    this.mode = options.mode ?? (options.preview ? 'export' : 'rebar');
    this.preview = options.preview ?? null;
    this.rebar = options.rebar ?? null;
    this.workbook = options.workbook ?? READY;
    this.latencyMs = options.latencyMs ?? 120;
    this.theme = options.theme ?? 'light';
  }

  on: ColumnBridge['on'] = (event, handler) => this.events.on(event, handler);

  /** The user activated another workbook in Excel. */
  activate(workbook: KataColumnWorkbook = READY): void {
    this.workbook = workbook;
  }

  emitTheme(theme: KataTheme): void {
    this.events.emit('column.theme', { theme });
  }

  async request<C extends keyof ColumnCommandMap & string>(command: C, payload?: ColumnCommandMap[C]['request']): Promise<ColumnCommandMap[C]['response']> {
    this.calls.push({ command, payload });
    await new Promise((resolve) => setTimeout(resolve, this.latencyMs));
    return this.answer(command, payload) as ColumnCommandMap[C]['response'];
  }

  private state() {
    return {
      theme: this.theme,
      mode: this.mode,
      preview: this.preview,
      rebar: this.workbook.ready ? this.rebar : null,
      workbook: this.workbook,
    };
  }

  private answer(command: keyof ColumnCommandMap, payload: unknown): unknown {
    switch (command) {
      case 'column.load':
      case 'column.reload':
        return this.state();
      case 'column.generate':
        this.generated++;
        return { success: true, created: this.rebar?.barCount ?? 0, deleted: 0, warnings: this.generateWarnings, error: '' };
      case 'column.probe':
        return this.workbook;
      case 'column.write': {
        if (!this.workbook.ready) return { success: false, storeysWritten: 0, error: this.workbook.error, rowsWithoutBars: [], workbook: this.workbook };
        this.written = payload as KataColumnWriteRequest;
        return { success: true, storeysWritten: this.preview?.rows.length ?? 0, error: '', rowsWithoutBars: this.rowsWithoutBars, workbook: this.workbook };
      }
      case 'column.close':
        this.closed = true;
        return null;
    }
  }
}
