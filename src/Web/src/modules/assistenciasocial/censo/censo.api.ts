// Camada de API do Censo SUAS — unidades socioassistenciais + consolidacao/fechamento.
// Contrato real (AssistenciaSocialEndpoints.cs):
//  POST   /assistenciasocial/censo/unidades                          -> CadastrarUnidadeSocioassistencial (command -> { id })
//  POST   /assistenciasocial/censo/unidades/{id}/configuracao        -> ConfigurarUnidade                 (204)
//  GET    /assistenciasocial/censo/unidades                          -> ListarUnidadesSocioassistenciais (query)
//  POST   /assistenciasocial/censo/consolidar                        -> ConsolidarCenso                   (command -> { id })
//  POST   /assistenciasocial/censo/fechamento                        -> FecharCenso                       (204)
//  GET    /assistenciasocial/censo?exercicio                         -> ObterCenso                        (query)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham a Application: TipoUnidadeAtendimento, TipoServico,
//       UnidadeSocioassistencialResultado/ServicoOfertadoResultado, CensoResultado)
// ---------------------------------------------------------------------------

/** Enum TipoUnidadeAtendimento — projetado por ToString() no backend. */
export type TipoUnidadeAtendimento = 'Cras' | 'Creas' | 'CentroPop';

/** Enum TipoServico — projetado por ToString(). PAIF só em CRAS; PAEFI só em CREAS. */
export type TipoServico = 'Paif' | 'Paefi' | 'Scfv';

/** Servico ofertado por uma unidade (ServicoOfertadoResultado). */
export interface ServicoOfertado {
  servico: TipoServico;
  capacidadeMensal: number;
}

/** Projecao de leitura de uma unidade socioassistencial (UnidadeSocioassistencialResultado). */
export interface UnidadeSocioassistencial {
  id: string;
  nome: string;
  tipo: TipoUnidadeAtendimento;
  territorioCobertura: string;
  endereco: string;
  quantidadeProfissionais: number;
  servicos: ServicoOfertado[];
}

/** Resultado consolidado do Censo SUAS de uma unidade num exercicio (CensoResultado). */
export interface CensoResultado {
  formularioId: string;
  unidadeId: string;
  exercicio: number;
  fechado: boolean;
  quantidadeProfissionais: number;
  quantidadeServicosOfertados: number;
  familiasReferenciadas: number;
  /** Volume anual de atendimentos DERIVADO do RMA (sem dupla digitacao). */
  volumeAtendimentosAno: number;
}

/** Entrada do command CadastrarUnidadeSocioassistencial (POST /censo/unidades). */
export interface CadastrarUnidadeInput {
  nome: string;
  tipo: TipoUnidadeAtendimento;
  territorioCobertura: string;
  endereco: string;
}

/** Entrada do command ConfigurarUnidade (POST /censo/unidades/{id}/configuracao). */
export interface ConfigurarUnidadeInput {
  unidadeId: string;
  servico: TipoServico;
  capacidadeMensal: number;
  quantidadeProfissionais: number;
}

/** Entrada dos commands de consolidacao/fechamento do Censo (UnidadeId + Exercicio). */
export interface CensoUnidadeExercicioInput {
  unidadeId: string;
  exercicio: number;
}

/** Resposta dos commands que retornam { id }. */
export interface IdResposta {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao)
// ---------------------------------------------------------------------------

export const censoKeys = {
  all: ['assistenciasocial', 'censo'] as const,
  unidades: () => [...censoKeys.all, 'unidades'] as const,
  porExercicio: (exercicio: number) => [...censoKeys.all, 'exercicio', exercicio] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarUnidades(signal?: AbortSignal): Promise<UnidadeSocioassistencial[]> {
  return http.get<UnidadeSocioassistencial[]>('/assistenciasocial/censo/unidades', { signal });
}

function obterCenso(exercicio: number, signal?: AbortSignal): Promise<CensoResultado[]> {
  const params = new URLSearchParams({ exercicio: String(exercicio) });
  return http.get<CensoResultado[]>(`/assistenciasocial/censo?${params.toString()}`, { signal });
}

function cadastrarUnidade(input: CadastrarUnidadeInput): Promise<IdResposta> {
  return http.post<IdResposta>('/assistenciasocial/censo/unidades', input);
}

function configurarUnidade(input: ConfigurarUnidadeInput): Promise<void> {
  return http.post<void>(`/assistenciasocial/censo/unidades/${input.unidadeId}/configuracao`, {
    servico: input.servico,
    capacidadeMensal: input.capacidadeMensal,
    quantidadeProfissionais: input.quantidadeProfissionais,
  });
}

function consolidarCenso(input: CensoUnidadeExercicioInput): Promise<IdResposta> {
  return http.post<IdResposta>('/assistenciasocial/censo/consolidar', input);
}

function fecharCenso(input: CensoUnidadeExercicioInput): Promise<void> {
  return http.post<void>('/assistenciasocial/censo/fechamento', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as unidades socioassistenciais cadastradas (tenant-scoped). */
export function useUnidades(enabled = true) {
  return useQuery({
    queryKey: censoKeys.unidades(),
    queryFn: ({ signal }) => listarUnidades(signal),
    enabled,
  });
}

/** Lista os formularios consolidados do Censo de um exercicio. `enabled` controla o disparo. */
export function useCenso(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: censoKeys.porExercicio(exercicio),
    queryFn: ({ signal }) => obterCenso(exercicio, signal),
    enabled: enabled && exercicio >= 2000,
  });
}

/** Cadastra uma unidade socioassistencial (CRAS/CREAS/Centro POP). */
export function useCadastrarUnidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarUnidade,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: censoKeys.unidades() });
    },
  });
}

/** Configura servico ofertado + equipe de referencia da unidade. */
export function useConfigurarUnidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: configurarUnidade,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: censoKeys.unidades() });
    },
  });
}

/** (Re)consolida o Censo de uma unidade num exercicio (deriva volume do RMA). */
export function useConsolidarCenso() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: consolidarCenso,
    onSuccess: (_resposta, variables) => {
      queryClient.invalidateQueries({ queryKey: censoKeys.porExercicio(variables.exercicio) });
    },
  });
}

/** Fecha (sela) o Censo de uma unidade no exercicio para envio ao SAGI/MDS. */
export function useFecharCenso() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: fecharCenso,
    onSuccess: (_resposta, variables) => {
      queryClient.invalidateQueries({ queryKey: censoKeys.porExercicio(variables.exercicio) });
    },
  });
}
