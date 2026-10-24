import type { CategoryCount, DocumentSnapshot, ModelMetrics } from './protocol';

// Sample data for working on the UI in a normal browser (npm run dev). Shapes match protocol v1 exactly.

export const mockDocument: DocumentSnapshot = {
  hasDocument: true,
  title: 'TOA-NHA-MAU-KT',
  path: 'D:\\Projects\\Toa nha mau\\TOA-NHA-MAU-KT.rvt',
  isWorkshared: true,
  isModified: false,
  isReadOnly: false,
  isFamily: false,
  activeView: { id: 31245, name: 'Tầng 2 - Mặt bằng', viewType: 'FloorPlan', scale: 100 },
};

export const mockModelCategories: CategoryCount[] = [
  { categoryId: -2000011, name: 'Walls', count: 1842 },
  { categoryId: -2000023, name: 'Doors', count: 412 },
  { categoryId: -2000014, name: 'Windows', count: 389 },
  { categoryId: -2000032, name: 'Floors', count: 156 },
  { categoryId: -2001330, name: 'Structural Columns', count: 248 },
  { categoryId: -2001320, name: 'Structural Framing', count: 1310 },
  { categoryId: -2000080, name: 'Furniture', count: 735 },
  { categoryId: -2001140, name: 'Mechanical Equipment', count: 96 },
  { categoryId: -2008044, name: 'Pipes', count: 2104 },
  { categoryId: -2008000, name: 'Ducts', count: 980 },
  { categoryId: -2000160, name: 'Rooms', count: 214 },
  { categoryId: -2000220, name: 'Grids', count: 34 },
  { categoryId: -2000240, name: 'Levels', count: 12 },
  { categoryId: -2000150, name: 'Generic Annotations', count: 58 },
  { categoryId: -2000038, name: 'Ceilings', count: 141 },
  { categoryId: -2000120, name: 'Stairs', count: 8 },
].sort((a, b) => b.count - a.count);

export const mockMetrics: ModelMetrics = {
  elementTypes: 1386,
  views: 214,
  sheets: 63,
  levels: 12,
  families: 402,
  warnings: 127,
  links: 3,
  worksets: 9,
};

export const mockToolIds = [
  'revit.default3DView',
  'revit.reviewWarnings',
  'revit.projectInformation',
  'revit.projectUnits',
  'revit.visibilityGraphics',
  'revit.manageLinks',
  'revit.worksets',
  'revit.purgeUnused',
];
