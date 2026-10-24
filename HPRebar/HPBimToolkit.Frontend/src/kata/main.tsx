import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RevitBridgeClient } from '../bridge/revitBridgeClient';
import '../index.css';
import { ToastProvider } from '../ui/Toast';
import { KataApp } from './KataApp';
import type { KataBridge, KataCommandMap, KataEventMap } from './kataContracts';
import { KataMockBridge } from './kataMockBridge';

// Inside HPRebar's Kata Export window (WebView2) the real bridge; in a normal browser (npm run dev) the mock.
const port = window.chrome?.webview;
const bridge: KataBridge = port ? new RevitBridgeClient<KataCommandMap, KataEventMap>(port) : new KataMockBridge();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ToastProvider>
      <KataApp bridge={bridge} />
    </ToastProvider>
  </StrictMode>,
);
