import type { ReactNode } from 'react';
import { Icon } from './Icon';

interface StateViewProps {
  kind: 'loading' | 'empty' | 'error';
  title: string;
  message?: string;
  action?: ReactNode;
}

/** The three non-data states every module shows the same way. */
export function StateView({ kind, title, message, action }: StateViewProps) {
  return (
    <div role={kind === 'error' ? 'alert' : 'status'} className="flex flex-col items-center justify-center gap-2 px-4 py-8 text-center">
      {kind === 'loading' && <span className="h-6 w-6 animate-spin rounded-full border-2 border-line border-t-accent" aria-hidden="true" />}
      {kind === 'empty' && <Icon name="document" className="h-7 w-7 text-muted" />}
      {kind === 'error' && <Icon name="warning" className="h-7 w-7 text-danger" />}
      <div className="font-semibold">{title}</div>
      {message && <p className="max-w-80 text-[12px] text-muted">{message}</p>}
      {action}
    </div>
  );
}
