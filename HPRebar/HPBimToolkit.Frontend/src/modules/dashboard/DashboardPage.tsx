import type { BridgeClient } from '../../bridge/bridgeClient';
import type { StatisticsScope } from '../../bridge/protocol';
import { Button } from '../../ui/Button';
import { formatNumber } from '../../ui/format';
import { StateView } from '../../ui/StateView';
import { useToast } from '../../ui/Toast';
import { CategoryList } from './CategoryList';
import { DocumentHeader } from './DocumentHeader';
import { MetricsGrid } from './MetricsGrid';
import { QuickActions } from './QuickActions';
import { useDashboard } from './useDashboard';

const scopes: { value: StatisticsScope; label: string }[] = [
  { value: 'model', label: 'Toàn mô hình' },
  { value: 'activeView', label: 'View hiện hành' },
];

export function DashboardPage({ bridge, toolIds }: { bridge: BridgeClient; toolIds: string[] }) {
  const notify = useToast();
  const { state, refresh, setScope, showCategory, zoomToSelection, openTool } = useDashboard(bridge, notify);
  const { document, statistics, scope, stale } = state;
  const isCounting = statistics.status === 'loading';

  if (document.status === 'error' && !document.data) {
    return <StateView kind="error" title="Không đọc được tài liệu" message={document.error ?? undefined} action={<Button icon="refresh" onClick={() => void refresh()}>Thử lại</Button>} />;
  }

  if (!document.data) {
    return <StateView kind="loading" title="Đang kết nối Revit…" />;
  }

  if (!document.data.hasDocument) {
    return <StateView kind="empty" title="Chưa mở tài liệu" message="Mở một dự án hoặc family trong Revit, Dashboard sẽ tự cập nhật." />;
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-1.5">
        <div role="radiogroup" aria-label="Phạm vi thống kê" className="flex rounded-md border border-line bg-panel p-0.5">
          {scopes.map((option) => (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={scope === option.value}
              disabled={isCounting}
              onClick={() => setScope(option.value)}
              className={`rounded px-2 py-1 text-[12px] ${scope === option.value ? 'bg-accent text-white' : 'text-muted hover:text-ink'}`}
            >
              {option.label}
            </button>
          ))}
        </div>
        <Button icon="refresh" busy={isCounting} onClick={() => void refresh()}>
          Làm mới
        </Button>
        {stale && (
          <span role="status" className="rounded bg-steel/15 px-1.5 py-0.5 text-[11px] font-medium text-steel" title="Mô hình đã thay đổi từ lần thống kê trước">
            Dữ liệu cũ · +{formatNumber(stale.added)} / −{formatNumber(stale.deleted)} / ~{formatNumber(stale.modified)}
          </span>
        )}
      </div>

      <DocumentHeader document={document.data} />
      <QuickActions selectionCount={state.selectionCount} toolIds={toolIds} onZoomToSelection={() => void zoomToSelection()} onOpenTool={(id, label) => void openTool(id, label)} />

      {statistics.status === 'error' && !statistics.data ? (
        <StateView kind="error" title="Không thống kê được" message={statistics.error ?? undefined} action={<Button icon="refresh" onClick={() => void refresh()}>Thử lại</Button>} />
      ) : statistics.data ? (
        <>
          <MetricsGrid statistics={statistics.data} />
          {statistics.data.categories.length === 0 ? (
            <StateView kind="empty" title="Không có phần tử" message="Phạm vi này không có phần tử nào để thống kê." />
          ) : (
            <CategoryList categories={statistics.data.categories} onShow={(category, mode) => void showCategory(category, mode)} />
          )}
          <p className="text-right text-[11px] text-muted">Thống kê trong {formatNumber(statistics.data.elapsedMs)} ms</p>
        </>
      ) : (
        <StateView kind="loading" title="Đang thống kê mô hình…" />
      )}
    </div>
  );
}
