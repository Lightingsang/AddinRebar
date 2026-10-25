import { BridgeError } from '../bridge/bridgeClient';
import { EventHub } from '../bridge/eventHub';
import type { KataAction, KataBridge, KataCanvasInput, KataCommandMap, KataEventMap, KataPreviewColumn, KataUpdateRequest, KataViewState } from './kataContracts';
import type { KataWebColumn, KataWebScene, KataWebSection } from './kataScene';

const column = (letter: string, kind: string, row11: string, row19 = '', row20 = '', row21 = '', row22 = '', row23 = ''): KataPreviewColumn => ({
  letter, kind, row11, row19, row20, row21, row22, row23,
});

/** A two-span run like the sample model's, for `npm run dev` and the tests. */
export const mockKataColumns: KataPreviewColumn[] = [
  column('C', 'Cột', '400', '', '', '', '', 'A'),
  column('D', 'Nhịp 1', '6500', '250x600'),
  column('E', 'Cột', '400', '', '', '', '', 'B'),
  column('F', 'Nhịp 2', '6200', '250x600'),
  column('G', 'Cột', '400', '', '', '', '', 'C'),
];

export function mockKataState(): KataViewState {
  return {
    theme: 'light',
    header: {
      beamName: 'DY7', beamCount: '1', height: '600', width: '250', slabThickness: '120', axisGrid: '7', axisOffset: '0 mm',
      elevation: '+3.600', elementCount: '2 phần tử dầm', totalLength: '13 900 mm',
    },
    parameters: ['Mark', 'Comments', 'Kata_Name', 'Kata_Count'],
    nameParameter: 'Kata_Name',
    countParameter: 'Kata_Count',
    isReverse: false,
    isBusy: false,
    status: 'Sẵn sàng ghi sang Excel.',
    hasError: false,
    workbook: 'Workbook đích: KataDam.xlsm (sheet Dam)',
    warnings: [],
    columns: mockKataColumns,
    selectedColumn: -1,
    selectionText: 'Bấm vào hình hoặc bảng để chọn một cột Kata.',
    showRebar: true,
    showSection: true,
    showBarTags: true,
    can: { export: true, repick: true, loadRebar: true, generateRebar: false, undoRemoveBars: false },
  };
}

const kindName = (c: KataWebColumn, scene: KataWebScene) =>
  c.kind === 'span'
    ? `Nhịp ${c.spanNumber}`
    : c.kind === 'joint'
      ? 'Nối'
      : c.kind === 'freeEnd'
        ? 'Đầu tự do'
        : ({ foundation: 'Móng', beam: 'Dầm giao', column: 'Cột' } as const)[scene.elevation.supports.find((s) => s.columnIndex === c.index)?.kind ?? 'column'];

/** The table rows of a scene, as KataPreviewBuilder writes them. */
export function previewColumns(scene: KataWebScene): KataPreviewColumn[] {
  return scene.elevation.columns.map((c) => column(c.letter, kindName(c, scene), c.row11, c.row19, c.row20, c.row21, c.row22, c.row23));
}

/** A plain elevation of the mock columns, so the dev page shows something drawn by "the host". */
function mockSvg(state: KataViewState, width: number, height: number): string {
  const lengths = state.columns.map((c) => Number(c.row11) || 0);
  const total = lengths.reduce((sum, length) => sum + length, 0) || 1;
  const scale = (width - 40) / total;
  const parts: string[] = [`<rect width="${width}" height="${height}" fill="${state.theme === 'dark' ? '#252526' : '#ffffff'}"/>`];
  let x = 20;
  const order = state.isReverse ? [...state.columns.keys()].reverse() : [...state.columns.keys()];
  for (const index of order) {
    const c = state.columns[index]!;
    const w = (lengths[index] ?? 0) * scale;
    const selected = index === state.selectedColumn;
    const top = height / 2 - 30;
    parts.push(c.kind.startsWith('Nhịp')
      ? `<rect x="${x}" y="${top}" width="${w}" height="40" fill="none" stroke="${selected ? '#f5a623' : '#00838f'}" stroke-width="1.5"/>`
      : `<rect x="${x}" y="${top - 20}" width="${w}" height="80" fill="${selected ? '#f5a623' : '#8e8e93'}" fill-opacity="0.4"/>`);
    parts.push(`<text x="${x + w / 2}" y="${top + 70}" font-size="11" text-anchor="middle" fill="#1b5e20">${c.letter}</text>`);
    x += w;
  }

  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" width="${width}" height="${height}">${parts.join('')}</svg>`;
}

/** Stands in for Revit in a normal browser: keeps a state, answers every Kata command and pushes state and drawing. */
export class KataMockBridge implements KataBridge {
  readonly isMock = true;
  readonly calls: { command: keyof KataCommandMap; payload: unknown }[] = [];
  private readonly events = new EventHub<KataEventMap>();
  private state = mockKataState();
  private viewport = { width: 800, height: 300 };
  private revision = 0;
  private readonly latencyMs: number;

  private readonly scene: KataWebScene | null;
  private readonly sections: Record<number, KataWebSection>;
  private readonly removals: string[][] = [];
  private sceneRevision = 1;
  private focusSerial = 0;

  /**
   * @param options.scene A scene the page draws itself (the B03 fixture in `npm run dev`); its columns fill the table.
   * @param options.sections Sections the host would build, by cut number.
   */
  constructor(options: { latencyMs?: number; state?: Partial<KataViewState>; scene?: KataWebScene; sections?: Record<number, KataWebSection> } = {}) {
    this.latencyMs = options.latencyMs ?? 150;
    this.scene = options.scene ?? null;
    this.sections = options.sections ?? {};
    this.state = { ...this.state, ...(this.scene ? { columns: previewColumns(this.scene), showRebar: false, showSection: false } : {}), ...options.state };
    // The host sends the scene once the page is up.
    if (this.scene) setTimeout(() => this.events.emit('kata.scene', { revision: 1, scene: this.scene }), this.latencyMs);
  }

