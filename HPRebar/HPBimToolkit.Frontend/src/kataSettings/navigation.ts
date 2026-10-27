// The page's outline, shared by the side navigation and the sections: Kata's three tabs as groups, each with its cards.
import { Anchor, Box, Columns3, Eye, Link2, Ruler, Scissors, Spline, Table2, Waypoints, Weight, type LucideIcon } from 'lucide-react';
import type { SettingsStrings } from './i18n';

export type GroupId = 'detail' | 'special' | 'joints';

export interface NavSection {
  id: string;
  title(t: SettingsStrings): string;
  icon: LucideIcon;
}

export interface NavGroup {
  id: GroupId;
  title(t: SettingsStrings): string;
  description(t: SettingsStrings): string;
  sections: NavSection[];
}

export const NAV: NavGroup[] = [
  {
    id: 'detail',
    title: (t) => t.tabDetail,
    description: (t) => t.detailDescription,
    sections: [
      { id: 'sec-bars', title: (t) => t.barsAndHooks, icon: Spline },
      { id: 'sec-zones', title: (t) => t.zones, icon: Ruler },
      { id: 'sec-side-bars', title: (t) => t.sideBars, icon: Anchor },
      { id: 'sec-drawing', title: (t) => t.drawing, icon: Eye },
      { id: 'sec-shop', title: (t) => t.shop, icon: Scissors },
      { id: 'sec-laps', title: (t) => t.laps, icon: Link2 },
    ],
  },
  {
    id: 'special',
    title: (t) => t.tabSpecial,
    description: (t) => t.specialDescription,
    sections: [
      { id: 'sec-lap-info', title: (t) => t.lapInfo, icon: Waypoints },
      { id: 'sec-mass-table', title: (t) => t.massTable, icon: Weight },
      { id: 'sec-lap-table', title: (t) => t.lapTable, icon: Table2 },
    ],
  },
  {
    id: 'joints',
    title: (t) => t.tabJoints,
    description: (t) => t.jointsDescription,
    sections: [
      { id: 'sec-joint-beam', title: (t) => t.jointBeam, icon: Columns3 },
      { id: 'sec-joint-column', title: (t) => t.jointColumn, icon: Box },
    ],
  },
];

export const sectionOf = (id: string) => NAV.flatMap((g) => g.sections).find((s) => s.id === id)!;
