// Query keys centralizadas do módulo Educação (fonte única para invalidação
// consistente entre os arquivos de API por agregado: Escola, Matrícula, Diário).
export const educacaoKeys = {
  all: ['educacao'] as const,
  escolas: () => [...educacaoKeys.all, 'escolas'] as const,
  escolaPorInep: (codigoInep: string) => [...educacaoKeys.escolas(), 'inep', codigoInep] as const,
  matriculas: () => [...educacaoKeys.all, 'matriculas'] as const,
  matriculasPorAluno: (alunoId: string) => [...educacaoKeys.matriculas(), 'aluno', alunoId] as const,
  matriculasDaTurma: (turmaId: string, dataReferencia: string) =>
    [...educacaoKeys.matriculas(), 'turma', turmaId, dataReferencia] as const,
  diarioDaMatricula: (matriculaId: string) =>
    [...educacaoKeys.all, 'diarios', 'matricula', matriculaId] as const,
  frequenciaDoDiario: (diarioId: string) =>
    [...educacaoKeys.all, 'diarios', diarioId, 'frequencia'] as const,
};

/** Os endpoints de criação retornam `{ id }` (Results.Ok(new { id })). */
export interface CriadoResponse {
  id: string;
}
