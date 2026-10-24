import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { formatNumber } from '../../ui/format';

/** Labels for the tool ids the add-in advertises in app.hello (capability "tool:<id>"). Unknown ids show their id. */
export const toolLabels: Record<string, string> = {
  'revit.default3DView': '3D mặc định',
  'revit.reviewWarnings': 'Cảnh báo',
  'revit.projectInformation': 'Thông tin dự án',
  'revit.projectUnits': 'Đơn vị',
  'revit.visibilityGraphics': 'Visibility/Graphics',
  'revit.manageLinks': 'Quản lý link',
  'revit.worksets': 'Worksets',
  'revit.purgeUnused': 'Purge',
};

interface QuickActionsProps {
  selectionCount: number | null;
  toolIds: string[];
  onZoomToSelection: () => void;
  onOpenTool: (toolId: string, label: string) => void;
}

export function QuickActions({ selectionCount, toolIds, onZoomToSelection, onOpenTool }: QuickActionsProps) {
  return (
    <Card title="Lựa chọn & công cụ">
      <div className="flex items-center justify-between gap-2">
        <div>
          <div className="text-[11px] text-muted">Đang chọn trong Revit</div>
          <div className="text-xl font-semibold tabular-nums" aria-live="polite" data-testid="selection-count">
            {selectionCount === null ? '—' : formatNumber(selectionCount)}
          </div>
        </div>
        <Button icon="zoom" onClick={onZoomToSelection} disabled={!selectionCount}>
          Phóng tới
        </Button>
      </div>
      {toolIds.length > 0 && (
        <div className="mt-2 flex flex-wrap gap-1 border-t border-line pt-2">
          {toolIds.map((toolId) => {
            const label = toolLabels[toolId] ?? toolId;
            return (
              <Button key={toolId} variant="ghost" icon="tool" onClick={() => onOpenTool(toolId, label)}>
                {label}
              </Button>
            );
          })}
        </div>
      )}
    </Card>
  );
}
