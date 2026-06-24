// Helpers de apresentação das OBRAS (W9.3, Lei 14.133/2021): rótulos PT-BR de
// enums, variantes de Tag por situação e o STATUS INDICATIVO do prazo do
// art. 94 §3 (publicação/registro). O backend só EMITE o alerta (varredor
// transversal — VarrerPrazosArt94); aqui calculamos um status de tela a partir
// das datas da ficha (assinatura/conclusão) para sinalizar "a vencer/vencido".
import type { SelectOption, TagVariant } from '../../../components/ui';
import type { SituacaoObraNome } from './obra.api';
import {
  MOTIVO_PARALISACAO,
  REGIME_EXECUCAO,
  SITUACAO_OBRA,
  TIPO_OCORRENCIA,
} from './obra.api';

/** Rótulos PT-BR da situação da obra (nome cru do ToString() do backend). */
const SITUACAO_LABEL: Record<SituacaoObraNome, string> = {
  Planejada: 'Planejada',
  EmExecucao: 'Em execução',
  Paralisada: 'Paralisada',
  Concluida: 'Concluída',
  Incorporada: 'Incorporada',
  Rescindida: 'Rescindida',
};

export function situacaoObraLabel(situacao: SituacaoObraNome): string {
  return SITUACAO_LABEL[situacao] ?? situacao;
}

/** Variante semântica da Tag por situação da obra. */
export function situacaoObraTagVariant(situacao: SituacaoObraNome): TagVariant {
  switch (situacao) {
    case 'Planejada':
      return 'info';
    case 'EmExecucao':
      return 'warning';
    case 'Paralisada':
      return 'danger';
    case 'Concluida':
      return 'success';
    case 'Incorporada':
      return 'success';
    case 'Rescindida':
      return 'default';
    default:
      return 'default';
  }
}

/** Rótulo PT-BR do regime de execução (string ToString() do enum). */
const REGIME_LABEL: Record<string, string> = {
  EmpreitadaPorPrecoUnitario: 'Empreitada por preço unitário (art. 46, II)',
  EmpreitadaPorPrecoGlobal: 'Empreitada por preço global (art. 46, I)',
  Tarefa: 'Tarefa (art. 46, III)',
  EmpreitadaIntegral: 'Empreitada integral (art. 46, IV)',
  ContratacaoIntegrada: 'Contratação integrada (art. 46, V)',
  ContratacaoSemiIntegrada: 'Contratação semi-integrada (art. 46, VI)',
};

export function regimeExecucaoLabel(regime: string): string {
  return REGIME_LABEL[regime] ?? regime;
}

/** Rótulo PT-BR do tipo de ocorrência da fiscalização (art. 117). */
const OCORRENCIA_LABEL: Record<string, string> = {
  Notificacao: 'Notificação',
  Advertencia: 'Advertência',
  RegistroTecnico: 'Registro técnico',
};

export function ocorrenciaTipoLabel(tipo: string): string {
  return OCORRENCIA_LABEL[tipo] ?? tipo;
}

// ---------------------------------------------------------------------------
// Opções de Select
// ---------------------------------------------------------------------------

export const OPCOES_SITUACAO_OBRA: SelectOption[] = [
  { value: String(SITUACAO_OBRA.Planejada), label: 'Planejada' },
  { value: String(SITUACAO_OBRA.EmExecucao), label: 'Em execução' },
  { value: String(SITUACAO_OBRA.Paralisada), label: 'Paralisada' },
  { value: String(SITUACAO_OBRA.Concluida), label: 'Concluída' },
  { value: String(SITUACAO_OBRA.Incorporada), label: 'Incorporada' },
  { value: String(SITUACAO_OBRA.Rescindida), label: 'Rescindida' },
];

export const OPCOES_REGIME_EXECUCAO: SelectOption[] = [
  { value: String(REGIME_EXECUCAO.EmpreitadaPorPrecoUnitario), label: 'Empreitada por preço unitário' },
  { value: String(REGIME_EXECUCAO.EmpreitadaPorPrecoGlobal), label: 'Empreitada por preço global' },
  { value: String(REGIME_EXECUCAO.Tarefa), label: 'Tarefa' },
  { value: String(REGIME_EXECUCAO.EmpreitadaIntegral), label: 'Empreitada integral' },
  { value: String(REGIME_EXECUCAO.ContratacaoIntegrada), label: 'Contratação integrada' },
  { value: String(REGIME_EXECUCAO.ContratacaoSemiIntegrada), label: 'Contratação semi-integrada' },
];

export const OPCOES_TIPO_OCORRENCIA: SelectOption[] = [
  { value: String(TIPO_OCORRENCIA.Notificacao), label: 'Notificação' },
  { value: String(TIPO_OCORRENCIA.Advertencia), label: 'Advertência' },
  { value: String(TIPO_OCORRENCIA.RegistroTecnico), label: 'Registro técnico' },
];

