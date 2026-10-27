import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RevitBridgeClient } from '../bridge/revitBridgeClient';
import '../kata/kata.css';
import { ToastProvider } from '../ui/Toast';
import { SettingsApp } from './SettingsApp';
import type { KataSettingsSnapshot, SettingsBridge, SettingsCommandMap, SettingsEventMap } from './settingsContracts';
import { SettingsMockBridge } from './settingsMockBridge';

// Inside HPRebar's Kata Settings window (WebView2) the real bridge; in a normal browser (npm run dev) the mock, opened
// on a fixture HPRebar.Core.Tests writes (loaded on demand, so the add-in's build never carries it).
const port = window.chrome?.webview;

async function createBridge(): Promise<SettingsBridge> {
  if (port) return new RevitBridgeClient<SettingsCommandMap, SettingsEventMap>(port);
  // ?theme=dark&file=edited|notice opens the sample in that state (screenshots, design reviews).
  const query = new URLSearchParams(window.location.search);
  const [defaults, edited, notice] = await Promise.all([
    import('./__fixtures__/kata-settings-default.json'),
    import('./__fixtures__/kata-settings-edited.json'),
    import('./__fixtures__/kata-settings-notice.json'),
  ]);
  const file = query.get('file');
  const opened = file === 'edited' ? edited : file === 'notice' ? notice : defaults;
  return new SettingsMockBridge({
    snapshot: opened.default as KataSettingsSnapshot,
    defaults: defaults.default as KataSettingsSnapshot,
    theme: query.get('theme') === 'dark' ? 'dark' : 'light',
  });
}

void createBridge().then((bridge) =>
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      {/* The toasts sit above the footer's buttons, never over them. */}
      <ToastProvider bottom="bottom-16">
        <SettingsApp bridge={bridge} />
      </ToastProvider>
    </StrictMode>,
  ),
);
