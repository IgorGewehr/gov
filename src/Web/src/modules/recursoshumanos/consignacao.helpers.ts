// Apoio de apresentação das CONSIGNAÇÕES: permissões granulares (espelham o RBAC do
// backend), rótulos PT-BR dos enums, opções de Select e variantes de Tag. Mantém os
// nomes de enum exatamente como o backend serializa (JsonStringEnumConverter).
import type { SelectOption, TagVariant } from '../../components/ui';
import type {
  CategoriaConsignavel,
  GrupoMargem,
  SituacaoConsignacao,
  SituacaoConsignataria,
  TipoConsignataria,
} from './consignacao.api';

/** Ver margem/consignatárias/consignações. */
export const PERM_CONSIGNACAO_VER = 'rh.consignacao.ver';

/** Cadastrar/suspender/reativar/cancelar (gestão). */
export const PERM_CONSIGNACAO_GERENCIAR = 'rh.consignacao.gerenciar';

/** Averbar contrato (operação dedicada). */
export const PERM_CONSIGNACAO_AVERBAR = 'rh.consignacao.averbar';

// --- Consignatária -------------------------------------------------------------

export const TIPOS_CONSIGNATARIA: SelectOption[] = [
  { value: 'InstituicaoFinanceira', label: 'Instituição financeira' },
  { value: 'EntidadeClassista', label: 'Entidade classista' },
  { value: 'Seguradora', label: 'Seguradora' },
  { value: 'Outra', label: 'Outra' },
];

export function formatarTipoConsignataria(tipo: TipoConsignataria | string): string {
  return TIPOS_CONSIGNATARIA.find((t) => t.value === tipo)?.label ?? tipo;
}

export function situacaoConsignatariaTagVariant(
  situacao: SituacaoConsignataria | string,
): TagVariant {
  return situacao === 'Ativa' ? 'success' : 'warning';
}

/** Formata um CNPJ de 14 dígitos (NN.NNN.NNN/NNNN-NN). Devolve cru se não tiver 14. */
export function formatarCnpj(cnpj: string): string {
  const d = cnpj.replace(/\D/g, '');
  if (d.length !== 14) return cnpj;
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
}

// --- Rubrica / margem ----------------------------------------------------------

export const CATEGORIAS_CONSIGNAVEL: SelectOption[] = [
  { value: 'Obrigatoria', label: 'Obrigatória (prioridade máxima)' },
  { value: 'Facultativa', label: 'Facultativa' },
  { value: 'Beneficio', label: 'Benefício' },
];

export function formatarCategoria(categoria: CategoriaConsignavel | string): string {
  switch (categoria) {
    case 'Obrigatoria':
      return 'Obrigatória';
    case 'Facultativa':
      return 'Facultativa';
    case 'Beneficio':
      return 'Benefício';
    default:
      return String(categoria);
  }
}

export const GRUPOS_MARGEM: SelectOption[] = [
  { value: 'Geral', label: 'Geral (35%)' },
  { value: 'CartaoConsignado', label: 'Cartão consignado (5%)' },
  { value: 'CartaoBeneficio', label: 'Cartão benefício (5%)' },
];

export function formatarGrupoMargem(grupo: GrupoMargem | string): string {
  switch (grupo) {
    case 'Geral':
      return 'Margem geral';
    case 'CartaoConsignado':
      return 'Cartão consignado';
    case 'CartaoBeneficio':
      return 'Cartão benefício';
    default:
      return String(grupo);
  }
}

// --- Contrato ------------------------------------------------------------------

export function formatarSituacaoConsignacao(situacao: SituacaoConsignacao | string): string {
  return String(situacao);
}

export function situacaoConsignacaoTagVariant(situacao: SituacaoConsignacao | string): TagVariant {
  switch (situacao) {
    case 'Averbada':
      return 'success';
    case 'Suspensa':
      return 'warning';
    case 'Quitada':
      return 'info';
    case 'Cancelada':
      return 'danger';
    default:
      return 'info';
  }
}
