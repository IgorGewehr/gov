// Helpers de apresentacao compartilhados pelas telas do agregado Beneficio.
import type { TagVariant } from '../../../components/ui';
import type { SituacaoBeneficio, TipoBeneficio } from './beneficio.api';

/** Mapeia a situacao do beneficio para a variante semantica da Tag (cor + texto). */
export function situacaoTagVariant(situacao: SituacaoBeneficio): TagVariant {
  switch (situacao) {
    case 'EmAvaliacao':
      return 'warning';
    case 'Concedida':
      return 'success';
    case 'Indeferida':
      return 'danger';
    default:
      return 'default';
  }
}

/** Rotulo legivel da situacao para leitores e exibicao (PT-BR). */
export function situacaoLabel(situacao: SituacaoBeneficio): string {
  switch (situacao) {
    case 'EmAvaliacao':
      return 'Em avaliação';
    case 'Concedida':
      return 'Concedida';
    case 'Indeferida':
      return 'Indeferida';
    default:
      return situacao;
  }
}

/** Rotulo legivel do tipo de beneficio (PT-BR). */
export function tipoLabel(tipo: TipoBeneficio): string {
  switch (tipo) {
    case 'Bpc':
      return 'BPC (Prestação Continuada)';
    case 'Pbf':
      return 'PBF (Bolsa Família)';
    case 'Eventual':
      return 'Benefício eventual';
    default:
      return tipo;
  }
}

/** Opcoes do Select de tipo de beneficio. */
export const TIPO_OPTIONS: { value: TipoBeneficio; label: string }[] = [
  { value: 'Bpc', label: tipoLabel('Bpc') },
  { value: 'Pbf', label: tipoLabel('Pbf') },
  { value: 'Eventual', label: tipoLabel('Eventual') },
];

/** Opcoes de mes (1-12) para o Select de competencia. */
export const MES_OPTIONS: { value: string; label: string }[] = [
  { value: '1', label: 'Janeiro' },
  { value: '2', label: 'Fevereiro' },
  { value: '3', label: 'Março' },
  { value: '4', label: 'Abril' },
  { value: '5', label: 'Maio' },
  { value: '6', label: 'Junho' },
  { value: '7', label: 'Julho' },
  { value: '8', label: 'Agosto' },
  { value: '9', label: 'Setembro' },
  { value: '10', label: 'Outubro' },
  { value: '11', label: 'Novembro' },
  { value: '12', label: 'Dezembro' },
];

/**
 * Indica se a entrega de cesta basica e permitida sobre o beneficio (guarda I-8):
 * apenas beneficio Concedida do tipo Eventual.
 */
export function podeEntregarCesta(situacao: SituacaoBeneficio, tipo: TipoBeneficio): boolean {
  return situacao === 'Concedida' && tipo === 'Eventual';
}
