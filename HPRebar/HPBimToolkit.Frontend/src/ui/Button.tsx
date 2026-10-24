import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Icon, type IconName } from './Icon';

type Variant = 'primary' | 'secondary' | 'ghost';

const variants: Record<Variant, string> = {
  primary: 'bg-accent text-white hover:bg-accent-strong border-transparent',
  secondary: 'bg-panel text-ink border-line hover:bg-panel-muted',
  ghost: 'bg-transparent text-muted border-transparent hover:text-ink hover:bg-panel-muted',
};

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  icon?: IconName;
  busy?: boolean;
  children?: ReactNode;
}

export function Button({ variant = 'secondary', icon, busy = false, children, className = '', disabled, ...rest }: ButtonProps) {
  return (
    <button
      type="button"
      className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-[12px] font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${variants[variant]} ${className}`}
      disabled={disabled || busy}
      aria-busy={busy || undefined}
      {...rest}
    >
      {icon && <Icon name={icon} className={`h-3.5 w-3.5 ${busy ? 'animate-spin' : ''}`} />}
      {children}
    </button>
  );
}
