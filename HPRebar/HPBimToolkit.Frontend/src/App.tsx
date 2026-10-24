import { useEffect, useState } from 'react';
import type { BridgeClient } from './bridge/bridgeClient';
import { errorText } from './bridge/errorText';
import type { HelloResult, Theme } from './bridge/protocol';
import { DashboardPage } from './modules/dashboard/DashboardPage';
import { moduleFromHash, modules } from './modules/registry';
import { Icon } from './ui/Icon';
import { useToast } from './ui/Toast';

const applyTheme = (theme: Theme) => document.documentElement.classList.toggle('dark', theme === 'dark');

export function App({ bridge }: { bridge: BridgeClient }) {
  const notify = useToast();
  const [hello, setHello] = useState<HelloResult | null>(null);
  const [activeModule, setActiveModule] = useState(() => moduleFromHash(window.location.hash));

  useEffect(() => {
    let hasHello = false;
    const greet = () =>
      bridge
        .request('app.hello')
        .then((result) => {
          hasHello = true;
          setHello(result);
          applyTheme(result.theme);
        })
        .catch((error: unknown) => notify(`Không kết nối được Revit: ${errorText(error)}`, 'error'));

    const unsubscribers = [
      bridge.on('theme.changed', ({ theme }) => applyTheme(theme)),
      // A first handshake can fail while a Revit dialog is open; any later sign of life retries it.
      bridge.on('document.changed', () => {
        if (!hasHello) {
          void greet();
        }
      }),
    ];
    void greet();
    return () => unsubscribers.forEach((unsubscribe) => unsubscribe());
  }, [bridge, notify]);

  useEffect(() => {
    const onHashChange = () => setActiveModule(moduleFromHash(window.location.hash));
    window.addEventListener('hashchange', onHashChange);
    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  const toolIds = (hello?.capabilities ?? []).filter((c) => c.startsWith('tool:')).map((c) => c.slice('tool:'.length));

  return (
    <div className="@container flex h-full min-w-0 flex-col">
      <nav className="flex shrink-0 items-center gap-0.5 overflow-x-auto border-b border-line bg-panel px-1.5" aria-label="Module">
        {modules.map((module) => (
          <a
            key={module.id}
            href={module.isAvailable ? `#/${module.id}` : undefined}
            aria-current={activeModule === module.id ? 'page' : undefined}
            aria-disabled={!module.isAvailable || undefined}
            title={module.isAvailable ? module.label : `${module.label} — sắp có`}
            className={`flex shrink-0 items-center gap-1 border-b-2 px-2 py-2 text-[12px] ${
              activeModule === module.id ? 'border-accent text-ink font-semibold' : 'border-transparent text-muted'
            } ${module.isAvailable ? 'hover:text-ink' : 'cursor-default opacity-60'}`}
          >
            <Icon name={module.icon} className="h-3.5 w-3.5" />
            <span className="hidden @sm:inline">{module.label}</span>
            {!module.isAvailable && <span className="hidden text-[10px] @md:inline">(sắp có)</span>}
          </a>
        ))}
      </nav>

      <main className="@container min-h-0 flex-1 overflow-y-auto p-2">
        {activeModule === 'dashboard' && <DashboardPage bridge={bridge} toolIds={toolIds} />}
        <footer className="mt-2 text-center text-[10px] text-muted">
          HP BIM Toolkit {hello?.version ?? ''} · Revit {hello?.revitVersion ?? '…'}
          {bridge.isMock && ' · dữ liệu mẫu'}
        </footer>
      </main>
    </div>
  );
}
