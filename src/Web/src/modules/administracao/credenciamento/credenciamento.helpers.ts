// Helpers de apresentação compartilhados pelas telas de Credenciamento (art. 78, I e art. 79).
// Rótulos em PT-BR (linguagem cidadã) e mapeamento de status -> variante de Tag.
import type { TagVariant, SelectOption } from '../../../components/ui';
import type {
  HipoteseCredenciamento,
  SituacaoCredenciado,
  SituacaoCredenciamento,
} from './credenciamento.api';

/** Rótulo legível da hipótese autorizadora (art. 79, I a III). */
export const HIPOTESE_LABEL: Record<HipoteseCredenciamento, string> = {
  ParalelaNaoExcludente: 'Contratação paralela e não excludente (art. 79, I)',
  SelecaoCriterioBeneficiario: 'Seleção a critério do beneficiário (art. 79, II)',
  MercadosFluidos: 'Mercados fluidos (art. 79, III)',
};

/** Rótulo legível da situação do edital. */
export const SITUACAO_LABEL: Record<SituacaoCredenciamento, string> = {
  EmElaboracao: 'Em elaboração',
  ChamamentoAberto: 'Chamamento aberto',
  Suspenso: 'Suspenso',
  Encerrado: 'Encerrado',
  Anulado: 'Anulado',
  Revogado: 'Revogado',
};

/** Rótulo legível da situação da inscrição. */
export const SITUACAO_CREDENCIADO_LABEL: Record<SituacaoCredenciado, string> = {
  EmAnalise: 'Em análise',
  Credenciado: 'Credenciado',
  Indeferido: 'Indeferido',
  Suspenso: 'Suspenso',
  Descredenciado: 'Descredenciado',
};

/** Mapeia a situação do edital para a variante semântica da Tag (cor). */
export function situacaoTagVariant(situacao: SituacaoCredenciamento): TagVariant {
  switch (situacao) {
    case 'EmElaboracao':
      return 'info';
    case 'ChamamentoAberto':
      return 'success';
    case 'Suspenso':
      return 'warning';
    case 'Encerrado':
    case 'Anulado':
    case 'Revogado':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação da inscrição para a variante da Tag. */
export function credenciadoTagVariant(situacao: SituacaoCredenciado): TagVariant {
  switch (situacao) {
    case 'Credenciado':
      return 'success';
    case 'EmAnalise':
      return 'info';
    case 'Suspenso':
      return 'warning';
    case 'Indeferido':
    case 'Descredenciado':
      return 'danger';
    default:
      return 'default';
  }
}

// Opções dos selects (ordem = ordem dos enums no domínio).
export const HIPOTESE_OPTIONS: SelectOption[] = (
  ['ParalelaNaoExcludente', 'SelecaoCriterioBeneficiario', 'MercadosFluidos'] as const
).map((h) => ({ value: h, label: HIPOTESE_LABEL[h] }));

// Filtro da lista: "Todas" + cada situação.
export const FILTRO_SITUACAO_OPTIONS: SelectOption[] = [
  { value: 'Todas', label: 'Todas as situações' },
  ...(
    ['EmElaboracao', 'ChamamentoAberto', 'Suspenso', 'Encerrado', 'Anulado', 'Revogado'] as const
  ).map((s) => ({ value: s, label: SITUACAO_LABEL[s] })),
];
