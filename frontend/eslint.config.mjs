import { dirname } from "path";
import { fileURLToPath } from "url";
import { FlatCompat } from "@eslint/eslintrc";
import boundaries from "eslint-plugin-boundaries";
import unusedImports from "eslint-plugin-unused-imports";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

export default [
  ...compat.extends("next/core-web-vitals", "next/typescript", "prettier"),
  {
    plugins: {
      boundaries,
      "unused-imports": unusedImports,
    },
    settings: {
      "boundaries/include": ["src/**/*"],
      "boundaries/elements": [
        { type: "app", pattern: "src/app/*" },
        { type: "widgets", pattern: "src/widgets/*" },
        { type: "features", pattern: "src/features/*" },
        { type: "entities", pattern: "src/entities/*" },
        { type: "shared", pattern: "src/shared/*" },
      ],
      "import/resolver": {
        typescript: {
          alwaysTryTypes: true,
          project: "./tsconfig.json",
        },
      },
    },
    rules: {
      "unused-imports/no-unused-imports": "error",
      "boundaries/entry-point": "off",
      "boundaries/element-types": [
        "error",
        {
          default: "disallow",
          message: "${file.type} is not allowed to import ${dependency.type}",
          rules: [
            {
              from: ["app"],
              allow: ["widgets", "features", "entities", "shared"],
            },
            {
              from: ["widgets"],
              allow: ["features", "entities", "shared"],
            },
            {
              from: ["features"],
              allow: ["entities", "shared"],
            },
            {
              from: ["entities"],
              allow: ["shared"],
            },
            {
              from: ["shared"],
              allow: ["shared"],
            },
          ],
        },
      ],
    },
  },
];
