import { Card } from '../ui/Card';
import type { KataPreviewColumn } from './kataContracts';

/** The table's rows, top to bottom, as the WPF window lists them. */
const rows: { label: string; cell: (column: KataPreviewColumn) => string }[] = [
  { label: 'Loại', cell: (c) => c.kind },
  { label: 'Kích thước gối / nhịp', cell: (c) => c.row11 },
  { label: 'Giật mép trên dầm', cell: (c) => c.row19 },
  { label: 'Bề rộng dầm giao tại cột', cell: (c) => c.row20 },
  { label: 'Độ lệch dầm giao so với tâm cột', cell: (c) => c.row21 },
  { label: 'Tên các trục cột', cell: (c) => c.row22 },
  { label: 'Độ lệch của trục so với tâm cột', cell: (c) => c.row23 },
];

interface KataColumnsTableProps {
  columns: KataPreviewColumn[];
  selected: number;
  onSelect(index: number): void;
}

/** Every Kata column written to Excel (C, D, …), one table column each; a click selects it here and on the drawing. */
export function KataColumnsTable({ columns, selected, onSelect }: KataColumnsTableProps) {
  return (
    <Card title="Các ô ghi sang Kata Excel">
      {columns.length === 0 ? (
        <p className="text-[12px] text-muted">Chưa có bảng Kata cho dải dầm này.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="border-collapse text-[12px]" aria-label="Các ô ghi sang Kata Excel">
            <thead>
              <tr>
                <th scope="row" className="sticky left-0 z-10 bg-panel px-2 py-1 text-left font-bold text-accent">
                  Thông tin
                </th>
                {columns.map((column, index) => (
                  <th
                    key={column.letter}
                    scope="col"
                    aria-selected={index === selected}
                    className={`min-w-14 cursor-pointer border border-line px-2 py-1 text-center ${index === selected ? 'bg-accent text-white' : 'bg-panel-muted hover:bg-accent-soft'}`}
                    onClick={() => onSelect(index === selected ? -1 : index)}
                  >
                    {column.letter}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.label}>
                  <th scope="row" className="sticky left-0 z-10 whitespace-nowrap bg-panel px-2 py-1 text-left font-normal text-muted">
                    {row.label}
                  </th>
                  {columns.map((column, index) => (
                    <td
                      key={column.letter}
                      className={`cursor-pointer border border-line px-2 py-1 text-center ${index === selected ? 'bg-accent-soft font-semibold' : ''}`}
                      onClick={() => onSelect(index)}
                    >
                      {row.cell(column)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}
