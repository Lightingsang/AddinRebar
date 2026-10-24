import type { ReactNode } from 'react';

interface CardProps {
  title?: string;
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
}

export function Card({ title, actions, children, className = '' }: CardProps) {
  return (
    <section className={`rounded-lg border border-line bg-panel p-3 ${className}`} aria-label={title}>
      {(title || actions) && (
        <header className="mb-2 flex items-center justify-between gap-2">
          {title && <h2 className="text-[11px] font-semibold uppercase tracking-wide text-muted">{title}</h2>}
          {actions && <div className="flex items-center gap-1">{actions}</div>}
        </header>
      )}
      {children}
    </section>
  );
}

export function Badge({ children, tone = 'neutral' }: { children: ReactNode; tone?: 'neutral' | 'accent' | 'warning' }) {
  const tones = {
    neutral: 'bg-panel-muted text-muted border-line',
    accent: 'bg-accent-soft text-accent-strong border-transparent',
    warning: 'bg-steel/15 text-steel border-transparent',
  };
  return <span className={`inline-flex items-center rounded border px-1.5 py-0.5 text-[11px] font-medium ${tones[tone]}`}>{children}</span>;
}
