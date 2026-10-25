import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RevitBridgeClient } from '../bridge/revitBridgeClient';
import '../index.css';
import { ToastProvider } from '../ui/Toast';
import { KataApp } from './KataApp';
import type { KataBridge, KataCommandMap, KataEventMap } from './kataContracts';
import { KataMockBridge } from './kataMockBridge';
import type { KataWebScene, KataWebSection } from './kataScene';

// Inside HPRebar's Kata Export window (WebView2) the real bridge; in a normal browser (npm run dev) the mock.
// The mock draws the B03 fixture (loaded on demand, so the add-in's build never carries it).
const port = window.chrome?.webview;

async function createBridge(): Promise<KataBridge> {
  if (port) return new RevitBridgeClient<KataCommandMap, KataEventMap>(port);
  const [scene, section] = await Promise.all([import('./__fixtures__/kata-scene-b03.json'), import('./__fixtures__/kata-section-b03-1.json')]);
  return new KataMockBridge({ scene: scene.default as unknown as KataWebScene, sections: { 1: section.default as unknown as KataWebSection } });
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
