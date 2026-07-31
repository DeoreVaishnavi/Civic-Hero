import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: { port: 5173 },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          react: ["react", "react-dom", "react-router"],
          mui: ["@mui/material", "@mui/icons-material", "@emotion/react", "@emotion/styled"],
          data: ["axios", "@tanstack/react-query", "react-hook-form", "zod", "@hookform/resolvers"],
          maps: ["leaflet", "react-leaflet", "leaflet.heat"],
          i18n: ["i18next", "react-i18next", "i18next-browser-languagedetector"],
          realtime: ["@microsoft/signalr"],
        },
      },
    },
  },
  test: { environment: "jsdom", setupFiles: "./src/test/setupTests.js", include: ["src/**/*.test.{js,jsx}"] },
});
