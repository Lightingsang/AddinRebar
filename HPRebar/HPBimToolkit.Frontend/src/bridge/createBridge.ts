import type { BridgeClient } from './bridgeClient';
import { MockBridgeClient } from './mockBridgeClient';
import { RevitBridgeClient, type WebViewPort } from './revitBridgeClient';

declare global {
  interface Window {
    chrome?: { webview?: WebViewPort };
  }
}

/** Inside Revit's WebView2 the real bridge; in a normal browser (npm run dev) the mock with sample data. */
export function createBridge(): BridgeClient {
  const port = window.chrome?.webview;
  return port ? new RevitBridgeClient(port) : new MockBridgeClient();
}
