// Helpers compartilhados do modulo Convenios (dois fluxos). Centralizam rotulos
// PT-BR (linguagem cidada), variantes de Tag (semaforo de situacao) e a logica do
// SEMAFORO DE PRAZO de analise das prestacoes de contas. Sem dependencia de UI.
import type { TagVariant } from '../../components/ui';

// ---------------------------------------------------------------------------
// Fluxo A — Convenios federais RECEBIDOS (SituacaoConvenioRecebido)
// ---------------------------------------------------------------------------

/** Maquina de estados do convenio recebido — projetada por ToString() no backend. */
export type SituacaoConvenio =
  | 'EmProposta'
  | 'Celebrado'
  | 'EmExecucao'
  | 'EmPrestacaoContas'
  | 'EmAnalise'
  | 'Aprovado'
  | 'AprovadoComRessalva'
  | 'Rejeitado'
  | 'Inadimplente';

const SITUACAO_CONVENIO_LABEL: Record<SituacaoConvenio, string> = {
  EmProposta: 'Em proposta',
  Celebrado: 'Celebrado',
  EmExecucao: 'Em execução',
  EmPrestacaoContas: 'Em prestação de contas',
  EmAnalise: 'Em análise',
  Aprovado: 'Aprovado',
  AprovadoComRessalva: 'Aprovado com ressalva',
  Rejeitado: 'Rejeitado',
  Inadimplente: 'Inadimplente',
};

const SITUACAO_CONVENIO_TAG: Record<SituacaoConvenio, TagVariant> = {
  EmProposta: 'default',
  Celebrado: 'info',
  EmExecucao: 'info',
  EmPrestacaoContas: 'warning',
  EmAnalise: 'warning',
  Aprovado: 'success',
  AprovadoComRessalva: 'success',
  Rejeitado: 'danger',
  Inadimplente: 'danger',
};

export function situacaoConvenioLabel(s: SituacaoConvenio): string {
  return SITUACAO_CONVENIO_LABEL[s] ?? s;
}

export function situacaoConvenioTag(s: SituacaoConvenio): TagVariant {
  return SITUACAO_CONVENIO_TAG[s] ?? 'default';
}

// ---------------------------------------------------------------------------
// Fluxo B — Parcerias OSC / MROSC (SituacaoParceriaOsc)
// ---------------------------------------------------------------------------

/** Maquina de estados da parceria OSC — projetada por ToString() no backend. */
export type SituacaoParceria =
  | 'EmSelecao'
  | 'Celebrada'
  | 'EmExecucao'
  | 'EmPrestacaoContas'
  | 'EmAnalise'
  | 'Aprovada'
  | 'AprovadaComRessalva'
  | 'Rejeitada'
  | 'Inadimplente';

const SITUACAO_PARCERIA_LABEL: Record<SituacaoParceria, string> = {
  EmSelecao: 'Em seleção',
  Celebrada: 'Celebrada',
  EmExecucao: 'Em execução',
  EmPrestacaoContas: 'Em prestação de contas',
  EmAnalise: 'Em análise',
  Aprovada: 'Aprovada',
  AprovadaComRessalva: 'Aprovada com ressalva',
  Rejeitada: 'Rejeitada',
  Inadimplente: 'Inadimplente',
};

const SITUACAO_PARCERIA_TAG: Record<SituacaoParceria, TagVariant> = {
  EmSelecao: 'default',
  Celebrada: 'info',
  EmExecucao: 'info',
  EmPrestacaoContas: 'warning',
  EmAnalise: 'warning',
  Aprovada: 'success',
  AprovadaComRessalva: 'success',
  Rejeitada: 'danger',
  Inadimplente: 'danger',
};

export function situacaoParceriaLabel(s: SituacaoParceria): string {
  return SITUACAO_PARCERIA_LABEL[s] ?? s;
}

export function situacaoParceriaTag(s: SituacaoParceria): TagVariant {
  return SITUACAO_PARCERIA_TAG[s] ?? 'default';
}

/**
 * Inadimplencia BLOQUEIA novos repasses (B-INV-9 — gatilho LRF art. 48). Usado para
 * destacar, na UI, o estado de bloqueio da parceria.
 */
export function parceriaBloqueada(s: SituacaoParceria): boolean {
  return s === 'Inadimplente';
}

// ---------------------------------------------------------------------------
// Semaforo de PRAZO de analise da prestacao de contas (ambos os fluxos)
// ---------------------------------------------------------------------------

/** Cor do semaforo de prazo: vencido (danger), proximo (warning), no prazo (success). */
export type SemaforoPrazo = 'vencido' | 'proximo' | 'noPrazo';

const DIAS_ALERTA_PRAZO = 30;

/**
 * Classifica o prazo de analise (ISO yyyy-mm-dd) em relacao a hoje:
 * vencido (< hoje), proximo (<= 30 dias) ou no prazo. Null quando nao ha prazo.
 */
export function semaforoPrazo(prazoIso: string | null | undefined): SemaforoPrazo | null {
  if (!prazoIso) return null;
  const prazo = new Date(`${prazoIso}T00:00:00`);
  if (Number.isNaN(prazo.getTime())) return null;
  const hoje = new Date();
  hoje.setHours(0, 0, 0, 0);
  const dias = Math.round((prazo.getTime() - hoje.getTime()) / 86_400_000);
  if (dias < 0) return 'vencido';
  if (dias <= DIAS_ALERTA_PRAZO) return 'proximo';
  return 'noPrazo';
}

const SEMAFORO_TAG: Record<SemaforoPrazo, TagVariant> = {
  vencido: 'danger',
  proximo: 'warning',
  noPrazo: 'success',
};

const SEMAFORO_LABEL: Record<SemaforoPrazo, string> = {
  vencido: 'Prazo vencido',
  proximo: 'Prazo próximo',
  noPrazo: 'No prazo',
};

export function semaforoTag(s: SemaforoPrazo): TagVariant {
  return SEMAFORO_TAG[s];
}

export function semaforoLabel(s: SemaforoPrazo): string {
  return SEMAFORO_LABEL[s];
}
