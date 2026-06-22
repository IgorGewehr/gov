// Helpers de apresentação compartilhados pelas telas do módulo Tributos.
import type { TagVariant } from '../../components/ui';
import type { SituacaoDividaAtiva, TipoTributo } from './api';

/** Rótulo legível em PT-BR para a situação da Dívida Ativa. */
export const SITUACAO_DIVIDA_LABEL: Record<SituacaoDividaAtiva, string> = {
  Inscrita: 'Inscrita',
  CdaEmitida: 'CDA emitida',
  Protestada: 'Protestada',
  EmExecucaoFiscal: 'Em execução fiscal',
  Parcelada: 'Parcelada',
  Quitada: 'Quitada',
  Cancelada: 'Cancelada',
};

/** Rótulo legível em PT-BR para a espécie tributária. */
export const TIPO_TRIBUTO_LABEL: Record<TipoTributo, string> = {
  Iptu: 'IPTU',
  Iss: 'ISS',
  Itbi: 'ITBI',
  Taxa: 'Taxa',
};

/** Mapeia a situação da dívida para a variante semântica da Tag (cor + texto). */
export function situacaoTagVariant(situacao: SituacaoDividaAtiva): TagVariant {
  switch (situacao) {
    case 'Inscrita':
      return 'warning';
    case 'CdaEmitida':
      return 'info';
    case 'Protestada':
      return 'danger';
    case 'EmExecucaoFiscal':
      return 'danger';
    case 'Parcelada':
      return 'info';
    case 'Quitada':
      return 'success';
    case 'Cancelada':
      return 'danger';
    default:
      return 'default';
  }
}

/**
 * A CDA só pode ser emitida enquanto a dívida estiver apenas Inscrita (ainda sem
 * CDA). Demais situações já têm CDA ou são terminais.
 */
export function podeEmitirCda(situacao: SituacaoDividaAtiva): boolean {
  return situacao === 'Inscrita';
}
