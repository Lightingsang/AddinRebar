import { Plus, Trash2 } from 'lucide-react';
import { useContext } from 'react';
import { Button } from '@/components/ui/button';
import { FormContext, NumberInput, SearchableBlock } from './fields';
import { getIn } from './formPath';
import { useT } from './i18n';

export interface TableColumn {
  key: string;
  label: string;
}

/**
 * One of the two tables of "Thông số đặc thù", drawn like a sheet: a row per bar diameter, added and removed here; a
 * row left empty is ignored by the host. Each cell is a number box of the row's path, so the host's errors land on it.
 */
export function ShopTable({ path, columns, label }: { path: string; columns: TableColumn[]; label: string }) {
  const t = useT();
  const form = useContext(FormContext)!;
  const rows = (getIn(form.form, path) as object[] | undefined) ?? [];
  const emptyRow = Object.fromEntries(columns.map((c) => [c.key, null]));
  // A row added or removed renumbers the rows below it: their boxes start again from the form's values.
  const layout = `${form.revision}-${rows.length}`;
  const rowErrors = (i: number) => columns.map((c) => form.errors[`${path}[${i}].${c.key}`]).filter(Boolean);

  return (
    <SearchableBlock label={label}>
      <div className="mx-2.5 mt-1 mb-2 overflow-hidden rounded-lg border">
        <table className="w-full border-collapse text-[12px]" aria-label={label}>
          <thead>
            <tr className="bg-secondary text-[11px] text-muted-foreground">
              <th className="w-9 px-2 py-1.5 text-left font-medium">#</th>
              {columns.map((c) => (
                <th key={c.key} className="px-2 py-1.5 text-right font-medium">
                  {c.label}
                </th>
              ))}
              <th className="w-9" />
            </tr>
          </thead>
          <tbody>
            {rows.map((_, i) => (
              <tr key={`${layout}-${i}`} className="group/tr border-t transition-colors hover:bg-secondary/50">
                <td className="px-2 py-0.5 align-middle font-mono text-[11px] text-muted-foreground">{i + 1}</td>
                {columns.map((c) => (
                  <td key={c.key} className="px-0.5 py-0.5 align-middle">
                    <NumberInput path={`${path}[${i}].${c.key}`} label={`${c.label} — ${i + 1}`} cell />
                  </td>
                ))}
                <td className="px-1 py-0.5 text-right align-middle">
                  <Button
                    variant="ghost"
                    size="icon-xs"
                    className="text-muted-foreground opacity-0 group-hover/tr:opacity-100 hover:text-destructive focus-visible:opacity-100"
                    aria-label={t.removeRow(i + 1)}
                    title={t.removeRow(i + 1)}
                    onClick={() => form.update(path, rows.filter((__, j) => j !== i))}
                  >
                    <Trash2 />
                  </Button>
                </td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr className="border-t">
                <td colSpan={columns.length + 2} className="px-3 py-3 text-center text-[12px] text-muted-foreground">
                  {t.noRows}
                </td>
              </tr>
            )}
          </tbody>
        </table>
        <button
          type="button"
          className="flex w-full items-center justify-center gap-1.5 border-t border-dashed py-1.5 text-[12px] font-medium text-accent-strong transition-colors hover:bg-primary/5"
          onClick={() => form.update(path, [...rows, emptyRow])}
        >
          <Plus className="size-3.5" aria-hidden="true" />
          {t.addRow}
        </button>
      </div>
      {rows.map((_, i) =>
        rowErrors(i).length > 0 ? (
          <p key={i} role="alert" className="mx-3 mb-1.5 text-[11.5px] font-medium text-destructive">
            {i + 1}: {rowErrors(i).join(' ')}
          </p>
        ) : null,
      )}
    </SearchableBlock>
  );
}
