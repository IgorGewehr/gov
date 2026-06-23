// Helpers de apresentação do Painel do Gestor: formatação de frações e mapeamento
// do semáforo (SituacaoLimite / mínimos atingidos) para as cores gov.br. A COR é
// STATUS (não decoração): dirige o alerta de conformidade exibido ao gestor.
import type { TagVariant } from '../../components/ui';
import { SituacaoLimite } from './api';

/** Formata uma fração 0..1 como percentual pt-BR com 2 casas (ex.: 0,2531 -> "25,31%"). */
export function formatarFracao(fracao: number): string {
  return `${(fracao * 100).toLocaleString('pt-BR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}%`;
}

/** Moeda compacta para a faixa-resumo: "R$ 12,3 mi", "R$ 4,8 mil", "R$ 980". */
export function formatarMoedaCompacta(valor: number): string {
  const abs = Math.abs(valor);
  if (abs >= 1_000_000) return `R$ ${(valor / 1_000_000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} mi`;
  if (abs >= 1_000) return `R$ ${(valor / 1_000).toLocaleString('pt-BR', { maximumFractionDigits: 0 })} mil`;
  return `R$ ${valor.toLocaleString('pt-BR', { maximumFractionDigits: 0 })}`;
}

/** Exercício corrente (ano), usado como padrão do seletor. */
export function exercicioAtual(): number {
  return new Date().getFullYear();
}

/** Variante de Tag (cor gov.br) para o semáforo LRF/TCE. */
export function variantePorSituacao(situacao: SituacaoLimite): TagVariant {
  switch (situacao) {
    case SituacaoLimite.Adequado:
      return 'success';
    case SituacaoLimite.Alerta:
      return 'warning';
    case SituacaoLimite.Excedido:
      return 'danger';
    default:
      return 'default';
  }
}

/** Rótulo PT-BR do semáforo LRF (limite de teto: quanto maior, pior). */
export function rotuloSituacaoLrf(situacao: SituacaoLimite): string {
  switch (situacao) {
    case SituacaoLimite.Adequado:
      return 'Dentro do limite';
    case SituacaoLimite.Alerta:
      return 'Faixa de alerta/prudencial';
    case SituacaoLimite.Excedido:
      return 'Limite legal excedido';
    default:
      return 'Indeterminado (sem dados)';
  }
}

/** Rótulo PT-BR do semáforo de prestação de contas (remessas TCE-RS). */
export function rotuloSituacaoTce(situacao: SituacaoLimite): string {
  switch (situacao) {
    case SituacaoLimite.Adequado:
      return 'Em dia';
    case SituacaoLimite.Alerta:
      return 'Atenção a prazos';
    case SituacaoLimite.Excedido:
      return 'Remessas vencidas';
    default:
      return 'Indeterminado (sem dados)';
  }
}

/** Nome amigável do setor de um mínimo constitucional. */
export function rotuloSetor(setor: string): string {
  const mapa: Record<string, string> = {
    Saude: 'Saúde',
    Educacao: 'Educação',
  };
  return mapa[setor] ?? setor;
}

/** True quando a string de situação de um mínimo indica "Atingido". */
export function minimoAtingido(situacao: string): boolean {
  return situacao === 'Atingido';
}

/** Mínimo vigente padrão por setor (// parametrizável por tenant/exercício no backend). */
export function fundamentoMinimo(setor: string): string {
  const mapa: Record<string, string> = {
    Saude: 'Mínimo de 15% da receita (EC 29/2000; LC 141/2012).',
    Educacao: 'Mínimo de 25% da receita em MDE (CF art. 212).',
  };
  return mapa[setor] ?? 'Mínimo constitucional vigente.';
}
