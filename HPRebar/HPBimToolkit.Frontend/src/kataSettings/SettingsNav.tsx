import { cn } from '@/lib/utils';
import { useT } from './i18n';
import { NAV, type GroupId } from './navigation';

/**
 * The page's outline on the left: Kata's three tabs with their cards. The card being read is highlighted; a group with
 * wrong values shows how many.
 */
export function SettingsNav({ active, errors, onPick }: { active: string; errors: Record<GroupId, number>; onPick(id: string): void }) {
  const t = useT();
  return (
    <nav aria-label={t.outline} className="hidden w-52 shrink-0 overflow-y-auto border-r bg-card/40 px-2.5 py-3 md:block">
      {NAV.map((group) => (
        <div key={group.id} className="mb-3">
          <div className="flex items-center justify-between px-2 pb-1 text-[11px] font-semibold tracking-wide text-muted-foreground uppercase">
            {group.title(t)}
            {errors[group.id] > 0 && (
              <span className="grid h-4 min-w-4 place-items-center rounded-full bg-destructive px-1 text-[10px] font-bold text-white" aria-label={t.toFix(errors[group.id])}>
                {errors[group.id]}
              </span>
            )}
          </div>
          {group.sections.map((section) => {
            const Icon = section.icon;
            const on = active === section.id;
            return (
              <button
                key={section.id}
                type="button"
                aria-current={on ? 'location' : undefined}
                onClick={() => onPick(section.id)}
                className={cn(
                  'flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-[12.5px] transition-colors',
                  on ? 'bg-primary/10 font-semibold text-accent-strong' : 'text-foreground/80 hover:bg-secondary hover:text-foreground',
                )}
              >
                <Icon className={cn('size-3.5 shrink-0', on ? 'text-accent-strong' : 'text-muted-foreground')} aria-hidden="true" />
                <span className="truncate">{section.title(t)}</span>
              </button>
            );
          })}
        </div>
      ))}
    </nav>
  );
}
