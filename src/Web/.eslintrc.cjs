// Enforcement do Design System Constitution: acessibilidade obrigatória no JSX.
module.exports = {
  root: true,
  env: { browser: true, es2022: true, node: true },
  parser: '@typescript-eslint/parser',
  parserOptions: { ecmaVersion: 'latest', sourceType: 'module', ecmaFeatures: { jsx: true } },
  settings: { react: { version: 'detect' } },
  plugins: ['react', 'react-hooks', 'jsx-a11y', '@typescript-eslint'],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react/recommended',
    'plugin:react/jsx-runtime',
    'plugin:react-hooks/recommended',
    // Acessibilidade (eMAG / WCAG 2.1 AA) — build falha em violação.
    'plugin:jsx-a11y/recommended',
  ],
  ignorePatterns: ['dist', 'node_modules', 'coverage', '*.config.ts', '*.config.cjs'],
  rules: {
    'jsx-a11y/lang': 'error',
    'jsx-a11y/no-autofocus': 'error',
    'jsx-a11y/label-has-associated-control': 'error',
    // Design System Constitution §5 / eMAG: accesskeys gov.br padronizadas são
    // OBRIGATÓRIAS (conteúdo=1, menu=2, busca=3; 1º link salta para o conteúdo).
    // O default do plugin proíbe accessKey de forma genérica; aqui o padrão de
    // governo prevalece — uso restrito a essas teclas no shell.
    'jsx-a11y/no-access-key': 'off',
    // O TypeScript já cobre variáveis não usadas; evita duplicidade com no-unused-vars.
    'no-unused-vars': 'off',
    '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    '@typescript-eslint/consistent-type-imports': ['error', { prefer: 'type-imports' }],
  },
  overrides: [
    {
      files: ['**/*.{test,spec}.{ts,tsx}', 'src/test/**'],
      env: { node: true },
    },
  ],
};
