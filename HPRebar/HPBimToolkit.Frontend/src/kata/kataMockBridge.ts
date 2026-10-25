import { BridgeError } from '../bridge/bridgeClient';
import { EventHub } from '../bridge/eventHub';
import type { KataAction, KataBridge, KataCommandMap, KataEventMap, KataPreviewColumn, KataUpdateRequest, KataViewState } from './kataContracts';
import type { KataWeb3dModel, KataWebColumn, KataWebScene, KataWebSection } from './kataScene';

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

/** Stands in for Revit in a normal browser: keeps a state, answers every Kata command and pushes state and scene. */
export class KataMockBridge implements KataBridge {
  readonly isMock = true;
  readonly calls: { command: keyof KataCommandMap; payload: unknown }[] = [];
  private readonly events = new EventHub<KataEventMap>();
  private state = mockKataState();
  private readonly latencyMs: number;

  private readonly scene: KataWebScene | null;
  private readonly sections: Record<number, KataWebSection>;
  private readonly model3d: KataWeb3dModel | null;
  private readonly sceneError: string | null;
  private sceneDelayMs: number | null = null;
  private readonly removals: string[][] = [];
  private sceneRevision = 0;
  private focusSerial = 0;

  /**
   * @param options.scene A scene the page draws itself (the B03 fixture in `npm run dev`); its columns fill the table.
   * @param options.sections Sections the host would build, by cut number.
   * @param options.model3d The plan in 3D, without the bars struck off so far.
   * @param options.sceneError Sent instead of a scene, as the host does when the mapper throws.
   */
  constructor(
    options: { latencyMs?: number; state?: Partial<KataViewState>; scene?: KataWebScene; sections?: Record<number, KataWebSection>; model3d?: KataWeb3dModel; sceneError?: string } = {},
  ) {
    this.latencyMs = options.latencyMs ?? 150;
    this.scene = options.scene ?? null;
    this.sections = options.sections ?? {};
    this.model3d = options.model3d ?? null;
    this.sceneError = options.sceneError ?? null;
    this.state = { ...this.state, ...(this.scene ? { columns: previewColumns(this.scene), showRebar: false, showSection: false } : {}), ...options.state };
  }

  /** Holds the first scene back, to see the page before it arrives. */
  withSceneDelay(ms: number): this {
    this.sceneDelayMs = ms;
    return this;
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
      case 'kata.ready':
        // The host answers and sends the scene after it.
        setTimeout(() => this.sendScene(), this.sceneDelayMs ?? this.latencyMs);
        return null;
      case 'kata.update':
        return this.update(payload as KataUpdateRequest);
      case 'kata.run':
        return this.run((payload as { action: KataAction }).action);
      case 'kata.section':
        return this.sections[(payload as { number: number }).number] ?? null;
      case 'kata.model3d': {
        if (!this.model3d) throw new BridgeError({ code: 'BAD_REQUEST', message: 'There is no rebar plan: read the bars from Excel first.' });
        const removed = new Set(this.removals.flat());
        return { ...this.model3d, groups: this.model3d.groups.filter((g) => !removed.has(g.key)) };
      }
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

  /** After a removal or its undo: the undo button follows, a new scene comes. */
  private emitScene(): void {
    if (!this.scene?.rebar) return;
    this.setState({ ...this.state, can: { ...this.state.can, undoRemoveBars: this.removals.length > 0 } });
    this.sendScene();
  }

  /** The scene without the bar groups removed so far, as the view model would send it (none: a null frame). */
  private sendScene(): void {
    if (!this.scene || this.sceneError) {
      this.events.emit('kata.scene', { revision: ++this.sceneRevision, scene: null, error: this.sceneError });
      return;
    }
    const removed = new Set(this.removals.flat());
    const rebar = this.scene.rebar && { ...this.scene.rebar, lines: this.scene.rebar.lines.filter((l) => !l.keys.some((k) => removed.has(k))) };
    this.events.emit('kata.scene', { revision: ++this.sceneRevision, scene: { ...this.scene, rebar } });
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

  private setState(next: KataViewState): void {
    this.state = next;
    this.events.emit('kata.state', next);
  }
}
