import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig(({ mode }) => ({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      "/api": {
        target: "http://127.0.0.1:8080",
      },
      "/gameHub": {
        target: "http://127.0.0.1:8080",
        ws: true,
      },
    },
  },

  esbuild: {
    loader: "jsx",
    include: /src\/.*\.jsx?$/,
    exclude: [],
    drop: mode === "production" ? ["debugger"] : [],
    pure:
      mode === "production"
        ? ["console.log", "console.debug", "console.info", "console.warn"]
        : [],
  },
  optimizeDeps: {
    esbuildOptions: {
      loader: { ".js": "jsx" },
    },
  },
}));
