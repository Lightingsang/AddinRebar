import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// The build is embedded in the Revit add-ins and served from their virtual hosts under a strict CSP: relative asset URLs,
// no inline scripts (no module-preload polyfill), nothing fetched from the network. One HTML page per window:
// index.html = HP BIM Toolkit's dashboard pane, kata.html = HPRebar's Kata Export window (Revit 2026).
export default defineConfig({
  base: './',
  plugins: [react(), tailwindcss()],
  // `@/` = src/, as the shadcn/ui components under src/components/ui import their helpers.
  resolve: { alias: { '@': '/src' } },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    target: 'es2022',
    modulePreload: { polyfill: false },
    assetsInlineLimit: 0,
    // three.js (~560 kB) is its own chunk, loaded from the embedded resources only when Kata Export's 3D tab opens.
    chunkSizeWarningLimit: 650,
    rollupOptions: {
      input: { index: 'index.html', kata: 'kata.html' },
    },
  },
  server: { port: 5173, strictPort: true },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
});
