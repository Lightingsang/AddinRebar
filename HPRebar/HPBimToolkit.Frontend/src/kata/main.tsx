import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RevitBridgeClient } from '../bridge/revitBridgeClient';
import './kata.css';
import { ToastProvider } from '../ui/Toast';
import { KataApp } from './KataApp';
import type { KataBridge, KataCommandMap, KataEventMap } from './kataContracts';
import { KataMockBridge } from './kataMockBridge';
import type { KataWeb3dModel, KataWebScene, KataWebSection } from './kataScene';

// Inside HPRebar's Kata Export window (WebView2) the real bridge; in a normal browser (npm run dev) the mock.
// The mock draws the B03 fixture (loaded on demand, so the add-in's build never carries it).
const port = window.chrome?.webview;

async function createBridge(): Promise<KataBridge> {
  if (port) return new RevitBridgeClient<KataCommandMap, KataEventMap>(port);
  const [scene, section, model3d] = await Promise.all([
    import('./__fixtures__/kata-scene-b03.json'),
    import('./__fixtures__/kata-section-b03-1.json'),
    import('./__fixtures__/kata-model3d-b03.json'),
  ]);
  // ?theme=dark&column=3&rebar=1&warn=1 opens the sample in that state (screenshots, design reviews).
  const query = new URLSearchParams(window.location.search);
  return new KataMockBridge({
    state: {
      theme: query.get('theme') === 'dark' ? 'dark' : 'light',
      selectedColumn: Number(query.get('column') ?? -1),
      ...(query.get('rebar') ? { showRebar: true, showBarTags: true } : {}),
      ...(query.get('warn')
        ? { warnings: ['[Ví dụ] [Chặn] B5: h trong sheet 1200 mm, Revit 1100 mm (lệch 100 mm); thép theo kích thước Revit.', '[Ví dụ] [Cảnh báo] K11 = 0: đầu console — thép console vẽ theo quy tắc tạm của HPRebar.'] }
        : {}),
    },
    scene: scene.default as unknown as KataWebScene,
    sections: { 1: section.default as unknown as KataWebSection },
    model3d: model3d.default as unknown as KataWeb3dModel,
  });
}

void createBridge().then((bridge) =>
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <ToastProvider>
        <KataApp bridge={bridge} />
      </ToastProvider>
    </StrictMode>,
  ),
);
