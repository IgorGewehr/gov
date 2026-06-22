// Helpers de apresentação e validação das ATRIBUIÇÕES de papel com escopo.
import type { SelectOption } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import type { AtribuicaoResumo, UnidadeNo } from './atribuicao.api';

/**
 * Achata a árvore de unidades em opções de <select>, indentando pelo nível para
 * preservar a hierarquia visual. Unidades inativas ficam desabilitadas (não é
 * possível atribuir escopo a uma UO desativada). Inclui o código para desambiguar.
 */
export function unidadesParaOpcoes(arvore: UnidadeNo[] | undefined): SelectOption[] {
  const opcoes: SelectOption[] = [];
  const visitar = (nos: UnidadeNo[], nivel: number): void => {
    for (const no of nos) {
      const prefixo = nivel > 0 ? `${'  '.repeat(nivel)}↳ ` : '';
      opcoes.push({
        value: no.id,
        label: `${prefixo}${no.nome} (${no.codigo})${no.ativo ? '' : ' — inativa'}`,
        disabled: !no.ativo,
      });
      if (no.filhos?.length) visitar(no.filhos, nivel + 1);
    }
  };
  visitar(arvore ?? [], 0);
  return opcoes;
}

/** Mapa id -> nome legível da unidade (para exibir atribuições por nome). */
export function mapaUnidadesNome(arvore: UnidadeNo[] | undefined): Map<string, string> {
  const mapa = new Map<string, string>();
  const visitar = (nos: UnidadeNo[]): void => {
    for (const no of nos) {
      mapa.set(no.id, `${no.nome} (${no.codigo})`);
      if (no.filhos?.length) visitar(no.filhos);
    }
  };
  visitar(arvore ?? []);
  return mapa;
}

/** Rótulo legível da vigência: "Indeterminada" ou data formatada (PT-BR). */
export function vigenciaLabel(vigenciaFim: string | null | undefined): string {
  if (!vigenciaFim) return 'Indeterminada';
  // Aceita 'yyyy-MM-dd' (data pura) sem deslocamento de fuso.
  const partes = vigenciaFim.slice(0, 10).split('-');
  if (partes.length === 3) {
    const [ano, mes, dia] = partes;
    return `Até ${dia}/${mes}/${ano}`;
  }
  return `Até ${vigenciaFim}`;
}

/** Rótulo do escopo: subunidades incluídas ou apenas a própria UO. */
export function escopoLabel(incluiSubunidades: boolean): string {
  return incluiSubunidades ? 'Esta UO e subunidades' : 'Apenas esta UO';
}

/**
 * Identidade canônica de uma atribuição (papel + UO + escopo) — usada como key de
 * lista e para casar a linha em revogação otimista.
 */
export function atribuicaoKey(a: Pick<AtribuicaoResumo, 'papelId' | 'unidadeId' | 'incluiSubunidades'>): string {
  return `${a.papelId}|${a.unidadeId}|${a.incluiSubunidades ? '1' : '0'}`;
}

/**
 * Mensagem amigável (PT-BR) para erros de atribuição. Trata o 403 da regra
 * "não delega o que não tem" de forma explícita, sem vazar detalhes técnicos.
 */
export function mensagemErroAtribuicao(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    if (error.status === 403) {
      return (
        'Você não pode conceder ou revogar um papel além do seu próprio escopo. ' +
        'Solicite a uma chefia com escopo superior nesta unidade.'
      );
    }
    return error.userMessage;
  }
  return fallback;
}
