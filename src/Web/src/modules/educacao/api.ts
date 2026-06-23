// Barrel da camada de API do módulo Educação. A implementação foi dividida por
// agregado para manter os arquivos < 300 linhas (CLAUDE.md §13 / regras de QA):
//   educacao.keys.ts  -> query keys centralizadas + CriadoResponse
//   escola.api.ts     -> DTOs + hooks do agregado Escola
//   matricula.api.ts  -> DTOs + hooks do agregado Matrícula
//   diario.api.ts     -> DTOs + hooks do agregado Diário de Classe
//
// As páginas/modais continuam importando de './api' (compatibilidade total).
export * from './educacao.keys';
export * from './aluno.api';
export * from './turma.api';
export * from './escola.api';
export * from './matricula.api';
export * from './diario.api';
export * from './diarioTurma.api';
export * from './fiscal.api';
export * from './merenda.api';
export * from './transporte.api';
