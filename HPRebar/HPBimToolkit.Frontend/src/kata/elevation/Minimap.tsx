import { useRef, type PointerEvent } from 'react';
import type { KataWebElevation } from '../kataScene';
import type { Palette } from './palette';
import { toStation, type Viewport } from './viewport';

const WIDTH = 220;
const HEIGHT = 44;
const PAD = 6;

interface MinimapProps {
  elevation: KataWebElevation;
  viewport: Viewport;
  areaWidth: number;
  palette: Palette;
  /** Pans the drawing so that station `x` is at the middle of the area. */
  centreOn(x: number): void;
}

/**
 * The whole run in miniature with the part on screen framed: a click or a drag on it moves the view there. Spans and
 * supports only — enough to find one's way along a long beam.
 */
export function Minimap({ elevation, viewport, areaWidth, palette, centreOn }: MinimapProps) {
  const dragging = useRef(false);
  const { start, end } = elevation.bounds;
  const scale = (WIDTH - 2 * PAD) / Math.max(1, end - start);
  const mx = (station: number) => PAD + (station - start) * scale;
  const depth = Math.max(1, elevation.topMm - elevation.bottomMm);
  const my = (heightMm: number) => PAD + ((elevation.topMm - heightMm) / depth) * (HEIGHT - 2 * PAD);
  const left = Math.max(0, mx(toStation(viewport, 0)));
  const right = Math.min(WIDTH, mx(toStation(viewport, areaWidth)));

  const move = (e: PointerEvent<SVGSVGElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    centreOn(start + (e.clientX - rect.left - PAD) / scale);
  };

  return (
    <svg
      width={WIDTH}
      height={HEIGHT}
      className="absolute bottom-2 right-2 cursor-pointer rounded border"
      style={{ background: palette.fill, borderColor: palette.kata.grey }}
      data-testid="kata-minimap"
      aria-label="Toàn dải dầm thu nhỏ"
      onPointerDown={(e) => {
        e.stopPropagation();
        dragging.current = true;
        e.currentTarget.setPointerCapture?.(e.pointerId);
        move(e);
      }}
      onPointerMove={(e) => {
        e.stopPropagation();
        if (dragging.current) move(e);
      }}
      onPointerUp={(e) => {
        e.stopPropagation();
        dragging.current = false;
      }}
      onClick={(e) => e.stopPropagation()}
    >
      {elevation.columns.map((c) =>
        c.kind === 'support' ? (
          <rect key={c.index} x={mx(c.extent.start)} y={PAD - 3} width={Math.max(1, (c.extent.end - c.extent.start) * scale)} height={HEIGHT - 2 * PAD + 6} fill={palette.supportFill} />
        ) : null,
      )}
      {elevation.beams.map((b, i) => (
        <rect key={i} x={mx(b.extent.start)} y={my(b.topMm)} width={(b.extent.end - b.extent.start) * scale} height={Math.max(1, my(b.bottomMm) - my(b.topMm))} fill="none" stroke={palette.outline.color} strokeWidth={1} />
      ))}
      <rect x={left} y={1} width={Math.max(4, right - left)} height={HEIGHT - 2} fill={`${palette.accent}22`} stroke={palette.accent} strokeWidth={1.5} data-testid="kata-minimap-view" />
    </svg>
  );
}
