// Constantes compartilhadas do modulo Legislativo: enums de dominio (valores
// numericos espelham o backend — vide *.rules.md), query keys (fonte unica de
// invalidacao) e o contrato de resposta de criacao. Mantem cada *.api.ts < 300
// linhas (CLAUDE.md §13).

// ---------------------------------------------------------------------------
// Enums de dominio
// ---------------------------------------------------------------------------

/** Especie da materia (TipoProposicao). */
export const TIPOS_PROPOSICAO = [
  { value: 1, label: 'Projeto de Lei Ordinaria (PLO)' },
  { value: 2, label: 'Projeto de Lei Complementar (PLC)' },
  { value: 3, label: 'Emenda a Lei Organica (EmendaALOM)' },
  { value: 4, label: 'Projeto de Decreto Legislativo (PDL)' },
  { value: 5, label: 'Projeto de Resolucao (PR)' },
  { value: 6, label: 'Requerimento' },
  { value: 7, label: 'Indicacao' },
  { value: 8, label: 'Mocao' },
] as const;

/** Regime de tramitacao (RegimeTramitacao). */
export const REGIMES_TRAMITACAO = [
  { value: 1, label: 'Ordinario' },
  { value: 2, label: 'Urgencia' },
  { value: 3, label: 'Prioridade' },
  { value: 4, label: 'Qualificado' },
] as const;

/** Situacao da proposicao (SituacaoProposicao). */
export const SITUACOES_PROPOSICAO = [
  { value: 1, label: 'Apresentada' },
  { value: 2, label: 'Distribuida' },
  { value: 3, label: 'Em Ordem do Dia' },
  { value: 4, label: 'Aprovada' },
  { value: 5, label: 'Rejeitada' },
  { value: 6, label: 'Arquivada' },
  { value: 7, label: 'Autografo Enviado' },
] as const;

/** Especie da sessao (TipoSessao). */
export const TIPOS_SESSAO = [
  { value: 1, label: 'Ordinaria' },
  { value: 2, label: 'Extraordinaria' },
] as const;

/** Modalidade de apuracao (TipoVotacao). */
export const TIPOS_VOTACAO = [
  { value: 1, label: 'Simbolica' },
  { value: 2, label: 'Nominal' },
  { value: 3, label: 'Secreta' },
] as const;

/** Criterio de aprovacao (MaioriaExigida). */
export const MAIORIAS_EXIGIDAS = [
  { value: 1, label: 'Simples (> 50% dos presentes)' },
  { value: 2, label: 'Absoluta (> 50% dos membros)' },
  { value: 3, label: 'Qualificada (2/3 em dois turnos)' },
] as const;

/** Sentido do voto nominal (SentidoVoto). */
export const SENTIDOS_VOTO = [
  { value: 1, label: 'Sim' },
  { value: 2, label: 'Não' },
  { value: 3, label: 'Abstenção' },
] as const;

/** Cargo do vereador na Mesa Diretora (CargoMesa). */
export const CARGOS_MESA = [
  { value: 0, label: 'Nenhum' },
  { value: 1, label: 'Presidente' },
  { value: 2, label: 'Vice-Presidente' },
  { value: 3, label: '1º Secretário' },
  { value: 4, label: '2º Secretário' },
] as const;

/** Situacao do mandato do vereador (SituacaoVereador). */
export const SITUACOES_VEREADOR = [
  { value: 1, label: 'Ativo' },
  { value: 2, label: 'Licenciado' },
  { value: 3, label: 'Suplente em Exercício' },
  { value: 4, label: 'Afastado' },
] as const;

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao)
// ---------------------------------------------------------------------------

export const legislativoKeys = {
  all: ['legislativo'] as const,
  proposicoes: () => [...legislativoKeys.all, 'proposicoes'] as const,
  proposicoesPorSituacao: (situacao: number) =>
    [...legislativoKeys.proposicoes(), 'situacao', situacao] as const,
  proposicao: (id: string) => [...legislativoKeys.proposicoes(), 'detalhe', id] as const,
  proposicaoTramitacao: (id: string) =>
    [...legislativoKeys.proposicoes(), 'tramitacao', id] as const,
  sessoes: () => [...legislativoKeys.all, 'sessoes'] as const,
  sessoesAgendadas: () => [...legislativoKeys.sessoes(), 'agendadas'] as const,
  sessao: (id: string) => [...legislativoKeys.sessoes(), 'detalhe', id] as const,
  sessaoPresencas: (id: string) => [...legislativoKeys.sessoes(), 'presencas', id] as const,
  sessaoAta: (id: string) => [...legislativoKeys.sessoes(), 'ata', id] as const,
  votacoes: () => [...legislativoKeys.all, 'votacoes'] as const,
  votacoesPorSessao: (sessaoId: string) =>
    [...legislativoKeys.votacoes(), 'sessao', sessaoId] as const,
  votacao: (id: string) => [...legislativoKeys.votacoes(), 'detalhe', id] as const,
  votacaoPlacar: (id: string) => [...legislativoKeys.votacoes(), 'placar', id] as const,
  votacaoPainel: (id: string) => [...legislativoKeys.votacoes(), 'painel', id] as const,
  votacaoVotosNominais: (id: string) =>
    [...legislativoKeys.votacoes(), 'votos-nominais', id] as const,
  vereadores: () => [...legislativoKeys.all, 'vereadores'] as const,
  vereador: (id: string) => [...legislativoKeys.vereadores(), 'detalhe', id] as const,
};

/** Resposta dos endpoints de criacao (`Results.Ok(new { id })`). */
export interface CriacaoResponse {
  id: string;
}
