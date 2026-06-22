// Helpers de apresentação das telas de Contabilidade (PCASP).
import type { SelectOption, TagVariant } from '../../../components/ui';
import { LADO_PARTIDA } from './contabilidade.api';
import type { ContaContabil, LinhaBalancete } from './contabilidade.api';

/** Tolerância para comparação de igualdade de débitos/créditos (centavos). */
const TOLERANCIA_FECHAMENTO = 0.005;

/** Opções de mês (1-12) para o seletor do balancete. */
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

/** Opções de lado da partida para <Select> (value = número do enum). */
export const LADOS_PARTIDA: SelectOption[] = [
  { value: String(LADO_PARTIDA.Debito), label: 'Débito' },
  { value: String(LADO_PARTIDA.Credito), label: 'Crédito' },
];

/** true se a conta é analítica (folha do plano, lançável). */
export function contaEhAnalitica(conta: ContaContabil): boolean {
  return conta.tipo.toLowerCase().startsWith('analit');
}

/** Variante de Tag para o tipo da conta (sintética x analítica). */
export function tipoContaTagVariant(tipo: string): TagVariant {
  return tipo.toLowerCase().startsWith('analit') ? 'info' : 'default';
}

/** Soma dos débitos das linhas (balancete/razão). */
export function totalDebitos(linhas: LinhaBalancete[] | undefined): number {
  return (linhas ?? []).reduce((acc, l) => acc + l.totalDebitos, 0);
}

/** Soma dos créditos das linhas (balancete/razão). */
export function totalCreditos(linhas: LinhaBalancete[] | undefined): number {
  return (linhas ?? []).reduce((acc, l) => acc + l.totalCreditos, 0);
}

/**
 * Indica se o balancete está desbalanceado (ΣDébitos ≠ ΣCréditos). Em partidas
 * dobradas o total de débitos deve igualar o de créditos; divergência sinaliza erro.
 */
export function balanceteDesbalanceado(linhas: LinhaBalancete[] | undefined): boolean {
  if (!linhas || linhas.length === 0) return false;
  return Math.abs(totalDebitos(linhas) - totalCreditos(linhas)) > TOLERANCIA_FECHAMENTO;
}

/** Indentação visual do código por nível hierárquico do plano de contas. */
export function recuoPorNivel(nivel: number): string {
  return `${Math.max(0, nivel - 1) * 1.25}rem`;
}
