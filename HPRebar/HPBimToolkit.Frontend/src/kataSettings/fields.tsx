import { createContext, useContext, useState, type ReactNode } from 'react';
import { CircleAlert, type LucideIcon } from 'lucide-react';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import { cn } from '@/lib/utils';
import { getIn } from './formPath';
import { useT } from './i18n';
import type { SettingsController } from './useSettingsForm';

// The inputs of the settings page. Each takes the form path of its value: it reads the value, the host's error and
// whether it differs from the saved file from the form context, and writes back through update(path, value).

export const FormContext = createContext<SettingsController | null>(null);

/** The search box's text, already folded by `fold`: rows whose label does not hold it are hidden. */
export const SearchContext = createContext('');

/** Lower case without Vietnamese marks, so "thep" finds "Thép". */
export const fold = (text: string) =>
  text
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase();

function useForm(): SettingsController {
  const form = useContext(FormContext);
  if (!form) throw new Error('Settings fields need a FormContext');
  return form;
}

function useField<T>(path: string) {
  const form = useForm();
  return { value: getIn(form.form, path) as T, error: form.errors[path], set: (value: unknown) => form.update(path, value), revision: form.revision };
}

/** Whether any of `paths` holds something else than the settings file. */
function useChanged(paths: string[]): boolean {
  const form = useForm();
  if (!form.savedForm) return false;
  return paths.some((path) => JSON.stringify(getIn(form.form, path)) !== JSON.stringify(getIn(form.savedForm, path)));
}

export const idOf = (path: string) => `field-${path.replace(/[^\w]/g, '-')}`;

export type Unit = keyof ReturnType<typeof useT>['units'];

const PENDING_BADGE = 'rounded-full bg-warning/12 px-2 py-0.5 text-[10.5px] font-semibold text-warning ring-1 ring-warning/25 ring-inset';

export function PendingBadge({ label }: { label: string }) {
  return <span className={cn(PENDING_BADGE, 'whitespace-nowrap')}>{label}</span>;
}

/** A card of the page: an icon, a title, an optional "not applied" badge and note, then its rows. */
export function Section({ id, icon: Icon, title, pending, note, children }: { id: string; icon: LucideIcon; title: string; pending?: boolean; note?: string; children: ReactNode }) {
  const t = useT();
  return (
    <section id={id} data-section className={cn('scroll-mt-3 rounded-xl border bg-card shadow-xs', pending && 'border-dashed')} aria-label={title}>
      <header className="flex items-start gap-3 px-4 pt-3.5 pb-1.5">
        <span className={cn('grid size-8 shrink-0 place-items-center rounded-lg', pending ? 'bg-warning/12 text-warning' : 'bg-primary/10 text-accent-strong')}>
          <Icon className="size-4" aria-hidden="true" />
        </span>
        <div className="min-w-0 flex-1 pt-1">
          <h3 className="text-[13.5px] leading-tight font-semibold">{title}</h3>
          {note && <p className="mt-1 text-[11.5px] leading-snug text-muted-foreground">{note}</p>}
        </div>
        {pending && <PendingBadge label={t.notApplied} />}
      </header>
      <div className="px-1.5 pb-1.5">{children}</div>
    </section>
  );
}

/** A note between rows ("the items below are not applied"). */
export function RowNote({ children }: { children: ReactNode }) {
  return <p className="mx-2.5 my-1.5 rounded-md bg-secondary px-3 py-1.5 text-[11.5px] text-muted-foreground">{children}</p>;
}

