import { defineConfig } from "@playwright/test";

const frontendUrl = "http://localhost:13001";

export default defineConfig({
  testDir: "./e2e",
  webServer: {
    command: "npm exec -- vite --port 13001 --strictPort",
    url: frontendUrl,
    reuseExistingServer: false,
  },
  use: {
    baseURL: frontendUrl,
  },
});
