import { Card } from '../ui/Card';
import type { KataUpdateRequest, KataViewState } from './kataContracts';

const NONE = '';

function ParameterRow({ label, value, parameter, parameters, onChange }: {
  label: string;
  value: string;
  parameter: string;
  parameters: string[];
  onChange(parameter: string): void;
}) {
  return (
    <div className="grid grid-cols-[7.5rem_1fr] items-center gap-2">
      <span className="text-muted">{label}</span>
      <div className="flex min-w-0 items-center gap-2">
        <select
          aria-label={`Tham số ${label}`}
          className="min-w-0 flex-1 rounded border border-line bg-panel px-1.5 py-1 text-[12px]"
          value={parameter}
          onChange={(e) => onChange(e.target.value)}
        >
          <option value={NONE}>(không lấy tham số)</option>
          {parameters.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </select>
        <span className="w-16 shrink-0 truncate text-right font-semibold" title={value}>
          {value}
        </span>
      </div>
    </div>
  );
}

function ValueRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[7.5rem_1fr] items-center gap-2">
      <span className="text-muted">{label}</span>
      <span className="truncate text-right font-semibold" title={value}>
        {value}
      </span>
    </div>
  );
}

/** Cells B3:B10 as they will be written; B3 and B4 come from the beam parameters the user picks. */
export function KataParameters({ state, update }: { state: KataViewState; update(request: KataUpdateRequest): void }) {
  const h = state.header;
  return (
    <Card title="Thông số Kata" actions={<span className="text-[11px] text-muted">Ô B3:B10</span>}>
      <div className="flex flex-col gap-1.5 text-[12px]">
        <ParameterRow label="Tên dầm" value={h.beamName} parameter={state.nameParameter} parameters={state.parameters} onChange={(nameParameter) => update({ nameParameter })} />
        <ParameterRow label="Số cấu kiện" value={h.beamCount} parameter={state.countParameter} parameters={state.parameters} onChange={(countParameter) => update({ countParameter })} />
        <ValueRow label="h dầm (mm)" value={h.height} />
        <ValueRow label="b dầm (mm)" value={h.width} />
        <ValueRow label="h sàn (mm)" value={h.slabThickness} />
        <ValueRow label="Tên trục dầm" value={h.axisGrid} />
        <ValueRow label="Độ lệch của trục" value={h.axisOffset} />
        <ValueRow label="Cao trình dầm" value={h.elevation} />
      </div>
    </Card>
  );
}
