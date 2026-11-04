import type { KataColumnPreviewRow } from './columnContracts';

const PAD = 24;
const LABEL_WIDTH = 96;

/**
 * The stack as Kata's elevation shows it: one direction (b, the column's own X) against height, each storey with
 * the beam depth at its head hatched. Width and height have their own scales (a 0.3 m column on a 20 m stack would
 * be a hairline at one scale): the sketch shows steps and offsets, it is not a drawing to measure.
 */
export function ColumnSketch({ rows, title }: { rows: KataColumnPreviewRow[]; title: string }) {
  if (rows.length === 0) return null;
  const minX = Math.min(...rows.map((r) => r.minXMm));
  const maxX = Math.max(...rows.map((r) => r.maxXMm));
  const bottom = rows[0]!.baseMm;
  const top = rows[rows.length - 1]!.topMm;
  const width = 240;
  const height = 420;
  const scaleX = ((width - 2 * PAD) * 0.6) / Math.max(maxX - minX, 1);
  const scale = (height - 2 * PAD) / Math.max(top - bottom, 1);
  const x = (mm: number) => LABEL_WIDTH + PAD + (width - 2 * PAD) * 0.2 + (mm - minX) * scaleX;
  const y = (mm: number) => height - PAD - (mm - bottom) * scale;

  return (
    <svg role="img" aria-label={title} viewBox={`0 0 ${LABEL_WIDTH + width} ${height}`} className="h-full max-h-[460px] w-full">
      {rows.map((row, i) => (
        <g key={`${i}-${row.level}`} data-storey={row.level}>
          <rect
            x={x(row.minXMm)}
            y={y(row.topMm)}
            width={(row.maxXMm - row.minXMm) * scaleX}
            height={(row.topMm - row.baseMm) * scale}
            className={row.placement === 'leftFace' ? 'fill-steel/15 stroke-steel' : row.placement === 'stub' ? 'fill-none stroke-muted-foreground' : 'fill-primary/10 stroke-accent-strong'}
            strokeDasharray={row.placement === 'stub' ? '4 3' : undefined}
            strokeWidth={1.5}
            rx={row.shape === 'circle' ? 4 : 0}
          />
          {row.beamDepthMm > 0 && (
            <rect
              x={x(minX) - 8}
              y={y(row.topMm)}
              width={(maxX - minX) * scaleX + 16}
              height={row.beamDepthMm * scale}
              className="fill-muted-foreground/15 stroke-muted-foreground"
              strokeDasharray="4 3"
              strokeWidth={1}
            />
          )}
          <line x1={4} x2={LABEL_WIDTH + width - 4} y1={y(row.baseMm)} y2={y(row.baseMm)} className="stroke-border" strokeWidth={1} />
          <text x={4} y={y(row.baseMm) - 4} className="fill-muted-foreground text-[10px]">
            {row.level}
          </text>
        </g>
      ))}
    </svg>
  );
}
