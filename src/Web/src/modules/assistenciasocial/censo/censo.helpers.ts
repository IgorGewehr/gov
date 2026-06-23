// Helpers de apresentacao compartilhados pelas telas do Censo SUAS.
import type { TipoServico, TipoUnidadeAtendimento } from './censo.api';

/** Rotulo legivel do tipo de unidade (PT-BR). */
export function tipoUnidadeLabel(tipo: TipoUnidadeAtendimento): string {
  switch (tipo) {
    case 'Cras':
      return 'CRAS — proteção social básica';
    case 'Creas':
      return 'CREAS — proteção social especial';
    case 'CentroPop':
      return 'Centro POP — população em situação de rua';
    default:
      return tipo;
  }
}

/** Sigla curta do tipo de unidade (para colunas estreitas). */
export function tipoUnidadeSigla(tipo: TipoUnidadeAtendimento): string {
  switch (tipo) {
    case 'Cras':
      return 'CRAS';
    case 'Creas':
      return 'CREAS';
    case 'CentroPop':
      return 'Centro POP';
    default:
      return tipo;
  }
}

/** Rotulo legivel do servico tipificado (Res. CNAS 109/2009). */
export function servicoLabel(servico: TipoServico): string {
  switch (servico) {
    case 'Paif':
      return 'PAIF (CRAS)';
    case 'Paefi':
      return 'PAEFI (CREAS)';
    case 'Scfv':
      return 'SCFV';
    default:
      return servico;
  }
}

/** Opcoes do Select de tipo de unidade. */
export const TIPO_UNIDADE_OPTIONS: { value: TipoUnidadeAtendimento; label: string }[] = [
  { value: 'Cras', label: tipoUnidadeLabel('Cras') },
  { value: 'Creas', label: tipoUnidadeLabel('Creas') },
  { value: 'CentroPop', label: tipoUnidadeLabel('CentroPop') },
];

/** Opcoes do Select de servico tipificado. */
export const SERVICO_OPTIONS: { value: TipoServico; label: string }[] = [
  { value: 'Paif', label: servicoLabel('Paif') },
  { value: 'Paefi', label: servicoLabel('Paefi') },
  { value: 'Scfv', label: servicoLabel('Scfv') },
];
