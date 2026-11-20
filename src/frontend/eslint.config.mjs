import js from '@eslint/js';
import { defineConfig, globalIgnores } from 'eslint/config';
import nextVitals from 'eslint-config-next/core-web-vitals';

export default defineConfig([
  ...nextVitals,
  js.configs.recommended,
  {
    rules: {
      'no-undef': 'off',
      'no-unused-vars': 'off',
      'prefer-const': 'error',
      'no-console': ['warn', { allow: ['warn', 'error'] }],
    },
  },
  globalIgnores(['.next/**', 'node_modules/**', 'coverage/**', 'next-env.d.ts', 'src/lib/api/schema.d.ts']),
]);
