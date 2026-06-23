// Helpers de apresentação compartilhados pelas telas de Transparência.
import type { TagVariant, SelectOption } from '../../components/ui';
import type {
  SituacaoRemessa,
  SeveridadeCritica,
  TipoPeriodo,
} from './remessa.api';
import type { SituacaoDeclaracaoFiscal, TipoDeclaracaoFiscal } from './declaracao-fiscal.api';

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