  on: KataBridge['on'] = (event, handler) => this.events.on(event, handler);

  async request<C extends keyof KataCommandMap & string>(command: C, payload?: KataCommandMap[C]['request']): Promise<KataCommandMap[C]['response']> {
    this.calls.push({ command, payload });
    await new Promise((resolve) => setTimeout(resolve, this.latencyMs));
    return this.answer(command, payload) as KataCommandMap[C]['response'];
  }

  private answer(command: keyof KataCommandMap, payload: unknown): unknown {
    switch (command) {
      case 'kata.getState':
        return this.state;
      case 'kata.update':
        return this.update(payload as KataUpdateRequest);
      case 'kata.run':
        return this.run((payload as { action: KataAction }).action);
      case 'kata.viewport':
        this.viewport = payload as { width: number; height: number };
        this.draw();
        return null;
      case 'kata.canvas':
        return this.gesture(payload as KataCanvasInput);
      case 'kata.section':
        return this.sections[(payload as { number: number }).number] ?? null;
      case 'kata.removeBars':
        this.removals.push((payload as { keys: string[] }).keys);
        this.emitScene();
        return null;
      default:
        throw new BridgeError({ code: 'UNKNOWN_COMMAND', message: `Lệnh không tồn tại: ${String(command)}` });
    }
  }

  private update(request: KataUpdateRequest): KataViewState {
    const next = { ...this.state };
    if (request.nameParameter !== undefined) next.nameParameter = request.nameParameter;
    if (request.countParameter !== undefined) next.countParameter = request.countParameter;
    if (request.reverse !== undefined) next.isReverse = request.reverse;
    if (request.showRebar !== undefined) next.showRebar = request.showRebar;
    if (request.showSection !== undefined) next.showSection = request.showSection;
    if (request.showBarTags !== undefined) next.showBarTags = request.showBarTags;
    if (request.selectedColumn !== undefined) {
      next.selectedColumn = request.selectedColumn;
      const c = next.columns[request.selectedColumn];
      next.selectionText = c ? `Cột ${c.letter} · ${c.kind} · hàng 11 = ${c.row11}` : mockKataState().selectionText;
    }

    this.setState(next);
    return this.state;
  }

  private run(action: KataAction): { started: boolean } {
    if (action === 'export' && !this.state.can.export) {
      throw new BridgeError({ code: 'BUSY', message: `'${action}' is not available now` });
    }

    const status: Partial<Record<KataAction, string>> = {
      export: `Đã ghi ${this.state.columns.length} cột vào 'KataDam.xlsm' (sheet Dam).`,
      loadRebar: "Đã đọc thép dầm 'DY7': 24 thanh, 182.4 kg — kiểm tra rồi bấm Tạo thép.",
      refreshExcel: 'Sẵn sàng ghi sang Excel.',
    };
    if (action === 'frameAll') this.focus(null);
    if (action === 'undoRemoveBars' && this.removals.pop()) this.emitScene();
    if (action === 'previousSpan' || action === 'nextSpan') this.stepSpan(action === 'nextSpan' ? 1 : -1);
    if (status[action]) {
      this.setState({ ...this.state, status: status[action]!, hasError: false, can: { ...this.state.can, generateRebar: action === 'loadRebar' || this.state.can.generateRebar } });
    }

    return { started: true };
  }

  /** The scene without the bar groups removed so far, as the view model would send it. */
  private emitScene(): void {
    if (!this.scene?.rebar) return;
    const removed = new Set(this.removals.flat());
    const lines = this.scene.rebar.lines.filter((l) => !l.keys.some((k) => removed.has(k)));
    this.setState({ ...this.state, can: { ...this.state.can, undoRemoveBars: this.removals.length > 0 } });
    this.events.emit('kata.scene', { revision: ++this.sceneRevision, scene: { ...this.scene, rebar: { ...this.scene.rebar, lines } } });
  }

  private focus(columnIndex: number | null): void {
    this.events.emit('kata.focus', { columnIndex, serial: ++this.focusSerial });
  }

  /** The span after or before the selected column (KataElevation.SpanStep), selected and framed. */
  private stepSpan(direction: number): void {
    const spans = this.state.columns.flatMap((c, i) => (c.kind.startsWith('Nhịp') ? [i] : []));
    const current = this.state.selectedColumn;
    const next = current < 0 ? (direction > 0 ? spans[0] : spans.at(-1)) : direction > 0 ? spans.find((i) => i > current) : spans.filter((i) => i < current).at(-1);
    if (next === undefined) return;
    this.update({ selectedColumn: next });
    this.focus(next);
  }

  private gesture(input: KataCanvasInput): { handled: boolean } {
    if (input.kind === 'click' && input.x !== undefined) {
      const index = Math.min(this.state.columns.length - 1, Math.max(0, Math.floor((input.x / this.viewport.width) * this.state.columns.length)));
      this.update({ selectedColumn: index });
    }

    return { handled: input.kind !== 'key' };
  }

  private setState(next: KataViewState): void {
    this.state = next;
    this.events.emit('kata.state', next);
    this.draw();
  }

  private draw(): void {
    const { width, height } = this.viewport;
    this.events.emit('kata.drawing', { revision: ++this.revision, width, height, svg: mockSvg(this.state, width, height), hasBarSelection: false });
  }
}
