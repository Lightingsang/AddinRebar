import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { Badge, Card } from '../ui/Card';
import { Button } from '../ui/Button';
import { useToast } from '../ui/Toast';
import { ElevationView } from './ElevationView';
import { NativeElevation } from './elevation/NativeElevation';
import type { SharedBarSelection } from './elevation/useKataPicking';
import type { KataWebRebar } from './kataScene';
import { Model3dPanel } from './model3d/Model3dPanel';
import { KataColumnsTable } from './KataColumnsTable';
import { KataParameters } from './KataParameters';
import type { KataBridge, KataViewState } from './kataContracts';
import { useKata, type KataController } from './useKata';

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange(value: boolean): void }) {
  return (
    <label className="inline-flex cursor-pointer items-center gap-1 text-[12px] font-semibold">
      <input type="checkbox" className="accent-[var(--accent)]" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      {label}
    </label>
  );
}

/** `?renderer=native|svg` on the page forces one drawing, to compare them; otherwise the page draws what it can itself. */
const forcedRenderer = new URLSearchParams(window.location.search).get('renderer');

/** The page draws everything itself once the scene has come; the WPF picture only before that, or when forced. */
function drawsNatively(kata: KataController): boolean {
  if (!kata.scene) return false;
  return forcedRenderer !== 'svg';
}

function ElevationCard({ state, kata }: { state: KataViewState; kata: KataController }) {
  const notify = useToast();
  const busy = state.isBusy;
  const scene = kata.scene;
  const rebar = scene?.rebar ?? null;
  const [want3d, setWant3d] = useState(false);
  // One bar selection for both views, for the plan it was picked on.
  const [picked, setPicked] = useState<{ rebar: KataWebRebar | null; keys: string[] | null }>({ rebar: null, keys: null });
  const pickedKeys = picked.rebar === rebar ? picked.keys : null;
  const shared = useMemo<SharedBarSelection>(() => ({ keys: pickedKeys, set: (keys) => setPicked({ rebar, keys }) }), [pickedKeys, rebar]);
  const show3d = want3d && rebar !== null && drawsNatively(kata);
  const tab = (active: boolean) => `px-2 py-0.5 text-[12px] font-semibold ${active ? 'bg-[var(--accent)] text-white' : 'text-muted'}`;
  return (
    <Card title="Mặt đứng dải dầm" className="flex min-h-0 flex-col">
      <div className="mb-1.5 flex shrink-0 flex-wrap items-center gap-1.5">
        <div className="inline-flex overflow-hidden rounded border border-line" role="tablist" aria-label="Kiểu xem">
          <button type="button" role="tab" aria-selected={!show3d} className={tab(!show3d)} onClick={() => setWant3d(false)}>
            2D
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={show3d}
            className={tab(show3d)}
            disabled={rebar === null || !drawsNatively(kata)}
            title={rebar === null ? 'Đọc thép Excel trước' : 'Dầm và thép trong không gian'}
            onClick={() => setWant3d(true)}
          >
            3D
          </button>
        </div>
        {show3d ? null : (
          <>
            <Button icon="select" onClick={() => kata.run('previousSpan')} disabled={state.columns.length === 0}>
              Trước
            </Button>
            <Button onClick={() => kata.run('nextSpan')} disabled={state.columns.length === 0}>
              Tiếp
            </Button>
            <Toggle label="Đảo chiều" checked={state.isReverse} onChange={(reverse) => kata.update({ reverse })} />
            <Button icon="zoom" onClick={() => kata.run('frameAll')}>
              Toàn dải
            </Button>
            <span className="mx-1 h-4 w-px bg-line" />
            <Toggle label="Hiện thép" checked={state.showRebar} onChange={(showRebar) => kata.update({ showRebar })} />
            <Toggle label="Hiện số hiệu" checked={state.showBarTags} onChange={(showBarTags) => kata.update({ showBarTags })} />
            <Toggle label="Hiện mặt cắt" checked={state.showSection} onChange={(showSection) => kata.update({ showSection })} />
          </>
        )}
        <Button className="ml-auto" disabled={busy || !state.can.undoRemoveBars} onClick={() => kata.run('undoRemoveBars')} title="Ctrl+Z trên hình">
          Hoàn tác xóa
        </Button>
      </div>
      <div className="mb-1 shrink-0 truncate text-[12px] text-muted" data-testid="kata-selection">
        {state.selectionText}
      </div>
      <div className="min-h-[360px] flex-1">
        {show3d && rebar ? (
          <Model3dPanel
            rebar={rebar}
            loadModel3d={kata.loadModel3d}
            theme={state.theme}
            selectedKeys={shared.keys}
            onSelectKeys={shared.set}
            removeBars={kata.removeBars}
            canUndoRemoveBars={state.can.undoRemoveBars}
            undoRemoveBars={() => kata.run('undoRemoveBars')}
            onUnhandledEscape={() => kata.run('close')}
          />
        ) : scene && drawsNatively(kata) ? (
          <NativeElevation
            sharedSelection={shared}
            elevation={scene.elevation}
            rebar={scene.rebar}
            stationMap={scene.stationMap}
            showRebar={state.showRebar}
            showBarTags={state.showBarTags}
            showSection={state.showSection}
            canUndoRemoveBars={state.can.undoRemoveBars}
            loadSection={kata.loadSection}
            removeBars={kata.removeBars}
            undoRemoveBars={() => kata.run('undoRemoveBars')}
            beamName={state.header.beamName}
            notify={(text) => notify(text, 'error')}
            theme={state.theme}
            selectedColumn={state.selectedColumn}
            focus={kata.focus}
            onSelectColumn={(selectedColumn) => kata.update({ selectedColumn })}
            onUnhandledEscape={() => kata.run('close')}
          />
        ) : (
          <ElevationView drawing={kata.drawing} gesture={kata.gesture} setViewport={kata.setViewport} onUnhandledEscape={() => kata.run('close')} />
        )}
      </div>
      <p className="mt-1 shrink-0 text-[11px] text-muted">
        {show3d
          ? 'Kéo chuột trái: xoay · chuột phải hoặc giữa: di chuyển · lăn: phóng to · bấm: chọn nhóm thép (Delete xóa, Ctrl+Z hoàn tác)'
          : 'Lăn chuột: phóng to · giữ chuột giữa hoặc Shift + kéo: di chuyển · nháy đúp chuột giữa: toàn dải · bấm: chọn cột / nhóm thép'}
      </p>
    </Card>
  );
}