export const OPCOES_MOTIVO_PARALISACAO: SelectOption[] = [
  { value: String(MOTIVO_PARALISACAO.OrdemFiscalizacao), label: 'Ordem da fiscalização' },
  { value: String(MOTIVO_PARALISACAO.FaltaProjeto), label: 'Falta/insuficiência de projeto' },
  { value: String(MOTIVO_PARALISACAO.Clima), label: 'Condição climática adversa' },
  { value: String(MOTIVO_PARALISACAO.Orcamentaria), label: 'Restrição orçamentária/financeira' },
  { value: String(MOTIVO_PARALISACAO.Judicial), label: 'Determinação judicial' },
];

// ---------------------------------------------------------------------------
// Prazo art. 94 §3 (status INDICATIVO de tela)
// ---------------------------------------------------------------------------

/** Prazos legais padrão do art. 94 §3 (dias ÚTEIS). Parametrizáveis no backend. */
const PRAZO_ASSINATURA_DIAS_UTEIS = 25;
const PRAZO_CONCLUSAO_DIAS_UTEIS = 45;

export type StatusPrazo = 'aVencer' | 'vencido' | 'naoAplicavel';

export interface PrazoArt94 {
  /** Tipo do relógio aplicável (assinatura 25 d.u. ou conclusão 45 d.u.). */
  tipo: 'Assinatura25' | 'Conclusao45';
  /** Data-base (assinatura ou conclusão). */
  base: string;
  /** Vencimento estimado (data-base + N dias úteis). */
  vencimento: string;
  /** Dias úteis restantes (negativo = vencido). */
  diasUteisRestantes: number;
  status: StatusPrazo;
}

/** Soma `dias` dias úteis (seg–sex) a uma data ISO yyyy-mm-dd. */
function somarDiasUteis(baseIso: string, dias: number): Date {
  const data = new Date(`${baseIso}T00:00:00`);
  let restantes = dias;
  while (restantes > 0) {
    data.setDate(data.getDate() + 1);
    const dow = data.getDay();
    if (dow !== 0 && dow !== 6) {
      restantes -= 1;
    }
  }
  return data;
}

/** Conta dias úteis (seg–sex) entre `de` (exclusive) e `ate` (inclusive); negativo se `ate < de`. */
function contarDiasUteis(de: Date, ate: Date): number {
  const sinal = ate.getTime() >= de.getTime() ? 1 : -1;
  const inicio = sinal === 1 ? de : ate;
  const fim = sinal === 1 ? ate : de;
  let conta = 0;
  const cursor = new Date(inicio);
  while (cursor.getTime() < fim.getTime()) {
    cursor.setDate(cursor.getDate() + 1);
    const dow = cursor.getDay();
    if (dow !== 0 && dow !== 6) {
      conta += 1;
    }
  }
  return conta * sinal;
}

/**
 * Calcula o status INDICATIVO do prazo do art. 94 §3 a partir da ficha:
 * em execução -> relógio da assinatura (25 d.u.); concluída -> relógio da
 * conclusão (45 d.u.). Aproximação por dias úteis sem feriados (o cálculo
 * OFICIAL é do varredor transversal no backend, com calendário do tenant).
 */
export function calcularPrazoArt94(
  situacao: SituacaoObraNome,
  dataAssinaturaContrato: string,
  dataConclusao: string | null | undefined,
): PrazoArt94 | null {
  const concluida = situacao === 'Concluida' || situacao === 'Incorporada';
  if (concluida && dataConclusao) {
    return montar('Conclusao45', dataConclusao, PRAZO_CONCLUSAO_DIAS_UTEIS);
  }
  if (situacao === 'EmExecucao' || situacao === 'Planejada' || situacao === 'Paralisada') {
    return montar('Assinatura25', dataAssinaturaContrato, PRAZO_ASSINATURA_DIAS_UTEIS);
  }
  return null;
}

function montar(
  tipo: PrazoArt94['tipo'],
  base: string,
  diasUteis: number,
): PrazoArt94 {
  const vencimento = somarDiasUteis(base, diasUteis);
  const hoje = new Date(new Date().toISOString().slice(0, 10) + 'T00:00:00');
  const restantes = contarDiasUteis(hoje, vencimento);
  return {
    tipo,
    base,
    vencimento: vencimento.toISOString().slice(0, 10),
    diasUteisRestantes: restantes,
    status: restantes < 0 ? 'vencido' : 'aVencer',
  };
}

// ---------------------------------------------------------------------------
// Utilitários compartilhados
// ---------------------------------------------------------------------------

/** Data de hoje em ISO yyyy-mm-dd (default de inputs date). */
export function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}