/** One line of a section: a dot when changed, the label, the control, and the host's error under the control. */
function Row({ label, paths, htmlFor, pending, error, errorId, children }: { label: string; paths: string[]; htmlFor?: string; pending?: boolean; error?: string; errorId?: string; children: ReactNode }) {
  const t = useT();
  const query = useContext(SearchContext);
  const changed = useChanged(paths);
  return (
    <div data-row hidden={query !== '' && !fold(label).includes(query)} className="rounded-lg px-2.5 py-1.5 transition-colors hover:bg-secondary/60">
      <div className="flex min-h-8 flex-wrap items-center gap-x-4 gap-y-1.5">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <span
            className={cn('size-1.5 shrink-0 rounded-full transition-colors', changed ? 'bg-primary' : 'bg-transparent')}
            title={changed ? t.changed : undefined}
            data-changed={changed || undefined}
          />
          <Label htmlFor={htmlFor} className="min-w-0 text-[12.5px] leading-snug font-normal">
            {label}
          </Label>
          {pending && <PendingBadge label={t.notApplied} />}
        </div>
        <div className="ml-auto flex shrink-0 items-center gap-2">{children}</div>
      </div>
      {error && (
        <p id={errorId} role="alert" className="mt-1 flex items-center justify-end gap-1 text-[11.5px] font-medium text-destructive">
          <CircleAlert className="size-3.5 shrink-0" aria-hidden="true" />
          {error}
        </p>
      )}
    </div>
  );
}

/** Six significant digits are enough to read and edit (Kata's 1/6 shows as 0.166667); the form keeps the exact value until the box is typed in. */
const formatNumber = (value: number | null) => (value === null ? '' : String(Number(value.toPrecision(6))));

/** Empty or not a number reads as null, which the host refuses by name; integers only when `integer`. */
export function parseNumber(text: string, integer = false): number | null {
  const trimmed = text.trim().replace(',', '.');
  if (trimmed === '') return null;
  const value = Number(trimmed);
  if (!Number.isFinite(value) || (integer && !Number.isInteger(value))) return null;
  return value;
}

const BOX =
  'flex h-8 items-center rounded-md border border-input bg-background/70 shadow-xs transition-[color,box-shadow,border-color] focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/35 data-[invalid]:border-destructive data-[invalid]:ring-destructive/20 data-[disabled]:opacity-50 dark:bg-input/30';

/**
 * A number box with its unit inside, keeping what the user typed ("0." stays "0.") while the form holds the number it
 * reads as. It starts again from the form's value when the whole form is replaced (`revision`) or its row moves.
 * `cell` draws it borderless for the tables.
 */
export function NumberInput({ path, unit, integer, disabled, label, cell }: { path: string; unit?: Unit; integer?: boolean; disabled?: boolean; label?: string; cell?: boolean }) {
  const { value, error, set, revision } = useField<number | null>(path);
  return <NumberBox key={revision} path={path} value={value} error={error} set={set} unit={unit} integer={integer} disabled={disabled} label={label} cell={cell} />;
}

function NumberBox({ path, value, error, set, unit, integer, disabled, label, cell }: { path: string; value: number | null; error?: string; set(v: unknown): void; unit?: Unit; integer?: boolean; disabled?: boolean; label?: string; cell?: boolean }) {
  const t = useT();
  const [text, setText] = useState(() => formatNumber(value));
  const input = (
    <input
      id={idOf(path)}
      inputMode={integer ? 'numeric' : 'decimal'}
      aria-label={label}
      aria-invalid={!!error}
      aria-describedby={error ? `${idOf(path)}-error` : undefined}
      disabled={disabled}
      className={cn('h-full w-full min-w-0 bg-transparent text-right font-mono text-[12.5px] tabular-nums outline-none focus-visible:outline-none disabled:cursor-not-allowed', cell ? 'px-2' : 'px-2.5')}
      value={text}
      onChange={(e) => {
        setText(e.target.value);
        set(parseNumber(e.target.value, integer));
      }}
    />
  );
  if (cell) {
    return (
      <div
        className={cn(
          'flex h-8 rounded-md border border-transparent transition-colors hover:border-input focus-within:border-ring focus-within:bg-background',
          error && 'border-destructive bg-destructive/5',
        )}
      >
        {input}
      </div>
    );
  }
  return (
    <div className={cn(BOX, 'w-32')} data-invalid={error ? '' : undefined} data-disabled={disabled ? '' : undefined}>
      {input}
      {unit && <span className="shrink-0 pr-2.5 text-[11px] whitespace-nowrap text-muted-foreground">{t.units[unit]}</span>}
    </div>
  );
}