function Warnings({ warnings }: { warnings: string[] }) {
  if (warnings.length === 0) return null;
  return (
    <Card title="Cảnh báo">
      <ul className="max-h-28 list-disc overflow-y-auto pl-4 text-[12px] text-steel" data-testid="kata-warnings">
        {warnings.map((warning) => (
          <li key={warning}>{warning}</li>
        ))}
      </ul>
    </Card>
  );
}

function Footer({ state, kata }: { state: KataViewState; kata: KataController }) {
  const busy = state.isBusy;
  return (
    <footer className="flex flex-wrap items-center gap-2 border-t border-line bg-panel px-3 py-2">
      <span className="text-[12px] text-muted">
        Số phần tử: <b className="text-ink">{state.header.elementCount}</b> · Tổng chiều dài: <b className="text-ink">{state.header.totalLength}</b>
      </span>
      <span role="status" className={`min-w-0 flex-1 truncate text-[12px] ${state.hasError ? 'text-danger' : 'text-success'}`} title={state.status}>
        {state.status}
      </span>
      <Button disabled={!state.can.loadRebar} busy={busy} onClick={() => kata.run('loadRebar')}>
        Đọc thép Excel
      </Button>
      <Button disabled={!state.can.generateRebar} onClick={() => kata.run('generateRebar')}>
        Tạo thép Revit
      </Button>
      <Button variant="ghost" onClick={() => kata.run('close')}>
        Đóng
      </Button>
      <Button variant="primary" disabled={!state.can.export} onClick={() => kata.run('export')}>
        Xuất Excel
      </Button>
    </footer>
  );
}

function Shell({ children }: { children: ReactNode }) {
  return <div className="flex h-full min-w-0 flex-col">{children}</div>;
}

/** The Kata Export window: the same sheet, drawing and commands as the WPF window, from the same view model. */
export function KataApp({ bridge }: { bridge: KataBridge }) {
  const notify = useToast();
  const kata = useKata(bridge, notify);
  const state = kata.state;

  useEffect(() => {
    if (state) document.documentElement.classList.toggle('dark', state.theme === 'dark');
  }, [state?.theme, state]);

  if (!state) {
    return (
      <Shell>
        <div className="m-auto text-[13px] text-muted">{kata.loadError ?? 'Đang đọc dải dầm…'}</div>
      </Shell>
    );
  }

  return (
    <Shell>
      <header className="flex flex-wrap items-center gap-2 border-b border-line bg-panel px-3 py-2">
        <Badge tone="accent">HPRebar Kata Pro</Badge>
        <h1 className="text-[14px] font-bold tracking-wide">XUẤT DẦM SANG KATA EXCEL</h1>
        <div className="ml-auto flex items-center gap-2">
          <Button icon="select" disabled={!state.can.repick} onClick={() => kata.run('repick')}>
            Chọn lại dầm
          </Button>
          <Button icon="refresh" onClick={() => kata.run('refreshExcel')}>
            Kiểm tra Excel
          </Button>
          <span className="text-[12px]" data-testid="kata-workbook">
            <b>Excel: </b>
            {state.workbook}
          </span>
          {bridge.isMock && <Badge tone="warning">dữ liệu mẫu</Badge>}
        </div>
      </header>
      <main className="grid min-h-0 flex-1 grid-cols-1 gap-2 overflow-y-auto p-2 lg:grid-cols-[minmax(260px,320px)_1fr] lg:grid-rows-[minmax(500px,1fr)_auto_auto]">
        <KataParameters state={state} update={kata.update} />
        <ElevationCard state={state} kata={kata} />
        <div className="lg:col-span-2">
          <KataColumnsTable columns={state.columns} selected={state.selectedColumn} onSelect={(selectedColumn) => kata.update({ selectedColumn })} />
        </div>
        <div className="lg:col-span-2">
          <Warnings warnings={state.warnings} />
        </div>
      </main>
      <Footer state={state} kata={kata} />
    </Shell>
  );
}
