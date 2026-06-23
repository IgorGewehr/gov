// Query keys centralizadas do módulo Educação (fonte única para invalidação
// consistente entre os arquivos de API por agregado: Escola, Matrícula, Diário).
export const educacaoKeys = {
  all: ['educacao'] as const,
  escolas: () => [...educacaoKeys.all, 'escolas'] as const,
  escolaPorInep: (codigoInep: string) => [...educacaoKeys.escolas(), 'inep', codigoInep] as const,
  alunos: () => [...educacaoKeys.all, 'alunos'] as const,
  // Filtro serializável (objeto) na key — invalidação por prefixo via educacaoKeys.alunos().
  alunosBusca: (filtro: unknown) => [...educacaoKeys.alunos(), 'busca', filtro] as const,
  alunoPorId: (alunoId: string) => [...educacaoKeys.alunos(), 'id', alunoId] as const,
  turmas: () => [...educacaoKeys.all, 'turmas'] as const,
  turmasBusca: (filtro: unknown) => [...educacaoKeys.turmas(), 'busca', filtro] as const,
  turmaPorId: (turmaId: string) => [...educacaoKeys.turmas(), 'id', turmaId] as const,
  matriculas: () => [...educacaoKeys.all, 'matriculas'] as const,
  matriculasPorAluno: (alunoId: string) => [...educacaoKeys.matriculas(), 'aluno', alunoId] as const,
  matriculasDaTurma: (turmaId: string, dataReferencia: string) =>
    [...educacaoKeys.matriculas(), 'turma', turmaId, dataReferencia] as const,
  diarioDaMatricula: (matriculaId: string) =>
    [...educacaoKeys.all, 'diarios', 'matricula', matriculaId] as const,
  frequenciaDoDiario: (diarioId: string) =>
    [...educacaoKeys.all, 'diarios', diarioId, 'frequencia'] as const,
  diarioDaTurma: (turmaId: string, data: string) =>
    [...educacaoKeys.turmas(), 'id', turmaId, 'diario', data] as const,
  boletimDaMatricula: (matriculaId: string) =>
    [...educacaoKeys.matriculas(), 'id', matriculaId, 'boletim'] as const,
  historicoEscolar: (alunoId: string) =>
    [...educacaoKeys.alunos(), 'id', alunoId, 'historico'] as const,
  fiscal: () => [...educacaoKeys.all, 'fiscal'] as const,
  mde: (exercicio: number) => [...educacaoKeys.fiscal(), 'mde', exercicio] as const,
  fundebAplicacao: (exercicio: number) =>
    [...educacaoKeys.fiscal(), 'fundeb', exercicio, 'aplicacao'] as const,
  // --- Merenda (PNAE) ---
  merenda: () => [...educacaoKeys.all, 'merenda'] as const,
  cardapios: () => [...educacaoKeys.merenda(), 'cardapios'] as const,
  cardapiosBusca: (filtro: unknown) => [...educacaoKeys.cardapios(), 'busca', filtro] as const,
  cardapioPorId: (cardapioId: string) => [...educacaoKeys.cardapios(), 'id', cardapioId] as const,
  consumoMerenda: (escolaId: string, de: string, ate: string) =>
    [...educacaoKeys.merenda(), 'consumo', escolaId, de, ate] as const,
  // --- Transporte (PNATE) ---
  rotas: () => [...educacaoKeys.all, 'rotas'] as const,
  rotasBusca: (filtro: unknown) => [...educacaoKeys.rotas(), 'busca', filtro] as const,
  rotaPorId: (rotaId: string) => [...educacaoKeys.rotas(), 'id', rotaId] as const,
};

/** Os endpoints de criação retornam `{ id }` (Results.Ok(new { id })). */
export interface CriadoResponse {
  id: string;
}
