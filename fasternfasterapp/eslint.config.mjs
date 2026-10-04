import js from "@eslint/js";
import globals from "globals";
import pluginReact from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import { defineConfig } from "eslint/config";

export default defineConfig([
  { files: ["**/*.{js,mjs,cjs,jsx}"], plugins: { js }, extends: ["js/recommended"], languageOptions: { globals: globals.browser } },
  pluginReact.configs.flat.recommended,
  pluginReact.configs.flat["jsx-runtime"],
  {
    plugins: { "reactHooks" : reactHooks },
    settings: { react: { version: "detect" } },
    rules: {
      "reactHooks/rules-of-hooks": "error",
      "reactHooks/exhaustive-deps": "warn",
      "react/prop-types": [0]
    },
  },
]);
