// Helpers de apresentação compartilhados pelas telas de Licitacao.
// Rótulos em PT-BR (linguagem cidadã) e mapeamento de status -> variante de Tag.
import type { TagVariant, SelectOption } from '../../../components/ui';
import type {
  CriterioJulgamento,
  ModalidadeLicitacao,
  ResultadoHabilitacao,
  SituacaoLicitacao,
  SituacaoProposta,
} from './licitacao.api';

/** Rótulo legível da situação do certame. */
export const SITUACAO_LABEL: Record<SituacaoLicitacao, string> = {
  Aberta: 'Aberta',
  EmJulgamento: 'Em julgamento',
  Homologada: 'Homologada',
  Fracassada: 'Fracassada',
  Deserta: 'Deserta',
  Revogada: 'Revogada',
  Anulada: 'Anulada',
};

/** Rótulo legível da modalidade. */
export const MODALIDADE_LABEL: Record<ModalidadeLicitacao, string> = {
  Pregao: 'Pregão',
  Concorrencia: 'Concorrência',
  DialogoCompetitivo: 'Diálogo competitivo',
  Dispensa: 'Dispensa',
  Inexigibilidade: 'Inexigibilidade',
};

/** Rótulo legível do critério de julgamento. */
export const CRITERIO_LABEL: Record<CriterioJulgamento, string> = {
  MenorPreco: 'Menor preço',
  MaiorDesconto: 'Maior desconto',
  MelhorTecnica: 'Melhor técnica',
  TecnicaEPreco: 'Técnica e preço',
  MaiorLance: 'Maior lance',
  MaiorRetornoEconomico: 'Maior retorno econômico',
};

/** Rótulo legível da situação da proposta. */
export const SITUACAO_PROPOSTA_LABEL: Record<SituacaoProposta, string> = {
  Recebida: 'Recebida',
  Classificada: 'Classificada',
  Desclassificada: 'Desclassificada',
  Vencedora: 'Vencedora',
};

/** Rótulo legível do resultado de habilitação. */
export const RESULTADO_HABILITACAO_LABEL: Record<ResultadoHabilitacao, string> = {
  Habilitado: 'Habilitado',
  Inabilitado: 'Inabilitado',
};

/** Mapeia a situação do certame para a variante semântica da Tag (cor). */
export function situacaoTagVariant(situacao: SituacaoLicitacao): TagVariant {
  switch (situacao) {
    case 'Aberta':
      return 'info';
    case 'EmJulgamento':
      return 'warning';
    case 'Homologada':
      return 'success';
    case 'Fracassada':
    case 'Deserta':
    case 'Revogada':
    case 'Anulada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação da proposta para a variante da Tag. */
export function propostaTagVariant(situacao: SituacaoProposta): TagVariant {
  switch (situacao) {
    case 'Vencedora':
      return 'success';
    case 'Classificada':
      return 'info';
    case 'Recebida':
      return 'default';
    case 'Desclassificada':
      return 'danger';
    default:
      return 'default';
  }
}

// Opções dos selects (ordem = ordem dos enums no domínio).
export const SITUACAO_OPTIONS: SelectOption[] = (
  ['Aberta', 'EmJulgamento', 'Homologada', 'Fracassada', 'Deserta', 'Revogada', 'Anulada'] as const
).map((s) => ({ value: s, label: SITUACAO_LABEL[s] }));

export const MODALIDADE_OPTIONS: SelectOption[] = (
  ['Pregao', 'Concorrencia', 'DialogoCompetitivo', 'Dispensa', 'Inexigibilidade'] as const
).map((m) => ({ value: m, label: MODALIDADE_LABEL[m] }));

export const CRITERIO_OPTIONS: SelectOption[] = (
  ['MenorPreco', 'MaiorDesconto', 'MelhorTecnica', 'TecnicaEPreco', 'MaiorLance', 'MaiorRetornoEconomico'] as const
).map((c) => ({ value: c, label: CRITERIO_LABEL[c] }));

export const RESULTADO_HABILITACAO_OPTIONS: SelectOption[] = (
  ['Habilitado', 'Inabilitado'] as const
).map((r) => ({ value: r, label: RESULTADO_HABILITACAO_LABEL[r] }));

// Critérios admitidos para Pregão (I-5 / art. 6º, XLI).
const CRITERIOS_PREGAO: ReadonlySet<CriterioJulgamento> = new Set(['MenorPreco', 'MaiorDesconto']);

/** true se a combinação modalidade × critério é permitida (espelha I-5). */
export function combinacaoModalidadeCriterioValida(
  modalidade: ModalidadeLicitacao,
  criterio: CriterioJulgamento,
): boolean {
  if (modalidade === 'Pregao') return CRITERIOS_PREGAO.has(criterio);
  return true;
}
