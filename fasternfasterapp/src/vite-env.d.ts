/// <reference types="vite/client" />

interface ViteTypeOptions {
  strictImportMetaEnv: unknown;
}

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_MOCK_LEADERBOARD?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
