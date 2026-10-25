// Drift guard for the hand-written kataContracts.ts and kataScene.ts: reads the C# records, enums and constants of the
// Kata Export window as text and checks every field, enum member, command, event, action and gesture name has its
// camelCase twin here.
import { describe, expect, it } from 'vitest';
import contractsSource from '../../../HPRebar/KataExport/View/Web/KataWebContracts.cs?raw';
import previewColumnSource from '../../../HPRebar/KataExport/ViewModel/KataPreviewColumn.cs?raw';
import sceneModelsSource from '../../../HPRebar.Core/KataExport/Web/KataWebSceneModels.cs?raw';
import model3dSource from '../../../HPRebar.Core/KataExport/Web/KataWeb3dModels.cs?raw';
import kataEnumsSource from '../../../HPRebar.Core/KataRebar/Models/Enums.cs?raw';
import elevationDrawingSource from '../../../HPRebar.Core/KataRebar/Models/KataElevationDrawing.cs?raw';
import sectionDrawingSource from '../../../HPRebar.Core/KataRebar/Models/KataSectionDrawing.cs?raw';
import elevationModelsSource from '../../../HPRebar.Core/KataExport/Models/KataElevationModels.cs?raw';
import runModelsSource from '../../../HPRebar.Core/KataExport/Models/KataRunModels.cs?raw';
import barTagBuilderSource from '../../../HPRebar.Core/KataRebar/Calculators/KataBarTagBuilder.cs?raw';
import tagStateSource from '../../../HPRebar.Core/KataRebar/Models/KataTagState.cs?raw';
import contractsTypescript from './kataContracts.ts?raw';
import sceneTypescript from './kataScene.ts?raw';

const csharp = [contractsSource, previewColumnSource, sceneModelsSource, model3dSource].join('\n');
const csharpEnums = [model3dSource, kataEnumsSource, elevationDrawingSource, sectionDrawingSource, elevationModelsSource, runModelsSource, barTagBuilderSource, tagStateSource].join('\n');
const typescript = contractsTypescript + sceneTypescript;

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

/** Members of `public enum Name { ... }`, camelCased as the wire writes them. */
function enumMembers(name: string): string[] {
  const match = new RegExp(`public enum ${name}\\s*\\{([^}]*)\\}`, 's').exec(csharpEnums);
  if (!match) throw new Error(`enum ${name} not found in the C# models`);
  const body = match[1]!.replace(/\/\/.*$/gm, '');
  return [...body.matchAll(/^\s*(\w+)\s*(?:=[^,]*)?,?\s*$/gm)].map((m) => camel(m[1]!));
}

/** Members of `export type Name = 'a' | 'b';`. */
function unionMembers(name: string): string[] {
  const match = new RegExp(`export type ${name} =([^;]*);`, 's').exec(typescript);
  if (!match) throw new Error(`type ${name} not found in the TypeScript contracts`);
  return [...match[1]!.matchAll(/'([^']+)'/g)].map((m) => m[1]!);
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
    ['KataPreviewColumn', 'KataPreviewColumn'],
    ['KataSectionRequest', 'KataSectionRequest'],
    ['KataRemoveBarsRequest', 'KataRemoveBarsRequest'],
    ['KataSceneFrame', 'KataSceneFrame'],
    ['KataFocusEvent', 'KataFocusEvent'],
  ])('%s has the same fields', (record, tsInterface) => {
    expect(interfaceKeys(tsInterface).sort()).toEqual(recordFields(record).sort());
  });

  it.each(
    [...(sceneModelsSource + model3dSource).matchAll(/public sealed record (KataWeb\w+)\(/g)].map((m) => m[1]!),
  )('scene record %s has the same fields', (record) => {
    expect(interfaceKeys(record).sort()).toEqual(recordFields(record).sort());
  });

  it.each(['KataDrawingPen', 'KataDimStyle', 'KataColumnKind', 'KataSupportKind', 'KataTagKind', 'KataLeaderArrow', 'KataTagLayout', 'KataBarRole', 'KataWeb3dBoxKind'])(
    'enum %s has the same members',
    (name) => {
      expect(unionMembers(name)).toEqual(enumMembers(name));
    },
  );

  it('every command and event has an entry in the maps', () => {
    for (const name of [...constants('KataWebCommands'), ...constants('KataWebEvents')]) {
      expect(typescript).toContain(`'${name}':`);
    }
  });

  it('every action is in its union', () => {
    for (const name of constants('KataWebActions')) {
      expect(typescript).toContain(`'${name}'`);
    }
  });
});
