import { useMemo, useState } from 'react';
import type { CategoryCount, ShowMode } from '../../bridge/protocol';
import { Card } from '../../ui/Card';
import { formatNumber } from '../../ui/format';
import { Icon } from '../../ui/Icon';

const COLLAPSED_ROWS = 10;

interface CategoryListProps {
  categories: CategoryCount[];
  onShow: (category: CategoryCount, mode: ShowMode) => void;
}

/** Elements per category as relative bars; a row selects its elements in Revit, the lens zooms to them. */
export function CategoryList({ categories, onShow }: CategoryListProps) {
  const [filter, setFilter] = useState('');
  const [expanded, setExpanded] = useState(false);

  const visible = useMemo(() => {
    const needle = filter.trim().toLocaleLowerCase('vi');
    return needle ? categories.filter((c) => c.name.toLocaleLowerCase('vi').includes(needle)) : categories;
  }, [categories, filter]);

  const rows = expanded || filter ? visible : visible.slice(0, COLLAPSED_ROWS);
  const max = Math.max(1, ...categories.map((c) => c.count));

  return (
    <Card title="Phần tử theo category">
      <label className="mb-2 flex items-center gap-1.5 rounded-md border border-line bg-panel-muted px-2 py-1 text-muted focus-within:border-accent">
        <Icon name="search" className="h-3.5 w-3.5" />
        <input
          type="search"
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          placeholder="Lọc category…"
          aria-label="Lọc category"
          className="min-w-0 flex-1 bg-transparent text-[12px] text-ink outline-none placeholder:text-muted"
        />
      </label>

      {rows.length === 0 ? (
        <p className="py-3 text-center text-[12px] text-muted">Không có category nào khớp.</p>
      ) : (
        <ul className="flex flex-col gap-0.5">
          {rows.map((category) => (
            <li key={category.categoryId} className="group flex items-center gap-1">
              <button
                type="button"
                onClick={() => onShow(category, 'select')}
                aria-label={`Chọn ${formatNumber(category.count)} phần tử ${category.name}`}
                title={`Chọn ${formatNumber(category.count)} phần tử ${category.name} trong Revit`}
                className="relative flex min-w-0 flex-1 items-center justify-between gap-2 overflow-hidden rounded px-2 py-1 text-left hover:bg-panel-muted"
              >
                <span className="absolute inset-y-0 left-0 bg-accent-soft" style={{ width: `${(category.count / max) * 100}%` }} aria-hidden="true" />
                <span className="relative truncate">{category.name}</span>
                <span className="relative shrink-0 font-medium tabular-nums">{formatNumber(category.count)}</span>
              </button>
              <button
                type="button"
                onClick={() => onShow(category, 'zoom')}
                className="rounded p-1 text-muted opacity-60 hover:bg-panel-muted hover:text-ink group-hover:opacity-100"
                aria-label={`Phóng tới ${category.name}`}
              >
                <Icon name="zoom" className="h-3.5 w-3.5" />
              </button>
            </li>
          ))}
        </ul>
      )}

      {!filter && visible.length > COLLAPSED_ROWS && (
        <button type="button" className="mt-1 text-[12px] text-accent hover:underline" onClick={() => setExpanded((value) => !value)}>
          {expanded ? 'Thu gọn' : `Xem tất cả ${visible.length} category`}
        </button>
      )}
    </Card>
  );
}
