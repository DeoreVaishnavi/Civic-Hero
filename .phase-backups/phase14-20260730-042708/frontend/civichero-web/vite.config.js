import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: 'http://localhost:5180', changeOrigin: true },
      '/health': { target: 'http://localhost:5180', changeOrigin: true },
      '/hubs': { target: 'http://localhost:5180', changeOrigin: true, ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setupTests.js',
    css: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'lcov', 'json-summary'],
      reportsDirectory: './coverage',
      include: ['src/**/*.{js,jsx}'],
      exclude: ['src/main.jsx', 'src/test/**'],
      thresholds: { lines: 20, functions: 20, branches: 15, statements: 20 },
    },
  },
});
