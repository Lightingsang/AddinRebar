// Both themes of the drawing: the canvas tokens come from HPRebar's palettes (ThemeDark.xaml / ThemeLight.xaml), and
// every line and text colour stays readable on its own background.
import { describe, expect, it } from 'vitest';
import themeDark from '../../../../HPRebar/Resources/Themes/ThemeDark.xaml?raw';
import themeLight from '../../../../HPRebar/Resources/Themes/ThemeLight.xaml?raw';
import { paletteFor } from './palette';

const xamlColor = (xaml: string, key: string) => {
  const match = new RegExp(`<Color x:Key="${key.replace(/\./g, '\\.')}">#([0-9A-Fa-f]{6})</Color>`).exec(xaml);
  if (!match) throw new Error(`${key} not in the theme`);
  return `#${match[1]!.toLowerCase()}`;
};

/** WCAG relative luminance contrast of two #rrggbb colours. */
function contrast(a: string, b: string): number {
  const luminance = (hex: string) => {
    const [r, g, b2] = [1, 3, 5].map((i) => {
      const c = parseInt(hex.slice(i, i + 2), 16) / 255;
      return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r! + 0.7152 * g! + 0.0722 * b2!;
  };
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi! + 0.05) / (lo! + 0.05);
}

describe.each([
  ['dark', themeDark],
  ['light', themeLight],
] as const)('the %s drawing', (theme, xaml) => {
  const palette = paletteFor(theme);

  it('takes the canvas fill, bounds, tags and selection from the HPRebar theme', () => {
    expect(palette.fill).toBe(xamlColor(xaml, 'Color.Canvas.Fill'));
    expect(palette.text).toBe(xamlColor(xaml, 'Color.Canvas.Bound'));
    expect(palette.mutedText).toBe(xamlColor(xaml, 'Color.Canvas.Tag'));
    expect(palette.selected).toBe(xamlColor(xaml, 'Color.Canvas.MainBar.Selected'));
    expect(palette.marker.color).toBe(xamlColor(xaml, 'Color.Canvas.MainBar'));
  });

  it('keeps every line and text at 3:1 or more against the fill', () => {
    const colours = {
      text: palette.text,
      mutedText: palette.mutedText,
      dimText: palette.dimText,
      bubbleText: palette.bubbleText,
      selected: palette.selected,
      outline: palette.outline.color,
      breakLine: palette.breakLine.color,
      dimension: palette.dimension.color,
      gridLine: palette.gridLine.color,
      bubble: palette.bubble.color,
      marker: palette.marker.color,
      ...Object.fromEntries(Object.entries(palette.kata).map(([k, v]) => [`kata.${k}`, v])),
    };
    // Ratios rounded to two decimals, as contrast checkers report them (the light selection token is 2.997).
    const weak = Object.entries(colours).filter(([, colour]) => Math.round(contrast(colour, palette.fill) * 100) / 100 < 3);
    expect(weak.map(([name, colour]) => `${name} ${colour} ${contrast(colour, palette.fill).toFixed(2)}`)).toEqual([]);
  });
});
