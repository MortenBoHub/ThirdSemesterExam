import js from '@eslint/js'
import tseslint from 'typescript-eslint'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import globals from 'globals'

export default tseslint.config([
    {
        ignores: [
            'dist/**',
            'node_modules/**',
            'coverage/**',
            'src/core/generated-client.ts',
            'eslint.config.js',
            '.dependency-cruiser.cjs',
        ],
    },
    js.configs.recommended,
    ...tseslint.configs.recommended,
    // Node / tool config files
    {
        files: ['eslint.config.js', '*.cjs', 'vite.config.ts', 'vitest.config.ts'],
        languageOptions: {
            globals: globals.node,
            parserOptions: {
                project: null,
            },
        },
    },
    {
        files: ['**/*.{ts,tsx}'],
        languageOptions: {
            parserOptions: {
                // Classic project mode pointing to a tsconfig that includes all linted files.
                project: ['./tsconfig.eslint.json'],
            },
        },
        plugins: { 'react-hooks': reactHooks, 'react-refresh': reactRefresh },
        rules: {
            'react-hooks/rules-of-hooks': 'error',
            'react-hooks/exhaustive-deps': 'warn',
            'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
            'no-mixed-spaces-and-tabs': 'off',
            // Loosen a few noisy rules initially; we can tighten later.
            '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }],
            '@typescript-eslint/no-explicit-any': 'off',
            '@typescript-eslint/no-empty-object-type': 'off',
            // Allow intentional empty blocks with a comment marker (warn only for now)
            'no-empty': ['warn', { allowEmptyCatch: true }],
            '@typescript-eslint/ban-ts-comment': ['warn', { 'ts-ignore': 'allow-with-description' }],
        },
    },
])
