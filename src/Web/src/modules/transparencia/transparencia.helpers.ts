// Helpers de apresentação compartilhados pelas telas de Transparência.
import type { TagVariant, SelectOption } from '../../components/ui';
import type {
  SituacaoRemessa,
  SeveridadeCritica,
  TipoPeriodo,
} from './remessa.api';
import type { SituacaoDeclaracaoFiscal, TipoDeclaracaoFiscal } from './declaracao-fiscal.api';
import type { SetorMinimo, SituacaoMinimo } from './fiscal.api';

// ---------------------------------------------------------------------------
// Remessa TCE-RS (prestação de contas)
// ---------------------------------------------------------------------------

/** Rótulo legível (PT-BR) da situação da remessa. */
export const situacaoRemessaLabel: Record<SituacaoRemessa, string> = {
  Gerada: 'Gerada',
  Validada: 'Validada',
  ProntaParaTransmissao: 'Pronta para transmissão',
  Enviada: 'Enviada',
};

/** Mapeia a situação da remessa para a variante semântica da Tag (cor + texto). */
export function situacaoRemessaTagVariant(situacao: SituacaoRemessa): TagVariant {
  switch (situacao) {
    case 'Gerada':
      return 'info';
    case 'Validada':
      return 'warning';
    case 'ProntaParaTransmissao':
      return 'info';
    case 'Enviada':
      return 'success';
    default:
      return 'default';
  }
}

/** Mapeia a severidade de uma crítica do RDI para a variante da Tag. */
export function severidadeCriticaTagVariant(severidade: SeveridadeCritica): TagVariant {
  switch (severidade) {
    case 'Erro':
      return 'danger';
    case 'Alerta':
      return 'warning';
    case 'Informacao':
      return 'info';
    default:
      return 'default';
  }
}

/** Rótulo legível (PT-BR) do tipo de período. */
export const tipoPeriodoLabel: Record<TipoPeriodo, string> = {
  Mensal: 'Mensal',
  Bimestre: 'Bimestral',
  Quadrimestre: 'Quadrimestral',
  Anual: 'Anual',
};

export const tipoPeriodoOptions: SelectOption[] = (
  ['Mensal', 'Bimestre', 'Quadrimestre', 'Anual'] as const
).map((value) => ({ value, label: tipoPeriodoLabel[value] }));

export const situacaoRemessaOptions: SelectOption[] = (
  ['Gerada', 'Validada', 'ProntaParaTransmissao', 'Enviada'] as const
).map((value) => ({ value, label: situacaoRemessaLabel[value] }));

/** Meses (1..12) para o seletor de competência da remessa de FOLHA (Res. 1099, mensal). */
export const mesOptions: SelectOption[] = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
].map((label, indice) => ({ value: String(indice + 1), label }));

/** Versão padrão do leiaute de folha ao TCE-RS (Resolução 1099/2018). */
export const leiauteFolhaVersaoPadrao = '1099';

// ---------------------------------------------------------------------------
// Declaração Fiscal
// ---------------------------------------------------------------------------

/** Mapeia a situação da declaração para a variante semântica da Tag. */
export function situacaoDeclaracaoTagVariant(situacao: SituacaoDeclaracaoFiscal): TagVariant {
  switch (situacao) {
    case 'Consolidada':
      return 'info';
    case 'Transmitida':
      return 'warning';
    case 'Homologada':
      return 'success';
    case 'Rejeitada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Rótulo legível (PT-BR) da espécie de demonstrativo. */
export const tipoDeclaracaoLabel: Record<TipoDeclaracaoFiscal, string> = {
  Msc: 'MSC — Matriz de Saldos Contábeis',
  Rreo: 'RREO — Relatório Resumido da Execução Orçamentária',
  Rgf: 'RGF — Relatório de Gestão Fiscal',
  Dca: 'DCA — Declaração de Contas Anuais',
};

export const tipoDeclaracaoOptions: SelectOption[] = (
  ['Msc', 'Rreo', 'Rgf', 'Dca'] as const
).map((value) => ({ value, label: tipoDeclaracaoLabel[value] }));

export const situacaoDeclaracaoOptions: SelectOption[] = (
  ['Consolidada', 'Transmitida', 'Homologada', 'Rejeitada'] as const
).map((value) => ({ value, label: value }));

/** Ano de exercício corrente (default dos filtros). */
export function exercicioCorrente(): number {
  return new Date().getFullYear();
}

// ---------------------------------------------------------------------------
// Mínimos constitucionais (Saúde 15% ASPS / Educação 25% MDE)
// ---------------------------------------------------------------------------

/** Rótulo legível (PT-BR) do setor sujeito a mínimo constitucional. */
export const setorMinimoLabel: Record<SetorMinimo, string> = {
  Saude: 'Saúde',
  Educacao: 'Educação',
};

/** Fundamento legal do mínimo de cada setor (caption do card). */
export const setorMinimoFundamento: Record<SetorMinimo, string> = {
  Saude: 'ASPS — LC 141/2012 (mínimo 15% da receita-base).',
  Educacao: 'MDE — CF art. 212 (mínimo 25% da receita-base).',
};

/** Ícone Font Awesome representativo do setor. */
export const setorMinimoIcone: Record<SetorMinimo, string> = {
  Saude: 'fas fa-heart-pulse',
  Educacao: 'fas fa-graduation-cap',
};

/** Mapeia a situação do mínimo para a variante semântica da Tag (semáforo). */
export function situacaoMinimoTagVariant(situacao: SituacaoMinimo): TagVariant {
  return situacao === 'Atingido' ? 'success' : 'danger';
}

/** Rótulo legível (PT-BR) da situação do mínimo. */
export const situacaoMinimoLabel: Record<SituacaoMinimo, string> = {
  Atingido: 'Atingido',
  NaoAtingido: 'Não atingido',
};

/** Tom semântico (Metrica) a partir da situação do mínimo. */
export function situacaoMinimoTom(situacao: SituacaoMinimo): 'sucesso' | 'perigo' {
  return situacao === 'Atingido' ? 'sucesso' : 'perigo';
}

const percentual = new Intl.NumberFormat('pt-BR', {
  style: 'percent',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/** Formata uma fração 0..1 como percentual pt-BR (ex.: 0,1532 → "15,32%"). */
export function formatarPercentual(fracao: number): string {
  return percentual.format(fracao);
}

/**
 * Margem em PONTOS PERCENTUAIS do aplicado sobre o mínimo, com sinal explícito
 * (ex.: +0,32 p.p. / -1,10 p.p.). Negativa quando o mínimo não foi atingido.
 */
export function formatarMargemPontos(percentualAplicado: number, percentualMinimo: number): string {
  const pontos = (percentualAplicado - percentualMinimo) * 100;
  const arredondado = Math.round(pontos * 100) / 100;
  const sinal = arredondado > 0 ? '+' : '';
  const valor = arredondado.toLocaleString('pt-BR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  return `${sinal}${valor} p.p.`;
}
