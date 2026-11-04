// Drift guard for the hand-written columnContracts.ts: reads the C# records and constants of the Kata Column Rebar
// window and its preview as text and checks every field, command and event name has its camelCase twin here.
import { describe, expect, it } from 'vitest';
import contractsSource from '../../../HPRebar/KataExport/View/Web/ColumnExport/KataColumnWebContracts.cs?raw';
import previewSource from '../../../HPRebar.Core/KataExport/ColumnStack/KataColumnPreview.cs?raw';
import summarySource from '../../../HPRebar.Core/KataRebar/ColumnStack/KataColumnRebarSummary.cs?raw';
import sectionSource from '../../../HPRebar.Core/KataRebar/ColumnStack/KataColumnSectionDrawing.cs?raw';
import drawingSource from '../../../HPRebar.Core/KataRebar/ColumnStack/KataColumnDrawing.cs?raw';
import chainSource from '../../../HPRebar.Core/KataRebar/Calculators/KataDimChains.cs?raw';
import typescript from './columnContracts.ts?raw';

const csharp = [contractsSource, previewSource, summarySource, sectionSource, drawingSource, chainSource].join('\n');

const camel = (name: string) => name[0]!.toLowerCase() + name.slice(1);

function recordFields(name: string): string[] {
  const positional = new RegExp(`record ${name}\\(([^;{]*?)\\)\\s*[;{]`, 's').exec(csharp);
  if (!positional) throw new Error(`record ${name} not found in the C# contracts`);
  return positional[1]!.split(',').map((parameter) => camel(parameter.trim().split(/\s+/)[1]!));
}

function interfaceKeys(name: string): string[] {
  const match = new RegExp(`export interface ${name} \\{([^}]*)\\}`, 's').exec(typescript);
  if (!match) throw new Error(`interface ${name} not found in columnContracts.ts`);
  return [...match[1]!.matchAll(/^\s*(\w+)\??:/gm)].map((m) => m[1]!);
}

function constants(name: string): string[] {
  const match = new RegExp(`class ${name}\\s*\\{(.*?)\\n\\}`, 's').exec(csharp);
  if (!match) throw new Error(`class ${name} not found in the C# contracts`);
  return [...match[1]!.matchAll(/const string \w+ = "([^"]+)"/g)].map((m) => m[1]!);
}

const quotedKeys = (name: string) => {
  const match = new RegExp(`export interface ${name} \\{(.*?)\\n\\}`, 's').exec(typescript);
  if (!match) throw new Error(`interface ${name} not found in columnContracts.ts`);
  return [...match[1]!.matchAll(/^\s*'([^']+)':/gm)].map((m) => m[1]!);
};

const records = [
  'KataColumnPreviewRow',
  'KataColumnPreview',
  'KataColumnWorkbook',
  'KataColumnPageState',
  'KataColumnWriteRequest',
  'KataColumnWriteResponse',
  'KataColumnThemeEvent',
  'KataColumnRebarSummaryRow',
  'KataColumnRebarSummary',
  'KataColumnGenerateResponse',
  'KataColumnSectionBar',
  'KataColumnSectionTie',
  'KataColumnSectionDrawing',
  'KataColumnDrawingStorey',
  'KataColumnDrawingLevel',
  'KataColumnDrawingBar',
  'KataColumnDrawingHoopZone',
  'KataColumnDrawingCut',
  'KataColumnElevationDrawing',
  'KataDimChain',
];

describe('column contracts', () => {
  it.each(records)('%s has the fields of the C# record, in order', (name) => {
    expect(interfaceKeys(name)).toEqual(recordFields(name));
  });

  it('knows every command and event the window registers', () => {
    expect(quotedKeys('ColumnCommandMap').sort()).toEqual(constants('KataColumnWebCommands').sort());
    expect(quotedKeys('ColumnEventMap').sort()).toEqual(constants('KataColumnWebEvents').sort());
  });
});
