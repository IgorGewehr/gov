// Helpers de apresentação compartilhados pelas telas de RecursosHumanos
// (espelha src/modules/tributos/dividaAtiva.helpers.ts).
import type { TagVariant, SelectOption } from '../../components/ui';

/** Permissão de LEITURA do módulo (gating de UI; backend é a fonte da verdade). */
export const PERM_RH_VER = 'recursoshumanos.ver';

/** Permissão de AÇÃO (criar/editar/comandos) do módulo. */
export const PERM_RH_GERENCIAR = 'recursoshumanos.gerenciar';

/** Mapeia a situação do servidor para a variante semântica da Tag (cor + texto). */
export function situacaoServidorTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Estavel':
    case 'EmExercicio':
      return 'success';
    case 'Empossado':
      return 'info';
    case 'Nomeado':
      return 'warning';
    case 'Afastado':
      return 'info';
    case 'Desligado':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação do cargo para a variante semântica da Tag. */
export function situacaoCargoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Ativo':
      return 'success';
    case 'Vago':
      return 'warning';
    case 'Extinto':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação da folha para a variante semântica da Tag. */
export function situacaoFolhaTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aberta':
      return 'warning';
    case 'Calculada':
      return 'info';
    case 'Fechada':
      return 'info';
    case 'Paga':
      return 'success';
    default:
      return 'default';
  }
}

/** Opções de tipo de cargo (rótulo PT-BR → enum numérico do backend). */
export const TIPOS_CARGO: SelectOption[] = [
  { value: '1', label: 'Efetivo' },
  { value: '2', label: 'Comissionado' },
  { value: '3', label: 'Temporário' },
];

/** Opções de regime previdenciário (rótulo → enum numérico do backend). */
export const REGIMES_PREVIDENCIARIOS: SelectOption[] = [
  { value: '1', label: 'RPPS (Regime Próprio)' },
  { value: '2', label: 'RGPS (Regime Geral)' },
];

/** Opções de tipo de evento da folha (rótulo → enum numérico do backend). */
export const TIPOS_EVENTO: SelectOption[] = [
  { value: '1', label: 'Provento' },
  { value: '2', label: 'Desconto' },
];

/** Opções de mês (1–12) para seletor de competência. */
export const MESES: SelectOption[] = [
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
