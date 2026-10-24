// Drift guard for the hand-written kataContracts.ts: reads the C# records and constants of the Kata Export window as
// text and checks every field, command, event, action and gesture name has its camelCase twin here.
import { describe, expect, it } from 'vitest';
import contractsSource from '../../../HPRebar/KataExport/View/Web/KataWebContracts.cs?raw';
import previewColumnSource from '../../../HPRebar/KataExport/ViewModel/KataPreviewColumn.cs?raw';
import typescript from './kataContracts.ts?raw';

const csharp = contractsSource + previewColumnSource;

const camel = (name: string) => name[0]!.toLowerCase() + name.slice(1);

/** Parameter names of `record Name(...)` (positional records only). */
function recordFields(name: string): string[] {
  const match = new RegExp(`record ${name}\\(([^;]*?)\\);`, 's').exec(csharp);
  if (!match) throw new Error(`record ${name} not found in the C# contracts`);
  return match[1]!.split(',').map((parameter) => camel(parameter.trim().split(/\s+/)[1]!.replace(/=.*/, '')));
}

/** Keys of `export interface Name { ... }`. */
function interfaceKeys(name: string): string[] {
  const match = new RegExp(`export interface ${name} \\{([^}]*)\\}`, 's').exec(typescript);
  if (!match) throw new Error(`interface ${name} not found in kataContracts.ts`);
  return [...match[1]!.matchAll(/^\s*(\w+)\??:/gm)].map((m) => m[1]!);
}

/** Values of `public const string X = "value";` inside `class Name`. */
function constants(name: string): string[] {
  const match = new RegExp(`class ${name}\\s*\\{(.*?)\\n\\}`, 's').exec(csharp);
  if (!match) throw new Error(`class ${name} not found in the C# contracts`);
  return [...match[1]!.matchAll(/const string \w+ = "([^"]+)"/g)].map((m) => m[1]!);
}

describe('kataContracts.ts mirrors KataWebContracts.cs', () => {
  it.each([
    ['KataViewState', 'KataViewState'],
    ['KataHeaderState', 'KataHeaderState'],
    ['KataCommandStates', 'KataCommandStates'],
    ['KataUpdateRequest', 'KataUpdateRequest'],
    ['KataCanvasInput', 'KataCanvasInput'],
    ['KataDrawingFrame', 'KataDrawingFrame'],
    ['KataPreviewColumn', 'KataPreviewColumn'],
  ])('%s has the same fields', (record, tsInterface) => {
    expect(interfaceKeys(tsInterface).sort()).toEqual(recordFields(record).sort());
  });

  it('every command and event has an entry in the maps', () => {
    for (const name of [...constants('KataWebCommands'), ...constants('KataWebEvents')]) {
      expect(typescript).toContain(`'${name}':`);
    }
  });

  it('every action and gesture is in its union', () => {
    for (const name of [...constants('KataWebActions'), ...constants('KataCanvasGestures')]) {
      expect(typescript).toContain(`'${name}'`);
    }
  });
});
