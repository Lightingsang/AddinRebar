// Drift guard for the hand-written settingsContracts.ts: reads the C# records and constants of the Kata Settings
// window and its form as text and checks every field, command and event name has its camelCase twin here.
import { describe, expect, it } from 'vitest';
import contractsSource from '../../../HPRebar/KataExport/View/Web/Settings/KataSettingsWebContracts.cs?raw';
import formSource from '../../../HPRebar.Core/KataExport/Settings/KataSettingsForm.cs?raw';
import choicesSource from '../../../HPRebar.Core/KataExport/Settings/KataSettingsFormChoices.cs?raw';
import typescript from './settingsContracts.ts?raw';

const csharp = [contractsSource, formSource, choicesSource].join('\n');

const camel = (name: string) => name[0]!.toLowerCase() + name.slice(1);

/** Fields of `record Name(...)` (positional) or `record Name { ... { get; init; } }`, camelCased as the wire writes them. */
function recordFields(name: string): string[] {
  const positional = new RegExp(`record ${name}\\(([^;{]*?)\\)\\s*[;{]`, 's').exec(csharp);
  if (positional) return positional[1]!.split(',').map((parameter) => camel(parameter.trim().split(/\s+/)[1]!));
  const body = new RegExp(`record ${name}\\s*\\{(.*?)\\n\\}`, 's').exec(csharp);
  if (!body) throw new Error(`record ${name} not found in the C# contracts`);
  return [...body[1]!.matchAll(/public [\w<>?,\s]+? (\w+) \{ get; init; \}/g)].map((m) => camel(m[1]!));
}

function interfaceKeys(name: string): string[] {
  const match = new RegExp(`export interface ${name} \\{([^}]*)\\}`, 's').exec(typescript);
  if (!match) throw new Error(`interface ${name} not found in settingsContracts.ts`);
  return [...match[1]!.matchAll(/^\s*(\w+)\??:/gm)].map((m) => m[1]!);
}

function constants(name: string): string[] {
  const match = new RegExp(`class ${name}\\s*\\{(.*?)\\n\\}`, 's').exec(csharp);
  if (!match) throw new Error(`class ${name} not found in the C# contracts`);
  return [...match[1]!.matchAll(/const string \w+ = "([^"]+)"/g)].map((m) => m[1]!);
}

const quotedKeys = (name: string) => {
  const match = new RegExp(`export interface ${name} \\{(.*?)\\n\\}`, 's').exec(typescript);
  if (!match) throw new Error(`interface ${name} not found in settingsContracts.ts`);
  return [...match[1]!.matchAll(/^\s*'([^']+)':/gm)].map((m) => m[1]!);
};

const records = [
  'KataSettingsForm',
  'KataDetailForm',
  'KataSpecialForm',
  'KataMassRowForm',
  'KataLapRowForm',
  'KataJointsForm',
  'KataJointForm',
  'KataSettingsError',
  'KataSettingsFormChoices',
  'KataSettingsSnapshot',
  'KataSettingsPageState',
  'KataSettingsValidation',
  'KataSettingsSaveResult',
  'KataSettingsThemeEvent',
];

describe('settings contracts', () => {
  it.each(records)('%s has the fields of the C# record, in order', (name) => {
    expect(interfaceKeys(name)).toEqual(recordFields(name));
  });

  it('knows every command and event the window registers', () => {
    expect(quotedKeys('SettingsCommandMap').sort()).toEqual(constants('KataSettingsWebCommands').sort());
    expect(quotedKeys('SettingsEventMap').sort()).toEqual(constants('KataSettingsWebEvents').sort());
  });
});
