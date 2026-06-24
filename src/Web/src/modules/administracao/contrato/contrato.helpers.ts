// Helpers de apresentação compartilhados pelas telas de Contrato.
import type { TagVariant } from '../../../components/ui';
import type { ContratoDetalhe, SituacaoContrato } from './contrato.api';

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

/** Situação de divulgação do contrato no PNCP (art. 94 — condição de eficácia). */
export interface PncpStatus {
  /** Já divulgado no PNCP (número de controle atribuído). */
  publicado: boolean;
  /** Rótulo curto para badge. */
  rotulo: 'Publicado' | 'Pendente';
  /** Variante semântica do badge (verde = publicado; âmbar = pendente). */
  variant: TagVariant;
  /** Número de controle PNCP, quando publicado. */
  numeroControle: string | null;
}

/**
 * Deriva o status PNCP do contrato a partir dos campos REAIS de ContratoDetalhe
 * (publicadoNoPncp + numeroContratoPncp). O prazo do art. 94 (data-limite) não é exposto pelo
 * endpoint de detalhe; a tela de detalhe apenas distingue Publicado × Pendente. A varredura de
 * prazos a vencer/vencidos vive no endpoint /contratos/prazos-pncp/varrer (alertas ao Portal do Gestor).
 */
export function pncpStatus(contrato: ContratoDetalhe): PncpStatus {
  const publicado = contrato.publicadoNoPncp;
  return {
    publicado,
    rotulo: publicado ? 'Publicado' : 'Pendente',
    variant: publicado ? 'success' : 'warning',
    numeroControle: contrato.numeroContratoPncp,
  };
}
