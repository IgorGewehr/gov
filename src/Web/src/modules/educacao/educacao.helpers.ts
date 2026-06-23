// Helpers de apresentação compartilhados pelas telas do módulo Educação.
import type { SelectOption, TagVariant } from '../../components/ui';

// --- Opções de enums (valores numéricos esperados pelo backend — IsInEnum) ---

/** Sexo do aluno (EducaCenso). value = código numérico do enum Domain.Alunos.Sexo. */
export const opcoesSexo: SelectOption[] = [
  { value: '1', label: 'Feminino' },
  { value: '2', label: 'Masculino' },
  { value: '9', label: 'Não informado' },
];

/** Grau de parentesco do responsável (Domain.Alunos.Parentesco). */
export const opcoesParentesco: SelectOption[] = [
  { value: '1', label: 'Mãe' },
  { value: '2', label: 'Pai' },
  { value: '3', label: 'Avô/Avó' },
  { value: '4', label: 'Tutor/Guardião' },
  { value: '9', label: 'Outro' },
];

/** Situação do cadastro do aluno (filtro da busca). */
export const opcoesSituacaoAluno: SelectOption[] = [
  { value: '1', label: 'Ativo' },
  { value: '2', label: 'Transferido' },
  { value: '3', label: 'Inativo' },
];

/** Etapa/modalidade de ensino da turma (Domain.Turmas.Etapa). */
export const opcoesEtapa: SelectOption[] = [
  { value: '1', label: 'Educação Infantil' },
  { value: '2', label: 'Fundamental — anos iniciais' },
  { value: '3', label: 'Fundamental — anos finais' },
  { value: '4', label: 'Ensino Médio' },
  { value: '5', label: 'EJA' },
  { value: '6', label: 'Educação Especial' },
];

/** Turno de funcionamento da turma (Domain.Turmas.Turno). */
export const opcoesTurno: SelectOption[] = [
  { value: '1', label: 'Matutino' },
  { value: '2', label: 'Vespertino' },
  { value: '3', label: 'Noturno' },
  { value: '4', label: 'Integral' },
];

/** Situação da turma (filtro da busca). */
export const opcoesSituacaoTurma: SelectOption[] = [
  { value: '1', label: 'Planejada' },
  { value: '2', label: 'Aberta' },
  { value: '3', label: 'Encerrada' },
];

/** Mapeia a situação do aluno para a variante semântica da Tag. */
export function situacaoAlunoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Ativo':
      return 'success';
    case 'Transferido':
      return 'warning';
    case 'Inativo':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação da turma para a variante semântica da Tag. */
export function situacaoTurmaTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aberta':
      return 'success';
    case 'Planejada':
      return 'info';
    case 'Encerrada':
      return 'danger';
    default:
      return 'default';
  }
}

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
