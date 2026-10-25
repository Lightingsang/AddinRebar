// The 3D tab: asks the host for the plan in 3D once per plan (a removal brings a new plan, hence a new model) and
// loads three.js only when the tab is first opened (its own chunk).
import { Suspense, lazy, useEffect, useState } from 'react';
import type { KataWeb3dModel, KataWebRebar } from '../kataScene';
import type { Rebar3DViewProps } from './Rebar3DView';

const Rebar3DView = lazy(() => import('./Rebar3DView'));

type Loaded = { rebar: KataWebRebar; status: 'ready'; model: KataWeb3dModel } | { rebar: KataWebRebar; status: 'failed'; message: string };

/**
 * The model of the plan `rebar` stands for. While a new plan's model is on its way (bars struck off, undone) the last
 * one stays, so the view — its WebGL context, camera and keyboard focus — is never torn down; null before the first.
 */
export function useModel3d(rebar: KataWebRebar, load: () => Promise<KataWeb3dModel>): Loaded | null {
  const [loaded, setLoaded] = useState<Loaded | null>(null);
  const current = loaded?.rebar === rebar ? loaded : null;
  const shown = current ?? (loaded?.status === 'ready' ? loaded : null);

  useEffect(() => {
    if (current) return;
    let alive = true;
    load().then(
      (model) => alive && setLoaded({ rebar, status: 'ready', model }),
      (error: unknown) => alive && setLoaded({ rebar, status: 'failed', message: error instanceof Error ? error.message : String(error) }),
    );
    return () => {
      alive = false;
    };
  }, [rebar, current, load]);

  return shown;
}

type Model3dPanelProps = Omit<Rebar3DViewProps, 'model'> & { rebar: KataWebRebar; loadModel3d(): Promise<KataWeb3dModel> };

export function Model3dPanel({ rebar, loadModel3d, ...view }: Model3dPanelProps) {
  const loaded = useModel3d(rebar, loadModel3d);
  const message = (text: string) => (
    <div className="flex h-full items-center justify-center text-[12px] text-muted" data-testid="kata-3d-state">
      {text}
    </div>
  );
  if (!loaded) return message('Đang dựng hình 3D…');
  if (loaded.status === 'failed') return message(`Không dựng được hình 3D: ${loaded.message}`);
  return (
    <Suspense fallback={message('Đang tải bộ vẽ 3D…')}>
      <Rebar3DView model={loaded.model} {...view} />
    </Suspense>
  );
}
