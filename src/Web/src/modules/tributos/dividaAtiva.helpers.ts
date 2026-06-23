// Helpers de apresentação compartilhados pelas telas do módulo Tributos.
import type { TagVariant } from '../../components/ui';
import type { OcorrenciaProtesto, SituacaoDividaAtiva, TipoTributo } from './api';

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

/** Rótulo legível em PT-BR para a ocorrência de retorno do protesto. */
export const OCORRENCIA_PROTESTO_LABEL: Record<OcorrenciaProtesto, string> = {
  Lavrado: 'Protesto lavrado',
  PagoOuRetirado: 'Pago/retirado em cartório',
  Sustado: 'Sustado (ordem judicial)',
  Rejeitado: 'Rejeitado pelo cartório',
};

/**
 * A CDA só pode ser emitida enquanto a dívida estiver apenas Inscrita (ainda sem
 * CDA). Demais situações já têm CDA ou são terminais.
 */
export function podeEmitirCda(situacao: SituacaoDividaAtiva): boolean {
  return situacao === 'Inscrita';
}

/**
 * O protesto extrajudicial (Lei 9.492/97) só ocorre com CDA já emitida e enquanto
 * a dívida não estiver protestada/executada/terminal. Domínio exige Situacao = CdaEmitida.
 */
export function podeProtestar(situacao: SituacaoDividaAtiva): boolean {
  return situacao === 'CdaEmitida';
}

/**
 * A execução fiscal (Lei 6.830/80) exige CDA emitida ou protestada (domínio:
 * Situacao in {CdaEmitida, Protestada}).
 */
export function podeExecutar(situacao: SituacaoDividaAtiva): boolean {
  return situacao === 'CdaEmitida' || situacao === 'Protestada';
}
