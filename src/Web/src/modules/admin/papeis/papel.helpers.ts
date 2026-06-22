// Helpers de apresentação da gestão de Papéis (RBAC).
// A principal responsabilidade é AGRUPAR o catálogo de permissões pelo prefixo
// do módulo (ex.: "tributos.*", "identidade.*") para exibir os checkboxes em
// blocos legíveis no PapelPermissoesModal.
import type { Permissao } from './papel.api';

/** Um grupo de permissões pertencentes ao mesmo módulo (prefixo). */
export interface GrupoPermissoes {
  /** Prefixo do módulo (1º segmento antes do primeiro ponto). Ex.: "tributos". */
  prefixo: string;
  /** Rótulo legível do grupo (prefixo capitalizado). */
  label: string;
  /** Permissões do grupo, ordenadas por chave. */
  permissoes: Permissao[];
}

/** Extrai o prefixo de módulo de uma chave de permissão ("tributos.cda.gerar" -> "tributos"). */
export function prefixoDaPermissao(chave: string): string {
  const ponto = chave.indexOf('.');
  return ponto === -1 ? chave : chave.slice(0, ponto);
}

/** Capitaliza a primeira letra de um termo para uso como rótulo de grupo. */
export function capitalizar(termo: string): string {
  if (termo.length === 0) return termo;
  return termo.charAt(0).toUpperCase() + termo.slice(1);
}

/**
 * Agrupa o catálogo de permissões por prefixo de módulo, ordenando os grupos e
 * as permissões internas alfabeticamente. Permissões sem chave são descartadas.
 */
export function agruparPermissoes(catalogo: Permissao[]): GrupoPermissoes[] {
  const mapa = new Map<string, Permissao[]>();
  for (const permissao of catalogo) {
    if (!permissao.chave) continue;
    const prefixo = prefixoDaPermissao(permissao.chave);
    const lista = mapa.get(prefixo);
    if (lista) lista.push(permissao);
    else mapa.set(prefixo, [permissao]);
  }

  return Array.from(mapa.entries())
    .map(([prefixo, permissoes]) => ({
      prefixo,
      label: capitalizar(prefixo),
      permissoes: [...permissoes].sort((a, b) => a.chave.localeCompare(b.chave)),
    }))
    .sort((a, b) => a.prefixo.localeCompare(b.prefixo));
}

/** Texto curto descrevendo a quantidade de permissões de um papel (PT-BR, plural). */
export function rotuloQuantidadePermissoes(quantidade: number): string {
  if (quantidade === 0) return 'Nenhuma permissão';
  if (quantidade === 1) return '1 permissão';
  return `${quantidade} permissões`;
}
