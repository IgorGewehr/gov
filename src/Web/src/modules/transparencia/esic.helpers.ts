// Helpers de apresentação do e-SIC interno (LAI). Rótulos PT-BR, variantes de Tag
// (semáforo de situação) e o cálculo do PRAZO LEGAL em dias úteis restantes.
import type { SelectOption, TagVariant } from '../../components/ui';
import type { ResultadoRecurso, SituacaoPedidoSic } from './esic.api';

/** Rótulo legível (PT-BR) da situação do pedido. */
export const situacaoPedidoLabel: Record<SituacaoPedidoSic, string> = {
  Aberto: 'Aberto',
  EmAtendimento: 'Em atendimento',
  Respondido: 'Respondido',
  Indeferido: 'Indeferido',
  RecursoAberto: 'Recurso aberto',
  RecursoRespondido: 'Recurso respondido',
  Encerrado: 'Encerrado',
};

/** Mapeia a situação do pedido para a variante semântica da Tag (semáforo). */
export function situacaoPedidoTagVariant(situacao: SituacaoPedidoSic): TagVariant {
  switch (situacao) {
    case 'Aberto':
      return 'info';
    case 'EmAtendimento':
      return 'warning';
    case 'Respondido':
      return 'success';
    case 'Indeferido':
      return 'danger';
    case 'RecursoAberto':
      return 'warning';
    case 'RecursoRespondido':
      return 'info';
    case 'Encerrado':
      return 'default';
    default:
      return 'default';
  }
}

export const situacaoPedidoOptions: SelectOption[] = (
  [
    'Aberto',
    'EmAtendimento',
    'Respondido',
    'Indeferido',
    'RecursoAberto',
    'RecursoRespondido',
    'Encerrado',
  ] as const
).map((value) => ({ value, label: situacaoPedidoLabel[value] }));

/** Rótulo legível (PT-BR) do resultado de um recurso (LAI art. 15-16). */
export const resultadoRecursoLabel: Record<ResultadoRecurso, string> = {
  Provido: 'Provido (acesso concedido)',
  ParcialmenteProvido: 'Parcialmente provido',
  Negado: 'Negado',
};

export const resultadoRecursoOptions: SelectOption[] = (
  ['Provido', 'ParcialmenteProvido', 'Negado'] as const
).map((value) => ({ value, label: resultadoRecursoLabel[value] }));

/**
 * Situações em que o pedido ainda está EM CURSO (o prazo legal corre). Fora delas
 * (Respondido/Indeferido/RecursoRespondido/Encerrado) o prazo não é mais cobrado.
 */
const SITUACOES_EM_CURSO: ReadonlySet<SituacaoPedidoSic> = new Set([
  'Aberto',
  'EmAtendimento',
  'RecursoAberto',
]);

/** True se a situação ainda tem prazo legal corrente (pedido em curso). */
export function pedidoEmCurso(situacao: SituacaoPedidoSic): boolean {
  return SITUACOES_EM_CURSO.has(situacao);
}

/**
 * Conta dias ÚTEIS (seg–sex) entre `de` (exclusivo) e `ate` (inclusivo). Não
 * considera feriados — o backend (ICalendarioDiasUteis) é a fonte da verdade do
 * prazo; aqui é uma aproximação para o INDICADOR visual de prazo restante.
 * Retorna negativo quando `ate` já passou (atraso, em dias úteis).
 */
export function diasUteisEntre(de: Date, ate: Date): number {
  const inicio = new Date(de.getFullYear(), de.getMonth(), de.getDate());
  const fim = new Date(ate.getFullYear(), ate.getMonth(), ate.getDate());
  const passo = fim >= inicio ? 1 : -1;
  let dias = 0;
  const cursor = new Date(inicio);
  while (cursor.getTime() !== fim.getTime()) {
    cursor.setDate(cursor.getDate() + passo);
    const diaSemana = cursor.getDay();
    if (diaSemana !== 0 && diaSemana !== 6) dias += passo;
  }
  return dias;
}

/** Resultado do cálculo de prazo legal (dias úteis restantes até o prazo vigente). */
export interface PrazoLegal {
  /** Dias úteis restantes (negativo = atraso). */
  diasUteisRestantes: number;
  /** True se o prazo já venceu sem desfecho. */
  vencido: boolean;
  /** Texto pronto para exibição (ex.: "3 dias úteis restantes" / "Vencido há 2 dias úteis"). */
  rotulo: string;
}

/**
 * Calcula o prazo legal restante (em dias úteis) de um pedido EM CURSO, ancorado
 * em `hoje` (default: data corrente). Para pedidos já desfechados, retorna null —
 * o prazo não é mais cobrado.
 */
export function calcularPrazoLegal(
  situacao: SituacaoPedidoSic,
  prazoVigenteIso: string,
  hoje: Date = new Date(),
): PrazoLegal | null {
  if (!pedidoEmCurso(situacao)) return null;
  const prazo = new Date(prazoVigenteIso);
  if (Number.isNaN(prazo.getTime())) return null;

  const dias = diasUteisEntre(hoje, prazo);
  const vencido = dias < 0;
  const abs = Math.abs(dias);
  const plural = abs === 1 ? 'dia útil' : 'dias úteis';
  const rotulo = vencido
    ? `Vencido há ${abs} ${plural}`
    : dias === 0
      ? 'Vence hoje'
      : `${dias} ${plural} restantes`;
  return { diasUteisRestantes: dias, vencido, rotulo };
}

/** Tom semântico (Tag) do prazo legal: perigo se vencido, alerta se <= 3 dias, senão sucesso. */
export function prazoLegalTagVariant(prazo: PrazoLegal): TagVariant {
  if (prazo.vencido) return 'danger';
  if (prazo.diasUteisRestantes <= 3) return 'warning';
  return 'success';
}
