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
} as const;

export type IconName = keyof typeof paths;

export function Icon({ name, className = 'h-4 w-4' }: { name: IconName; className?: string }) {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <path d={paths[name]} />
    </svg>
  );
}
