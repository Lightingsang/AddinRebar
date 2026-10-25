// Colours and pens of the elevation (port of KataCanvasPalette.cs), from the HPRebar theme tokens of ThemeDark.xaml /
// ThemeLight.xaml and the CAD colours of Kata's drawing (T2-DY7.dwg layers). WPF dash lengths are in pen widths;
// they are given here in pixels.

import type { KataTheme } from '../kataContracts';

export interface Pen {
  color: string;
  width: number;
  dash?: string;
  /** Kata's pens end flat and join round (KataCadPens); the plain elevation's use the SVG defaults. */
  cad?: boolean;
}

export interface Palette {
  fill: string;
  text: string;
  mutedText: string;
  accent: string;
  selected: string;
  dimText: string;
  bubbleText: string;
  beamFill: string;
  supportFill: string;
  selectionFill: string;
  outline: Pen;
  breakLine: Pen;
  dimension: Pen;
  gridLine: Pen;
  bubble: Pen;
  marker: Pen;
  /** Kata's own layers, for the rebar drawing. */
  kata: { bar: string; stirrup: string; leader: string; circle: string; tagText: string; number: string; outline: string; hidden: string; grey: string; flag: string; run: string };
}

const alpha = (hex: string, a: number) => `${hex}${a.toString(16).padStart(2, '0')}`;

const dash = (width: number, ...pattern: number[]) => pattern.map((p) => +(p * width).toFixed(2)).join(' ');

function make(dark: boolean): Palette {
  const tokens = dark
    ? { fill: '#252526', bound: '#cccccc', tag: '#8e8e93', selected: '#f5a623', marker: '#e5484d' }
    : { fill: '#ffffff', bound: '#333333', tag: '#6c6c70', selected: '#e07b00', marker: '#c1272d' };
  const outline = dark ? '#00e5ff' : '#00838f';
  const grey = dark ? '#808080' : '#737373';
  return {
    fill: tokens.fill,
    text: tokens.bound,
    mutedText: tokens.tag,
    accent: '#0696d7',
    selected: tokens.selected,
    dimText: dark ? '#00e676' : '#1b5e20',
    bubbleText: dark ? '#ffffff' : '#212121',
    beamFill: alpha(outline, 12),
    supportFill: alpha(outline, 20),
    selectionFill: alpha(tokens.selected, 48),
    outline: { color: outline, width: 1.4 },
    breakLine: { color: grey, width: 1.2 },
    dimension: { color: grey, width: 0.8 },
    gridLine: { color: grey, width: 0.8 },
    bubble: { color: dark ? '#d0d0d0' : '#424242', width: 1.2 },
    marker: { color: tokens.marker, width: 1.2, dash: dash(1.2, 5, 2, 1, 2) },
    kata: {
      bar: dark ? '#ff0000' : '#d00000',
      stirrup: dark ? '#ff00bf' : '#b00088',
      leader: grey,
      circle: dark ? '#ffffff' : '#212121',
      tagText: dark ? '#00ff00' : '#1b7a1b',
      number: dark ? '#ffffff' : '#212121',
      outline: dark ? '#00ffff' : '#00838f',
      hidden: dark ? '#c0c0c0' : '#606060',
      grey,
      flag: dark ? '#ffff00' : '#a08000',
      run: dark ? '#ff00ff' : '#b000b0',
    },
  };
}

const palettes = { dark: make(true), light: make(false) };

export const paletteFor = (theme: KataTheme): Palette => palettes[theme];
