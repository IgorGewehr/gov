// Tipos e helpers de paginação compartilhados pelas telas de LISTA/BUSCA do módulo Patrimonio
// (Onda 0 — Navegabilidade). Espelham o envelope REAL do backend `ResultadoPaginado<T>`:
//   { Itens, Total, Pagina, Tamanho }  (tamanho default 20, máx 100).
// Mantido LOCAL ao módulo (não é componente de UI base): é só contrato + cálculo de páginas.

/** Envelope paginado devolvido por todos os list/search do backend. */
export interface ResultadoPaginado<T> {
  itens: T[];
  total: number;
  pagina: number;
  tamanho: number;
}

/** Tamanho de página padrão das listas navegáveis (alinhado ao default do backend). */
export const TAMANHO_PAGINA_PADRAO = 20;

/** Número total de páginas para um resultado (mínimo 1, mesmo vazio). */
export function totalPaginas(total: number, tamanho: number): number {
  if (tamanho <= 0) return 1;
  return Math.max(1, Math.ceil(total / tamanho));
}

/** Índice (1-based) do primeiro item da página atual, ou 0 quando vazio. */
export function primeiroDaPagina(pagina: number, tamanho: number, total: number): number {
  if (total === 0) return 0;
  return (pagina - 1) * tamanho + 1;
}

/** Índice (1-based) do último item exibido na página atual. */
export function ultimoDaPagina(pagina: number, tamanho: number, total: number): number {
  return Math.min(pagina * tamanho, total);
}
