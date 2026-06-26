// Helpers de apresentação compartilhados pelas telas de Dispensa Eletrônica.
// Rótulos em PT-BR (linguagem cidadã) e mapeamento de status -> variante de Tag.
import type { TagVariant, SelectOption } from '../../../components/ui';
import type {
  CriterioJulgamentoDispensa,
  FundamentoDispensaValor,
  SituacaoCotacao,
  SituacaoDispensa,
} from './dispensa.api';

/** Rótulo legível da situação do procedimento. */
export const SITUACAO_LABEL: Record<SituacaoDispensa, string> = {
  Aberta: 'Aberta (rascunho)',
  AvisoPublicado: 'Aviso publicado',
  EmDisputa: 'Em disputa',
  EmJulgamento: 'Em julgamento',
  Homologada: 'Homologada',
  Fracassada: 'Fracassada',
  Deserta: 'Deserta',
  Revogada: 'Revogada',
  Anulada: 'Anulada',
};

/** Rótulo legível do fundamento legal (art. 75, I/II). */
export const FUNDAMENTO_LABEL: Record<FundamentoDispensaValor, string> = {
  ObrasEServicosEngenharia: 'Obras e serviços de engenharia (art. 75, I)',
  OutrosServicosECompras: 'Outros serviços e compras (art. 75, II)',
};

/** Rótulo legível do critério de julgamento. */
export const CRITERIO_LABEL: Record<CriterioJulgamentoDispensa, string> = {
  MenorPreco: 'Menor preço',
  MaiorDesconto: 'Maior desconto',
};

/** Rótulo legível da situação da cotação. */
export const SITUACAO_COTACAO_LABEL: Record<SituacaoCotacao, string> = {
  Recebida: 'Recebida',
  Classificada: 'Classificada',
  Desclassificada: 'Desclassificada',
  Vencedora: 'Vencedora',
};

/** Mapeia a situação do procedimento para a variante semântica da Tag (cor). */
export function situacaoTagVariant(situacao: SituacaoDispensa): TagVariant {
  switch (situacao) {
    case 'Aberta':
    case 'AvisoPublicado':
      return 'info';
    case 'EmDisputa':
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

/** Mapeia a situação da cotação para a variante da Tag. */
export function cotacaoTagVariant(situacao: SituacaoCotacao): TagVariant {
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
  [
    'Aberta',
    'AvisoPublicado',
    'EmDisputa',
    'EmJulgamento',
    'Homologada',
    'Fracassada',
    'Deserta',
    'Revogada',
    'Anulada',
  ] as const
).map((s) => ({ value: s, label: SITUACAO_LABEL[s] }));

export const FUNDAMENTO_OPTIONS: SelectOption[] = (
  ['ObrasEServicosEngenharia', 'OutrosServicosECompras'] as const
).map((f) => ({ value: f, label: FUNDAMENTO_LABEL[f] }));

export const CRITERIO_OPTIONS: SelectOption[] = (
  ['MenorPreco', 'MaiorDesconto'] as const
).map((c) => ({ value: c, label: CRITERIO_LABEL[c] }));
