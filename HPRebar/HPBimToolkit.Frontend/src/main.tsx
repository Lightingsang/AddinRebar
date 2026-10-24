import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { createBridge } from './bridge/createBridge';
import './index.css';
import { ToastProvider } from './ui/Toast';

const bridge = createBridge();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ToastProvider>
      <App bridge={bridge} />
    </ToastProvider>
  </StrictMode>,
);
