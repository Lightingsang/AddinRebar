// Text of the elevation as the WPF canvas set it (KataDrawPrimitives): Segoe UI 11 px, small 9.5 px, SemiBold for
// bold. Widths decide which labels fit, so they are measured with the browser's own font metrics; without a canvas
// (tests) an average Segoe UI advance stands in.

export const TEXT_SIZE = 11;
export const SMALL_TEXT_SIZE = 9.5;
export const FONT_FAMILY = "'Segoe UI', system-ui, sans-serif";

/** WPF's FormattedText height for Segoe UI: the line height, 1.33 em. */
const LINE_HEIGHT_EM = 1.33;

export interface TextBox {
  text: string;
  size: number;
  bold: boolean;
  width: number;
  height: number;
}

let context: CanvasRenderingContext2D | null | undefined;

function measureWidth(text: string, size: number, bold: boolean, family: string): number {
  if (context === undefined) {
    try {
      // jsdom (tests) reports every getContext call as an error: skip it there.
      const noCanvas = typeof document === 'undefined' || navigator.userAgent.includes('jsdom');
      context = noCanvas ? null : document.createElement('canvas').getContext('2d');
    } catch {
      context = null; // jsdom has no canvas: fall back to the average advance below
    }
  }
  if (!context) return text.length * size * (bold ? 0.58 : 0.54);
  context.font = `${bold ? 600 : 400} ${size}px ${family}`;
  return context.measureText(text).width;
}

const cache = new Map<string, TextBox>();

export function measure(text: string, size = TEXT_SIZE, bold = false, family = FONT_FAMILY): TextBox {
  const key = `${family}|${size}|${bold ? 1 : 0}|${text}`;
  let box = cache.get(key);
  if (!box) {
    // CAD texts change size with every zoom step: keep the cache from growing without end.
    if (cache.size > 5000) cache.clear();
    box = { text, size, bold, width: measureWidth(text, size, bold, family), height: size * LINE_HEIGHT_EM };
    cache.set(key, box);
  }
  return box;
}
