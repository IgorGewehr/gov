// Helpers de apresentação compartilhados pelas telas do módulo Educação.
import type { TagVariant } from '../../components/ui';

/** Mapeia a situação da escola para a variante semântica da Tag (cor + texto). */
export function situacaoEscolaTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Credenciada':
      return 'success';
    case 'EmCadastro':
      return 'info';
    case 'Desativada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação da matrícula para a variante semântica da Tag. */
export function situacaoMatriculaTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Ativa':
      return 'success';
    case 'Concluida':
      return 'info';
    case 'Transferida':
      return 'warning';
    case 'Abandono':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação do diário para a variante semântica da Tag. */
export function situacaoDiarioTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aberto':
      return 'info';
    case 'Apurado':
      return 'success';
    default:
      return 'default';
  }
}

/** Mapeia o resultado apurado do aluno para a variante semântica da Tag. */
export function resultadoAlunoTagVariant(resultado: string | null): TagVariant {
  switch (resultado) {
    case 'Aprovado':
      return 'success';
    case 'Reprovado':
    case 'ReprovadoPorFrequencia':
      return 'danger';
    default:
      return 'default';
  }
}

/** Formata percentual (0–100) em pt-BR com uma casa decimal. */
export function formatarPercentual(valor: number): string {
  return `${valor.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}
