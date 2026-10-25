// Inline SVG icons (no icon font: the page loads nothing from the network). 16 px, stroke = currentColor.

const paths = {
  refresh: 'M13.5 8a5.5 5.5 0 1 1-1.6-3.9M13.5 2.5v3h-3',
  zoom: 'M7 12.5a5.5 5.5 0 1 0 0-11 5.5 5.5 0 0 0 0 11ZM11 11l3.5 3.5M5 7h4M7 5v4',
  select: 'M3 3l4 10 1.5-4.5L13 7 3 3Z',
  warning: 'M8 2l6.5 11.5h-13L8 2ZM8 6.5v3M8 11.5v.5',
  tool: 'M10.5 2.5a3 3 0 0 0-3 4L3 11l2 2 4.5-4.5a3 3 0 0 0 4-3l-2 1-1.5-1.5 1-2Z',
  search: 'M7 12a5 5 0 1 0 0-10 5 5 0 0 0 0 10ZM10.5 10.5 14 14',
  layers: 'M8 2 14 5 8 8 2 5 8 2ZM2 8l6 3 6-3M2 11l6 3 6-3',
  document: 'M4 1.5h5L12.5 5v9.5h-8.5v-13ZM9 1.5V5h3.5',
  close: 'M4 4l8 8M12 4l-8 8',
  chevronLeft: 'M10 3.5 5.5 8l4.5 4.5',
  chevronRight: 'M6 3.5 10.5 8 6 12.5',
  expand: 'M2.5 6V2.5H6M10 2.5h3.5V6M13.5 10v3.5H10M6 13.5H2.5V10',
  undo: 'M5.5 3.5 2.5 6.5l3 3M2.5 6.5h7a3.5 3.5 0 0 1 0 7H7',
  cube: 'M8 1.5 14 4.5v7L8 14.5 2 11.5v-7L8 1.5ZM2 4.5l6 3 6-3M8 7.5v7',
  table: 'M2 3h12v10H2V3ZM2 6.5h12M2 10h12M6 3v10',
  sliders: 'M3 4.5h10M3 11.5h10M6 3v3M10 10v3',
  download: 'M8 2v8M5 7l3 3 3-3M3 13.5h10',
  rebar: 'M2 5.5h12M2 10.5h12M4.5 3.5v9M8 3.5v9M11.5 3.5v9',
  play: 'M5 3l8 5-8 5V3Z',
} as const;

export type IconName = keyof typeof paths;

export function Icon({ name, className = 'h-4 w-4' }: { name: IconName; className?: string }) {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <path d={paths[name]} />
    </svg>
  );
}
