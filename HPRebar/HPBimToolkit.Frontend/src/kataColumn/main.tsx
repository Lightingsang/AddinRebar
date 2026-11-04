import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RevitBridgeClient } from '../bridge/revitBridgeClient';
import '../kata/kata.css';
import { ToastProvider } from '../ui/Toast';
import { ColumnApp } from './ColumnApp';
import type { ColumnBridge, ColumnCommandMap, ColumnEventMap, KataColumnPreview, KataColumnRebarSummary } from './columnContracts';
import { ColumnMockBridge } from './columnMockBridge';

// Inside HPRebar's Kata Column Rebar window (WebView2) the real bridge; in a normal browser (npm run dev) the mock,
// opened on a fixture HPRebar.Core.Tests writes (loaded on demand, so the add-in's build never carries it).
const port = window.chrome?.webview;

async function createBridge(): Promise<ColumnBridge> {
  if (port) return new RevitBridgeClient<ColumnCommandMap, ColumnEventMap>(port);
  // ?theme=dark&stack=stepped opens the sample in that state (screenshots, design reviews).
  const query = new URLSearchParams(window.location.search);
  const theme = query.get('theme') === 'dark' ? 'dark' : 'light';
  // ?mode=rebar opens step 2, &plan=errors on a sheet with mistakes.
  const [sample, stepped, planned, refused] = await Promise.all([
    import('./__fixtures__/kata-column-sample.json'),
    import('./__fixtures__/kata-column-stepped.json'),
    import('./__fixtures__/kata-column-rebar-sample.json'),
    import('./__fixtures__/kata-column-rebar-errors.json'),
  ]);
  const preview = (query.get('stack') === 'stepped' ? stepped : sample).default as KataColumnPreview;
  const rebar = (query.get('plan') === 'errors' ? refused : planned).default as KataColumnRebarSummary;
  return new ColumnMockBridge({ preview, rebar, mode: query.get('mode') === 'rebar' ? 'rebar' : 'export', theme });
}

void createBridge().then((bridge) =>
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <ToastProvider bottom="bottom-16">
        <ColumnApp bridge={bridge} />
      </ToastProvider>
    </StrictMode>,
  ),
);
