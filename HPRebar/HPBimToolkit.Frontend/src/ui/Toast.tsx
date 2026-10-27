import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';
import { Icon } from './Icon';

export type ToastTone = 'info' | 'success' | 'error';

interface ToastItem {
  id: number;
  tone: ToastTone;
  text: string;
}

type Notify = (text: string, tone?: ToastTone) => void;

const ToastContext = createContext<Notify>(() => {});

const DISMISS_AFTER_MS = 4000;

/** @param bottom Tailwind bottom offset of the stack: a page with a footer bar lifts it above the bar ('bottom-16'). */
export function ToastProvider({ children, bottom = 'bottom-2' }: { children: ReactNode; bottom?: string }) {
  const [items, setItems] = useState<ToastItem[]>([]);
  const nextId = useRef(1);

  const dismiss = useCallback((id: number) => setItems((current) => current.filter((item) => item.id !== id)), []);

  const notify = useCallback<Notify>(
    (text, tone = 'info') => {
      const id = nextId.current++;
      setItems((current) => [...current.slice(-2), { id, tone, text }]);
      setTimeout(() => dismiss(id), DISMISS_AFTER_MS);
    },
    [dismiss],
  );

  const tones: Record<ToastTone, string> = {
    info: 'border-line',
    success: 'border-success',
    error: 'border-danger',
  };

  return (
    <ToastContext.Provider value={useMemo(() => notify, [notify])}>
      {children}
      <div className={`pointer-events-none fixed inset-x-2 ${bottom} z-50 flex flex-col items-end gap-1.5`} aria-live="polite">
        {items.map((item) => (
          <div key={item.id} role="status" className={`pointer-events-auto flex max-w-full items-start gap-2 rounded-md border-l-4 bg-panel px-3 py-2 text-[12px] shadow-lg ${tones[item.tone]}`}>
            <span className="min-w-0 break-words">{item.text}</span>
            <button type="button" className="text-muted hover:text-ink" aria-label="Đóng thông báo" onClick={() => dismiss(item.id)}>
              <Icon name="close" className="h-3 w-3" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export const useToast = (): Notify => useContext(ToastContext);
