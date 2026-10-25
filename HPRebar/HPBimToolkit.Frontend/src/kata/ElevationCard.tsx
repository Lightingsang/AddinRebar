import { useMemo, useState, type ReactNode } from 'react';
import { ArrowLeftRight, ChevronLeft, ChevronRight, Layers, Maximize, PanelsTopLeft, Tag, Undo2, Waypoints } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { NativeElevation } from './elevation/NativeElevation';
import type { SharedBarSelection } from './elevation/useKataPicking';
import type { KataViewState } from './kataContracts';
import type { KataWebRebar } from './kataScene';
import { Model3dPanel } from './model3d/Model3dPanel';
import type { KataController } from './useKata';
import { useT } from './i18n';

/** A labelled checkbox of the toolbar, with its icon between the box and the text. */
function Check({ label, icon, checked, onChange }: { label: string; icon: ReactNode; checked: boolean; onChange(checked: boolean): void }) {
  return (
    <label className="inline-flex cursor-pointer items-center gap-1.5 rounded-md px-1 py-1 text-[12.5px] font-medium whitespace-nowrap hover:bg-secondary">
      <input type="checkbox" className="size-3.5 accent-[var(--accent)]" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      <span className="text-accent-strong [&_svg]:size-3.5" aria-hidden="true">
        {icon}
      </span>
      {label}
    </label>
  );
}

/** The drawing: the run's elevation (2D) or the plan in 3D, with the span, direction and layer controls above it. */
export function ElevationCard({ state, kata }: { state: KataViewState; kata: KataController }) {
  const t = useT();
  const busy = state.isBusy;
  const scene = kata.scene;
  const rebar = scene?.rebar ?? null;
  const [want3d, setWant3d] = useState(false);
  // One bar selection for both views, for the plan it was picked on.
  const [picked, setPicked] = useState<{ rebar: KataWebRebar | null; keys: string[] | null }>({ rebar: null, keys: null });
  const pickedKeys = picked.rebar === rebar ? picked.keys : null;
  const shared = useMemo<SharedBarSelection>(() => ({ keys: pickedKeys, set: (keys) => setPicked({ rebar, keys }) }), [pickedKeys, rebar]);
  const show3d = want3d && rebar !== null;
  const noSpans = state.columns.length === 0;

  return (
    <section className="flex min-h-0 min-w-0 flex-col overflow-hidden rounded-lg border bg-card shadow-sm" aria-label={t.elevation}>
      <div className="flex shrink-0 flex-wrap items-center gap-x-0.5 gap-y-1 border-b px-3 py-2">
        <Waypoints className="size-4 text-accent-strong" aria-hidden="true" />
        <h2 className="mr-1.5 ml-1 text-[13.5px] font-bold whitespace-nowrap">{t.elevationTitle}</h2>
        <Tabs value={show3d ? '3d' : '2d'} onValueChange={(view) => setWant3d(view === '3d')} className="mr-1.5">
          <TabsList aria-label={t.viewKind} className="h-8">
            <TabsTrigger value="2d" className="px-2.5 text-[12px]">
              2D
            </TabsTrigger>
            <TabsTrigger value="3d" className="px-2.5 text-[12px]" disabled={rebar === null} title={rebar === null ? t.readRebarFirst : t.view3dHint}>
              3D
            </TabsTrigger>
          </TabsList>
        </Tabs>
        {!show3d && (
          <>
            <Button variant="ghost" size="sm" className="px-2 text-[12.5px]" onClick={() => kata.run('previousSpan')} disabled={noSpans} title={t.previousHint}>
              <ChevronLeft />
              {t.previous}
            </Button>
            <Button variant="ghost" size="sm" className="px-2 text-[12.5px]" onClick={() => kata.run('nextSpan')} disabled={noSpans} title={t.nextHint}>
              {t.next}
              <ChevronRight />
            </Button>
            <Check label={t.reverse} icon={<ArrowLeftRight />} checked={state.isReverse} onChange={(reverse) => kata.update({ reverse })} />
            <Button variant="ghost" size="sm" className="px-2 text-[12.5px]" onClick={() => kata.run('frameAll')} title={t.frameAllHint}>
              <Maximize />
              {t.frameAll}
            </Button>
            <Check label={t.showRebar} icon={<Layers />} checked={state.showRebar} onChange={(showRebar) => kata.update({ showRebar })} />
            <Check label={t.showTags} icon={<Tag />} checked={state.showBarTags} onChange={(showBarTags) => kata.update({ showBarTags })} />
            <Check label={t.showSections} icon={<PanelsTopLeft />} checked={state.showSection} onChange={(showSection) => kata.update({ showSection })} />
          </>
        )}
        <div className="ml-auto flex min-w-0 flex-1 basis-[7.5rem] items-center justify-end gap-1">
          <Button variant="ghost" size="icon-sm" aria-label={t.undoRemove} disabled={busy || !state.can.undoRemoveBars} onClick={() => kata.run('undoRemoveBars')} title={t.undoRemoveHint}>
            <Undo2 />
          </Button>
          <span
            className="max-w-[6.5rem] min-w-0 truncate rounded-md bg-primary px-2.5 py-1 text-[12px] font-semibold text-primary-foreground"
            data-testid="kata-selection"
            title={state.selectionText}
          >
            {state.selectionText}
          </span>
        </div>
      </div>

      <div className="relative min-h-0 flex-1 p-2">
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
        ) : scene ? (
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
            theme={state.theme}
            selectedColumn={state.selectedColumn}
            focus={kata.focus}
            onSelectColumn={(selectedColumn) => kata.update({ selectedColumn })}
            onUnhandledEscape={() => kata.run('close')}
          />
        ) : (
          <div
            className={`flex h-full items-center justify-center rounded-md border border-dashed px-4 text-center text-[12.5px] ${kata.sceneError ? 'text-danger' : 'text-muted-foreground'}`}
            data-testid="kata-elevation-waiting"
            role={kata.sceneArrived ? 'alert' : undefined}
          >
            {kata.sceneError ?? (kata.sceneArrived ? t.noElevation : t.buildingElevation)}
          </div>
        )}
      </div>
    </section>
  );
}
