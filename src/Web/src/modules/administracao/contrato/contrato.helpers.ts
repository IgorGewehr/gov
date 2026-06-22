// Helpers de apresentação compartilhados pelas telas de Contrato.
import type { TagVariant } from '../../../components/ui';
import type { SituacaoContrato } from './contrato.api';

/** Mapeia a situação do contrato para a variante semântica da Tag (cor + texto). */
export function situacaoTagVariant(situacao: SituacaoContrato): TagVariant {
  switch (situacao) {
    case 'Assinado':
      return 'warning';
    case 'Eficaz':
      return 'info';
    case 'EmExecucao':
      return 'success';
    case 'Encerrado':
      return 'default';
    case 'Rescindido':
      return 'danger';
    default:
      return 'default';
  }
}
