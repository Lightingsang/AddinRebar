import type { ModelStatistics, StatisticsScope } from '../../bridge/protocol';
import { Card } from '../../ui/Card';
import { StatTile } from '../../ui/StatTile';

export function MetricsGrid({ statistics }: { statistics: ModelStatistics | null }) {
  const metrics = statistics?.metrics;
  return (
    <Card title="Chỉ số mô hình">
      <div className="grid grid-cols-2 gap-1.5 @sm:grid-cols-3 @xl:grid-cols-5">
        <StatTile label={`Phần tử (${scopeLabel(statistics?.scope)})`} value={statistics?.total ?? null} hint={statistics ? `${statistics.categories.length} category` : undefined} />
        <StatTile label="Type" value={metrics?.elementTypes ?? null} />
        <StatTile label="Family" value={metrics?.families ?? null} />
        <StatTile label="View" value={metrics?.views ?? null} />
        <StatTile label="Sheet" value={metrics?.sheets ?? null} />
        <StatTile label="Level" value={metrics?.levels ?? null} />
        <StatTile label="Cảnh báo" value={metrics?.warnings ?? null} tone="warning" />
        <StatTile label="Link Revit" value={metrics?.links ?? null} />
        {metrics?.worksets !== null && metrics?.worksets !== undefined && <StatTile label="Workset" value={metrics.worksets} />}
      </div>
    </Card>
  );
}

export const scopeLabel = (scope: StatisticsScope | undefined): string => (scope === 'activeView' ? 'view hiện hành' : 'toàn mô hình');