export function NumberField({ path, label, unit, integer, pending, disabled }: { path: string; label: string; unit?: Unit; integer?: boolean; pending?: boolean; disabled?: boolean }) {
  const { error } = useField(path);
  return (
    <Row label={label} paths={[path]} htmlFor={idOf(path)} pending={pending} error={error} errorId={`${idOf(path)}-error`}>
      <NumberInput path={path} unit={unit} integer={integer} disabled={disabled} />
    </Row>
  );
}

export interface Option {
  value: string | number;
  label: string;
}

/** A short list as one row of buttons (2–4 choices); booleans travel as 'true' / 'false'. */
export function SegmentedInput({ path, options, label, disabled }: { path: string; options: Option[]; label: string; disabled?: boolean }) {
  const { value, error, set } = useField<string | number | boolean | null>(path);
  const numeric = typeof options[0]?.value === 'number';
  return (
    <ToggleGroup
      type="single"
      aria-label={label}
      disabled={disabled}
      value={value === null ? '' : String(value)}
      onValueChange={(next) => next && set(numeric ? Number(next) : next === 'true' ? true : next === 'false' ? false : next)}
      className={cn('rounded-lg bg-secondary p-0.5 ring-1 ring-border ring-inset', error && 'ring-destructive')}
    >
      {options.map((o) => (
        <ToggleGroupItem
          key={String(o.value)}
          value={String(o.value)}
          aria-label={`${label}: ${o.label}`}
          className="h-7 min-w-11 rounded-md px-2.5 font-mono text-[12px] font-medium text-muted-foreground hover:bg-transparent hover:text-foreground data-[state=on]:bg-card data-[state=on]:font-semibold data-[state=on]:text-accent-strong data-[state=on]:shadow-sm data-[state=on]:ring-1 data-[state=on]:ring-border"
        >
          {o.label}
        </ToggleGroupItem>
      ))}
    </ToggleGroup>
  );
}

export function SegmentedField({ path, label, options, pending }: { path: string; label: string; options: Option[]; pending?: boolean }) {
  const { error } = useField(path);
  return (
    <Row label={label} paths={[path]} pending={pending} error={error}>
      <SegmentedInput path={path} options={options} label={label} />
    </Row>
  );
}

