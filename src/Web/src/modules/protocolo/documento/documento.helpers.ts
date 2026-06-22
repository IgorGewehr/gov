// Helpers de apresentacao compartilhados pelas telas do agregado Documento.
import type { SelectOption, TagVariant } from '../../../components/ui';
import type {
  CriticidadeAto,
  SituacaoDocumento,
  TipoAssinatura,
} from './documento.api';
import { NivelMinimoPorCriticidade, TipoAssinaturaValor } from './documento.api';

/** Cor semantica da Tag conforme a situacao do documento (ciclo de vida). */
export function situacaoTagVariant(situacao: string): TagVariant {
  switch (situacao as SituacaoDocumento) {
    case 'Assinado':
      return 'success';
    case 'Juntado':
      return 'info';
    case 'Rascunho':
      return 'warning';
    case 'SemEfeito':
      return 'danger';
    default:
      return 'default';
  }
}

/** Cor semantica da Tag conforme a criticidade do ato. */
export function criticidadeTagVariant(criticidade: string): TagVariant {
  switch (criticidade as CriticidadeAto) {
    case 'Alta':
      return 'danger';
    case 'Media':
      return 'warning';
    case 'Baixa':
      return 'info';
    default:
      return 'default';
  }
}

/** Rotulo legivel (PT-BR) para o tipo de assinatura. */
export function rotuloTipoAssinatura(tipo: string | null | undefined): string {
  switch (tipo as TipoAssinatura) {
    case 'AssinaturaSimples':
      return 'Simples (gov.br bronze)';
    case 'AssinaturaAvancada':
      return 'Avancada (gov.br prata/ouro)';
    case 'AssinaturaQualificada':
      return 'Qualificada (ICP-Brasil)';
    default:
      return '—';
  }
}

/** Indica se a situacao admite assinar / tornar sem efeito (Juntado | Assinado). */
export function permiteTransicao(situacao: string): boolean {
  return situacao === 'Juntado' || situacao === 'Assinado';
}

/** Opcoes do <Select> de criticidade (rotulos com o nivel minimo exigido). */
export const OPCOES_CRITICIDADE: SelectOption[] = [
  { value: 'Baixa', label: 'Baixa — requerimento do cidadao (min.: Simples)' },
  { value: 'Media', label: 'Media — ato interno (min.: Avancada)' },
  { value: 'Alta', label: 'Alta — dirigente maximo / bens imoveis (min.: Qualificada)' },
];

/** Opcoes do <Select> de nivel de acesso. */
export const OPCOES_NIVEL_ACESSO: SelectOption[] = [
  { value: 'Publico', label: 'Publico' },
  { value: 'Restrito', label: 'Restrito' },
  { value: 'Sigiloso', label: 'Sigiloso' },
];

/** Opcoes do <Select> de tipo de assinatura. */
export const OPCOES_TIPO_ASSINATURA: SelectOption[] = [
  { value: 'AssinaturaSimples', label: 'Simples (gov.br bronze)' },
  { value: 'AssinaturaAvancada', label: 'Avancada (gov.br prata/ouro)' },
  { value: 'AssinaturaQualificada', label: 'Qualificada (ICP-Brasil)' },
];

/**
 * Valida no cliente a adequacao do nivel de assinatura a criticidade (I-5/I-6).
 * O dominio e a fonte da verdade; isto melhora o feedback antes do POST.
 */
export function assinaturaAtendeCriticidade(criticidade: CriticidadeAto, tipo: TipoAssinatura): boolean {
  return TipoAssinaturaValor[tipo] >= TipoAssinaturaValor[NivelMinimoPorCriticidade[criticidade]];
}
