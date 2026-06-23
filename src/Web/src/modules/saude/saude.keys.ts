// Query keys do módulo Saúde — fonte ÚNICA para invalidação consistente entre os
// arquivos de API por agregado (paciente/atendimento/regulacao).
export const saudeKeys = {
  all: ['saude'] as const,
  pacientes: () => [...saudeKeys.all, 'pacientes'] as const,
  pacientesBusca: (filtro: unknown) => [...saudeKeys.pacientes(), 'busca', filtro] as const,
  pacientePorCns: (cns: string) => [...saudeKeys.pacientes(), 'por-cns', cns] as const,
  historico: (pacienteId: string) => [...saudeKeys.pacientes(), 'historico', pacienteId] as const,
  atendimentos: () => [...saudeKeys.all, 'atendimentos'] as const,
  atendimentosPorPaciente: (pacienteId: string) =>
    [...saudeKeys.atendimentos(), 'paciente', pacienteId] as const,
  atendimento: (id: string) => [...saudeKeys.atendimentos(), 'detalhe', id] as const,
  regulacao: () => [...saudeKeys.all, 'regulacao'] as const,
  filaRegulacao: (codigoSigtap: string, prioridade: string) =>
    [...saudeKeys.regulacao(), 'fila', codigoSigtap, prioridade] as const,
  solicitacao: (id: string) => [...saudeKeys.regulacao(), 'detalhe', id] as const,
  fiscal: () => [...saudeKeys.all, 'fiscal'] as const,
  asps: (exercicio: number) => [...saudeKeys.fiscal(), 'asps', exercicio] as const,
  execucaoFms: (fundoId: string) => [...saudeKeys.fiscal(), 'fms', fundoId, 'execucao'] as const,
};

/** Resposta dos endpoints de criação (POST) do módulo: `{ id }`. */
export interface IdResponse {
  id: string;
}
