import { useEffect, useMemo, useRef, useState, type KeyboardEvent, type PointerEvent } from 'react';
import { endPress, startPress, wheelNotches, type Press } from '../elevationGestures';
import type { KataTheme } from '../kataContracts';
import type { KataFocusEvent, KataWebElevation, KataWebRange, KataWebRebar, KataWebSection, KataWebStationMap } from '../kataScene';
import { paintAnnotations } from './elevationAnnotations';
import { paintElevation } from './elevationPainter';
import { ZERO_WIDTH_REACH_PX, kataFrameOptions, type FrameOptions } from './framing';
import { kataPoint, paintKataElevation, type Point } from './kataElevationPainter';
import { barHits, describeKeys, flagAt } from './kataHit';
import { paintTags, tagBand } from './kataTagPainter';
import { paletteFor } from './palette';
import { CARD, cardFits, cardReserve, paintCutMarkers, selectedCut } from './plainSections';
import { buildScene, columnAt } from './scene';
import { SectionPanel } from './SectionPanel';
import { paintPicking } from './selectionPainter';
import { ShapesSvg } from './ShapesSvg';
import { visibleShapes } from './drawingTools';
import { ElevationTools } from './ElevationTools';
import { Minimap } from './Minimap';
import { useDrawingTools } from './useDrawingTools';
import { useBarSelection, useSection, type SharedBarSelection } from './useKataPicking';
import { useElevationView } from './useElevationView';
import { toStation } from './viewport';
import { useT } from '../i18n';

interface NativeElevationProps {
  elevation: KataWebElevation;
  /** Kata's rebar elevation, drawn instead of the plain one while the bars are shown. */
  rebar: KataWebRebar | null;
  stationMap: KataWebStationMap;
  showRebar: boolean;
  showBarTags: boolean;
  showSection: boolean;
  canUndoRemoveBars: boolean;
  theme: KataTheme;
  selectedColumn: number;
  focus: KataFocusEvent | null;
  onSelectColumn(index: number): void;
  loadSection(number: number): Promise<KataWebSection | null>;
  removeBars(keys: string[]): void;
  undoRemoveBars(): void;
  /** The beam's name, for the file names of saved drawings. */
  /** Esc the drawing did not use: the window closes on it, as the WPF window did. */
  onUnhandledEscape(): void;
  /** The bar selection shared with the 3D view; the elevation keeps its own without it. */
  sharedSelection?: SharedBarSelection;
}

/** The floating Kata section panel down the right of the drawing (KataElevationCanvas.Section), null when there is no room. */
function kataPanelRect(width: number, height: number) {
  if (width < 240 || height < 200) return null;
  const w = Math.min(350, width - 16);
  return { left: width - 8 - w, top: 8, width: w, height: height - 16 };
}

/**
 * The beam run drawn by the page itself from the scene, zoomed and panned in the browser (no round trip to Revit):
 * the plain elevation with its section card, or Kata's rebar elevation while the bars are shown. A click opens the
 * section of a flag, else selects the bar group under it (Delete removes it, Ctrl+Z undoes), else picks the column;
 * hovering a bar shows what it is.
 */
