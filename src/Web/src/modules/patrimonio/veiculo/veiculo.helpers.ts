// Helpers de apresentação compartilhados pelas telas de Veiculo (frota).
import type { TagVariant } from '../../../components/ui';
import type { SituacaoVeiculo } from './veiculo.api';

/** Rótulo legível (PT-BR) da situação do veículo. */
export function situacaoVeiculoLabel(situacao: SituacaoVeiculo): string {
  switch (situacao) {
    case 'EmIncorporacao':
      return 'Em incorporação';
    case 'Tombado':
      return 'Tombado';
    case 'Cedido':
      return 'Cedido';
    case 'Baixada':
      return 'Baixado';
    case 'Alienada':
      return 'Alienado';
    default:
      return situacao;
  }
}

/** Mapeia a situação do veículo para a variante semântica da Tag (cor). */
export function situacaoVeiculoTagVariant(situacao: SituacaoVeiculo): TagVariant {
  switch (situacao) {
    case 'Tombado':
      return 'success';
    case 'Cedido':
      return 'info';
    case 'EmIncorporacao':
      return 'warning';
    case 'Baixada':
    case 'Alienada':
      return 'danger';
    default:
      return 'default';
  }
}

/**
 * Operações de frota só são permitidas para veículo ATIVO NO ACERVO (I-5):
 * Situacao ∈ { Tombado, Cedido }. Usado para habilitar/desabilitar ações na DetailPage.
 */
export function veiculoAtivoNoAcervo(situacao: SituacaoVeiculo): boolean {
  return situacao === 'Tombado' || situacao === 'Cedido';
}

/** Data de hoje em formato ISO (yyyy-mm-dd), para defaults e validação de CNH (I-9). */
export function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}
