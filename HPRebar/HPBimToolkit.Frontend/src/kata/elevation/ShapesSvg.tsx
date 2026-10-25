import { memo } from 'react';
import type { Pen } from './palette';
import type { Shape } from './shapes';
import { FONT_FAMILY } from './text';

const r2 = (value: number) => Math.round(value * 100) / 100;

/** SVG stroke attributes of a pen; Kata's pens end flat and join round, as KataCadPens draws them. */
const stroke = (pen: Pen | undefined) =>
  pen
    ? { stroke: pen.color, strokeWidth: pen.width, strokeDasharray: pen.dash, strokeLinecap: pen.cad ? ('butt' as const) : undefined, strokeLinejoin: pen.cad ? ('round' as const) : undefined }
    : {};

/** Draws painter shapes as SVG elements, in screen pixels (a text's `top` is the top of its line, as in WPF). */
export const ShapesSvg = memo(function ShapesSvg({ shapes }: { shapes: Shape[] }) {
  return (
    <>
      {shapes.map((s, i) => {
        switch (s.t) {
          case 'line':
            return <line key={i} x1={r2(s.x1)} y1={r2(s.y1)} x2={r2(s.x2)} y2={r2(s.y2)} {...stroke(s.pen)} />;
          case 'poly': {
            const points = s.points.map(([x, y]) => `${r2(x)},${r2(y)}`).join(' ');
            const Tag = s.closed ? 'polygon' : 'polyline';
            return <Tag key={i} points={points} fill={s.fill ?? 'none'} data-id={s.id} {...stroke(s.pen)} />;
          }
          case 'path':
            return <path key={i} d={s.d} fill={s.fill ?? 'none'} {...stroke(s.pen)} />;
          case 'ctext':
            return (
              <text
                key={i}
                transform={`translate(${r2(s.x)} ${r2(s.y)})${s.vertical ? ' rotate(-90)' : ''} scale(${s.widthFactor} 1)`}
                textAnchor={s.anchor}
                fontFamily="Arial, sans-serif"
                fontSize={r2(s.size)}
                fill={s.color}
              >
                {s.text}
              </text>
            );
          case 'rect':
            return (
              <rect
                key={i}
                x={r2(Math.min(s.x1, s.x2))}
                y={r2(Math.min(s.y1, s.y2))}
                width={r2(Math.abs(s.x2 - s.x1))}
                height={r2(Math.abs(s.y2 - s.y1))}
                fill={s.fill ?? 'none'}
                stroke={s.pen?.color}
                strokeWidth={s.pen?.width}
              />
            );
          case 'circle':
            return <circle key={i} cx={r2(s.x)} cy={r2(s.y)} r={r2(s.r)} fill={s.fill ?? 'none'} stroke={s.pen.color} strokeWidth={s.pen.width} />;
          case 'text':
            return (
              <text
                key={i}
                x={r2(s.x)}
                y={r2(s.top)}
                dominantBaseline="text-before-edge"
                textAnchor={s.anchor}
                fontFamily={FONT_FAMILY}
                fontSize={s.box.size}
                fontWeight={s.box.bold ? 600 : 400}
                fill={s.color}
              >
                {s.box.text}
              </text>
            );
        }
      })}
    </>
  );
});