export function NativeElevation(props: NativeElevationProps) {
  const { elevation, rebar, stationMap, showRebar, showBarTags, showSection, theme, selectedColumn, focus } = props;
  const box = useRef<HTMLDivElement>(null);
  const svg = useRef<SVGSVGElement>(null);
  const press = useRef<(Press & { lastX: number; lastY: number }) | null>(null);
  const [openFlag, setOpenFlag] = useState<{ number: number; x: number } | null>(null);
  const [hover, setHover] = useState<{ keys: string[]; x: number; y: number } | null>(null);
  // A section flag under the cursor (it opens its section when clicked), for the pointing hand.
  const [overFlag, setOverFlag] = useState(false);

  // With the bars shown the run is drawn as Kata's elevation, framed with its own band; otherwise as read from Revit,
  // with the section card on the right when there is a plan and the sections are shown.
  const kata = showRebar && rebar ? rebar : null;
  const cardShown = !kata && showSection && rebar !== null;
  const tags = useMemo(() => (kata && showBarTags ? tagBand(kata, elevation.topMm - elevation.bottomMm) : null), [kata, showBarTags, elevation]);
  const options = useMemo<(FrameOptions & { range?: KataWebRange }) | undefined>(
    () => (kata ? kataFrameOptions(elevation, kata, stationMap, tags) : cardShown ? { reserve: cardReserve } : undefined),
    [kata, elevation, stationMap, tags, cardShown],
  );
  const view = useElevationView(box, elevation, selectedColumn, focus, options);
  const { viewport } = view;
  const withCard = cardShown && cardFits(view.size.width, view.size.height);
  const selection = useBarSelection(kata, props.sharedSelection);

  const scene = useMemo(() => {
    if (!viewport) return null;
    const above = (options?.band?.above ?? 0) * viewport.scale;
    const below = (options?.band?.below ?? 0) * viewport.scale;
    return buildScene(elevation, viewport, view.size.width, view.size.height, selectedColumn, above, below);
  }, [elevation, viewport, view.size.width, view.size.height, selectedColumn, options]);
  const point = useMemo(() => (scene ? kataPoint(scene, stationMap) : null), [scene, stationMap]);

  // The section shown: the flag clicked (Kata's elevation; found again by its station when bars are removed and the
  // cuts renumbered) or the cut of the picked column (the card beside the plain elevation).
  const kataCut = kata && showSection && openFlag ? (kata.cuts.find((c) => Math.abs(c.x - openFlag.x) < 1) ?? kata.cuts.find((c) => c.number === openFlag.number) ?? null) : null;
  const plainCut = withCard && scene && rebar ? selectedCut(scene, rebar.cuts, stationMap) : null;
  const panel = kataCut ? kataPanelRect(view.size.width, view.size.height) : plainCut ? { left: view.size.width - CARD.margin - CARD.width, top: CARD.margin, width: CARD.width, height: CARD.height } : null;
  const shownCut = panel ? (kataCut ?? plainCut) : null;
  const section = useSection(rebar, shownCut?.number ?? null, props.loadSection);
  const tools = useDrawingTools({ kata, scene, point, stationMap, frameRange: view.frameRange });

  const shapes = useMemo(() => {
    if (!scene || !point) return [];
    const palette = paletteFor(theme);
    if (kata) {
      return [
        ...paintKataElevation(scene, palette, kata, stationMap, kataCut?.number ?? null),
        ...(showBarTags ? paintTags(scene, palette, kata, stationMap) : []),
        ...paintPicking(kata.lines, point, palette, scene.viewport.scale, scene.height, selection.keys, hover?.keys ?? null),
      ];
    }
    const plain = [...paintElevation(scene, palette), ...paintAnnotations(scene, palette)];
    return showSection && rebar ? [...plain, ...paintCutMarkers(scene, palette, rebar.cuts, stationMap, plainCut)] : plain;
  }, [scene, point, theme, kata, stationMap, kataCut, showBarTags, selection.keys, hover, showSection, rebar, plainCut]);
  const drawn = [...visibleShapes(shapes, tools.hidden), ...tools.overlay(paletteFor(theme))];

  const at = (e: { clientX: number; clientY: number }): Point => {
    const element = box.current!;
    const rect = element.getBoundingClientRect();
    return [e.clientX - rect.left - element.clientLeft, e.clientY - rect.top - element.clientTop];
  };

  const onPointerDown = (e: PointerEvent<HTMLDivElement>) => {
    const [x, y] = at(e);
    const started = startPress(x, y, e.button, e.shiftKey);
    if (!started) return;
    press.current = { ...started, lastX: x, lastY: y };
    // Optional: the test DOM has no pointer capture.
    e.currentTarget.setPointerCapture?.(e.pointerId);
    e.currentTarget.focus();
    e.preventDefault();
  };

  const onPointerMove = (e: PointerEvent<HTMLDivElement>) => {
    const current = press.current;
    const p = at(e);
    if (current?.panning) {
      view.pan(p[0] - current.lastX, p[1] - current.lastY);
      current.lastX = p[0];
      current.lastY = p[1];
      return;
    }
    if (current) return;
    if (tools.measuring) {
      tools.measureMove(p);
      return;
    }
    if (!kata || !point || !viewport) return;
    const line = barHits(kata.lines, p, point)[0];
    setHover((before) => (line ? { keys: line.keys, x: p[0], y: p[1] } : before ? null : before));
    setOverFlag(showSection && !line && flagAt(kata.flags, p, point, viewport.scale) !== null);
  };

  const onPointerUp = (e: PointerEvent<HTMLDivElement>) => {
    const started = press.current;
    press.current = null;
    if (!started || !viewport || !point) return;
    const p = at(e);
    if (endPress(started, p[0], p[1])?.kind !== 'click') return;
    if (tools.measuring) {
      tools.measureClick(p);
      return;
    }
    if (kata) {
      const flag = showSection ? flagAt(kata.flags, p, point, viewport.scale) : null;
      if (flag) {
        setOpenFlag({ number: flag.number, x: flag.x });
        return;
      }
      if (selection.pick(p, point)) return;
      selection.clear();
    }
    const column = columnAt(elevation, toStation(viewport, p[0]), ZERO_WIDTH_REACH_PX / viewport.scale);
    if (column !== null) props.onSelectColumn(column);
  };

  // The wheel zooms the drawing and never scrolls the page: React's wheel listener is passive, so attach one that is not.
  const zoom = view.zoom;
  useEffect(() => {
    const element = box.current;
    if (!element) return;
    const onWheel = (e: globalThis.WheelEvent) => {
      e.preventDefault();
      const rect = element.getBoundingClientRect();
      zoom(e.clientX - rect.left - element.clientLeft, e.clientY - rect.top - element.clientTop, wheelNotches(e.deltaY, e.deltaMode));
    };
    element.addEventListener('wheel', onWheel, { passive: false });
    return () => element.removeEventListener('wheel', onWheel);
  }, [zoom]);

  /** Esc steps back (measure, section, selection, window); Delete removes the group; Ctrl+Z undoes. */
  const onKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    const keys = selection.keys;
    if (e.key === 'Escape') {
      // The measure on screen and the measure tool go first, then the section, the selection, the window.
      const used = tools.escape();
      if (!used && kataCut) setOpenFlag(null);
      else if (!used && keys) selection.clear();
      else if (!used) props.onUnhandledEscape();
    } else if (e.key === 'Delete' && keys) {
      selection.clear();
      props.removeBars(keys);
    } else if ((e.key === 'z' || e.key === 'Z') && e.ctrlKey && props.canUndoRemoveBars) {
      selection.clear();
      props.undoRemoveBars();
    } else {
      return;
    }
    e.preventDefault();
  };

  const palette = paletteFor(theme);
  const t = useT();
  return (
    <div
      ref={box}
      tabIndex={0}
      role="img"
      aria-label={t.elevation}
      data-testid="kata-elevation"
      data-renderer="native"
      data-mode={kata ? 'kata' : 'plain'}
      data-scale={viewport?.scale.toFixed(5)}
      data-selected={selection.keys?.length ?? 0}
      data-measuring={tools.measuring || undefined}
      className="relative h-full min-h-0 w-full overflow-hidden rounded border border-line outline-none select-none"
      // An arrow over the drawing, a hand over what a click picks (a bar, a section flag), a cross only to measure.
      style={{ background: palette.fill, cursor: tools.measuring ? 'crosshair' : hover || overFlag ? 'pointer' : 'default' }}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={onPointerUp}
      onPointerLeave={() => {
        setHover(null);
        setOverFlag(false);
      }}
      onPointerCancel={() => (press.current = null)}
      onAuxClick={(e) => e.button === 1 && e.detail === 2 && view.frameWhole()}
      onKeyDown={onKeyDown}
    >
      {viewport && (
        <svg ref={svg} className="pointer-events-none absolute left-0 top-0" width={view.size.width} height={view.size.height}>
          <ShapesSvg shapes={drawn} />
        </svg>
      )}
      <ElevationTools
        kata={kata !== null}
        measuring={tools.measuring}
        onMeasure={tools.setMeasuring}
        hidden={tools.hidden}
        onToggleLayer={tools.toggleLayer}
        onFind={tools.find}
      />
      {viewport && view.size.width >= 480 && !panel && (
        <Minimap elevation={elevation} viewport={viewport} areaWidth={view.size.width} palette={palette} centreOn={view.centreOn} />
      )}
      {hover && !press.current && !tools.measuring && (
        <div
          role="tooltip"
          className="pointer-events-none absolute z-10 max-w-[320px] rounded border px-2 py-1 text-[11px] shadow"
          style={{ left: hover.x + 14, top: hover.y + 14, background: palette.fill, color: palette.text, borderColor: palette.accent }}
        >
          {describeKeys(hover.keys, t).map((text) => (
            <div key={text}>{text}</div>
          ))}
          <div style={{ color: palette.mutedText }}>{t.pickHint}</div>
        </div>
      )}
      {panel && shownCut && section && (
        <SectionPanel
          number={shownCut.number}
          state={section}
          theme={theme}
          rect={panel}
          caption={kataCut ? undefined : `MẶT CẮT ${shownCut.number}-${shownCut.number}`}
          onClose={kataCut ? () => setOpenFlag(null) : undefined}
        />
      )}
    </div>
  );
}
