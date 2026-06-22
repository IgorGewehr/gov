// Barrel da camada de API do modulo Legislativo (Camara de Vereadores). Reexporta
// as constantes compartilhadas e os hooks/DTOs por agregado, mantendo o ponto unico
// de import (`./api`) das telas e cada arquivo < 300 linhas (CLAUDE.md §13).
//   legislativo.shared.ts -> enums de dominio + query keys + CriacaoResponse
//   proposicao.api.ts      -> 12 endpoints de /api/legislativo/proposicoes
//   sessao.api.ts          -> 12 endpoints de /api/legislativo/sessoes
//   votacao.api.ts         -> 7 endpoints de /api/legislativo/votacoes
export * from './legislativo.shared';
export * from './proposicao.api';
export * from './sessao.api';
export * from './votacao.api';
export * from './vereadores.api';
export * from './demonstracao.api';
export * from './normas.api';
export * from './diario.api';
export * from './tribuna.api';
export * from './comissoes.api';