export function SelectInput({ path, options, label, unit }: { path: string; options: Option[]; label?: string; unit?: Unit }) {
  const t = useT();
  const { value, error, set } = useField<string | number | null>(path);
  const numeric = typeof options[0]?.value === 'number';
  return (
    <Select value={value === null ? undefined : String(value)} onValueChange={(next) => set(numeric ? Number(next) : next)}>
      <SelectTrigger id={idOf(path)} size="sm" aria-label={label} aria-invalid={!!error} className="w-32 bg-background/70 font-mono text-[12.5px]">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {options.map((o) => (
          <SelectItem key={o.value} value={String(o.value)} className="font-mono text-[12.5px]">
            {o.label}
            {unit && <span className="ml-1 font-sans text-muted-foreground">{t.units[unit]}</span>}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

export function SelectField({ path, label, options, unit, pending }: { path: string; label: string; options: Option[]; unit?: Unit; pending?: boolean }) {
  const { error } = useField(path);
  return (
    <Row label={label} paths={[path]} htmlFor={idOf(path)} pending={pending} error={error}>
      <SelectInput path={path} options={options} unit={unit} />
    </Row>
  );
}

export function SwitchInput({ path, label, disabled }: { path: string; label?: string; disabled?: boolean }) {
  const { value, set } = useField<boolean>(path);
  // The kit's off track (--input) is the card's border colour: too faint on the light card to read as a switch.
  return (
    <Switch
      id={idOf(path)}
      aria-label={label}
      checked={value}
      disabled={disabled}
      className="data-[state=unchecked]:bg-muted-foreground/35 dark:data-[state=unchecked]:bg-input/80"
      onCheckedChange={(checked) => set(checked)}
    />
  );
}

/** A switch, with the value it turns on beside it (`children`) whose error `errorPath` names. */
export function SwitchField({ path, label, pending, children, errorPath }: { path: string; label: string; pending?: boolean; children?: ReactNode; errorPath?: string }) {
  const { error } = useField(errorPath ?? path);
  return (
    <Row
      label={label}
      paths={errorPath ? [path, errorPath] : [path]}
      htmlFor={idOf(path)}
      pending={pending}
      error={error}
      errorId={errorPath ? `${idOf(errorPath)}-error` : undefined}
    >
      {children}
      <SwitchInput path={path} />
    </Row>
  );
}

/** A hook: its angle from Kata's list and its length in d. */
export function HookField({ label, factorPath, anglePath, angles }: { label: string; factorPath: string; anglePath: string; angles: number[] }) {
  const t = useT();
  const factor = useField(factorPath);
  const angle = useField(anglePath);
  return (
    <Row label={label} paths={[factorPath, anglePath]} htmlFor={idOf(factorPath)} error={factor.error ?? angle.error} errorId={`${idOf(factorPath)}-error`}>
      <SegmentedInput path={anglePath} label={`${label} — ${t.angle}`} options={angles.map((a) => ({ value: a, label: `${a}°` }))} />
      <NumberInput path={factorPath} unit="d" label={`${label} (d)`} />
    </Row>
  );
}

/** A lap zone: measured from the support face or centre, a fraction of L. */
export function LapZoneField({ label, fractionPath, originPath }: { label: string; fractionPath: string; originPath: string }) {
  const t = useT();
  const fraction = useField(fractionPath);
  return (
    <Row label={label} paths={[fractionPath, originPath]} htmlFor={idOf(fractionPath)} error={fraction.error} errorId={`${idOf(fractionPath)}-error`}>
      <SegmentedInput
        path={originPath}
        label={`${label} — ${t.lapFrom}`}
        options={[
          { value: 'false', label: t.originFace },
          { value: 'true', label: t.originCentre },
        ]}
      />
      <NumberInput path={fractionPath} unit="l" label={label} />
    </Row>
  );
}

/** A text box for a joint notation ("5f8a50", "2f16"), with Kata's suggestions when it has a list. */
export function TextInput({ path, label, disabled, placeholder, suggestions }: { path: string; label?: string; disabled?: boolean; placeholder?: string; suggestions?: string[] }) {
  const { value, error, set } = useField<string>(path);
  const listId = suggestions ? `${idOf(path)}-list` : undefined;
  return (
    <div className={cn(BOX, 'w-32')} data-invalid={error ? '' : undefined} data-disabled={disabled ? '' : undefined}>
      <input
        id={idOf(path)}
        aria-label={label}
        aria-invalid={!!error}
        aria-describedby={error ? `${idOf(path)}-error` : undefined}
        disabled={disabled}
        placeholder={placeholder}
        list={listId}
        className="h-full w-full min-w-0 bg-transparent px-2.5 font-mono text-[12.5px] outline-none focus-visible:outline-none placeholder:text-muted-foreground/60 disabled:cursor-not-allowed"
        value={value}
        onChange={(e) => set(e.target.value)}
      />
      {suggestions && (
        <datalist id={listId}>
          {suggestions.map((s) => (
            <option key={s} value={s} />
          ))}
        </datalist>
      )}
    </div>
  );
}

export function TextField({ path, label, pending, placeholder, suggestions }: { path: string; label: string; pending?: boolean; placeholder?: string; suggestions?: string[] }) {
  const { error } = useField(path);
  return (
    <Row label={label} paths={[path]} htmlFor={idOf(path)} pending={pending} error={error} errorId={`${idOf(path)}-error`}>
      <TextInput path={path} placeholder={placeholder} suggestions={suggestions} />
    </Row>
  );
}

/** A block that is not a labelled row (a table) but hides with the search like one. */
export function SearchableBlock({ label, children }: { label: string; children: ReactNode }) {
  const query = useContext(SearchContext);
  return (
    <div data-row hidden={query !== '' && !fold(label).includes(query)}>
      {children}
    </div>
  );
}
