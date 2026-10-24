import type { DocumentSnapshot } from '../../bridge/protocol';
import { Badge, Card } from '../../ui/Card';
import { formatScale } from '../../ui/format';

/** Revit's view type names, shown in Vietnamese where a short name exists. */
const viewTypeNames: Record<string, string> = {
  FloorPlan: 'Mặt bằng',
  CeilingPlan: 'Mặt bằng trần',
  EngineeringPlan: 'Mặt bằng kết cấu',
  AreaPlan: 'Mặt bằng diện tích',
  Elevation: 'Mặt đứng',
  Section: 'Mặt cắt',
  Detail: 'Chi tiết',
  ThreeD: '3D',
  DrawingSheet: 'Bản vẽ',
  Schedule: 'Bảng thống kê',
  DraftingView: 'Bản vẽ phác',
  Legend: 'Chú giải',
};

export function DocumentHeader({ document }: { document: DocumentSnapshot }) {
  const view = document.activeView;
  return (
    <Card title="Tài liệu">
      <div className="min-w-0">
        <div className="truncate text-[15px] font-semibold" title={document.path ?? undefined}>
          {document.title}
        </div>
        <div className="truncate text-[11px] text-muted">{document.path ?? 'Chưa lưu'}</div>
      </div>
      <div className="mt-2 flex flex-wrap gap-1">
        {document.isFamily && <Badge tone="accent">Family</Badge>}
        {document.isWorkshared && <Badge tone="accent">Workshared</Badge>}
        {document.isModified && <Badge tone="warning">Chưa lưu thay đổi</Badge>}
        {document.isReadOnly && <Badge tone="warning">Chỉ đọc</Badge>}
        {!document.isModified && !document.isReadOnly && <Badge>Đã lưu</Badge>}
      </div>
      {view && (
        <div className="mt-2 flex min-w-0 items-baseline gap-2 border-t border-line pt-2 text-[12px]">
          <span className="text-muted">View:</span>
          <span className="truncate font-medium">{view.name}</span>
          <span className="shrink-0 text-muted">
            {viewTypeNames[view.viewType] ?? view.viewType} · {formatScale(view.scale)}
          </span>
        </div>
      )}
    </Card>
  );
}
