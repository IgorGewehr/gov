// Camada de API do agregado Declaração Fiscal (SICONFI / MSC / RREO / RGF / DCA).
// Contrato real: src/Modules/Transparencia/.../TransparenciaEndpoints.cs.
// Endpoints cobertos: consolidar (POST), obter (GET id), listar (GET), transmissão,
// homologação e rejeição (POST de transição de estado; rejeição exige motivo).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { transparenciaKeys } from './keys';
import type { CriacaoResponse } from './keys';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Espécie do demonstrativo fiscal transmitido ao SICONFI. */
export type TipoDeclaracaoFiscal = 'Msc' | 'Rreo' | 'Rgf' | 'Dca';

/** Situação no ciclo Consolidada → Transmitida → Homologada/Rejeitada. */
export type SituacaoDeclaracaoFiscal = 'Consolidada' | 'Transmitida' | 'Homologada' | 'Rejeitada';

/** Natureza do saldo de uma linha contábil (PCASP): 1 = Devedor, 2 = Credor. */
export type NaturezaSaldo = 1 | 2;

export interface DeclaracaoFiscalResumo {
  id: string;
  tipoDeclaracao: TipoDeclaracaoFiscal;
  exercicio: number;
  periodo: string;
  situacao: SituacaoDeclaracaoFiscal;
  dataLimite: string;
  dataTransmissao: string | null;
}

export interface DeclaracaoFiscalDetalhe {
  id: string;
  tipoDeclaracao: TipoDeclaracaoFiscal;
  exercicio: number;
  competencia: string | null;
  bimestre: string | null;
  quadrimestre: string | null;
  situacao: SituacaoDeclaracaoFiscal;
  dataLimite: string;
  dataTransmissao: string | null;
  protocoloSiconfi: string | null;
  totalDebitos: number;
  totalCreditos: number;
}

export interface LinhaContabilInput {
  contaPcasp: string;
  naturezaSaldo: NaturezaSaldo;
  valor: number;
  informacaoComplementar?: string | null;
}

export interface ConsolidarDeclaracaoFiscalInput {
  tipoDeclaracao: TipoDeclaracaoFiscal;
  exercicio: number;
  mes?: number | null;
  numeroBimestre?: number | null;
  numeroQuadrimestre?: number | null;
  linhas: LinhaContabilInput[];
}

export interface ListarDeclaracoesParams {
  exercicio: number;
  tipo?: TipoDeclaracaoFiscal | null;
  situacao?: SituacaoDeclaracaoFiscal | null;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarDeclaracoes(
  params: ListarDeclaracoesParams,
  signal?: AbortSignal,
): Promise<DeclaracaoFiscalResumo[]> {
  return http.get<DeclaracaoFiscalResumo[]>('/transparencia/declaracoes-fiscais', {
    query: { exercicio: params.exercicio, tipo: params.tipo, situacao: params.situacao },
    signal,
  });
}

function obterDeclaracao(id: string, signal?: AbortSignal): Promise<DeclaracaoFiscalDetalhe> {
  return http.get<DeclaracaoFiscalDetalhe>(`/transparencia/declaracoes-fiscais/${id}`, { signal });
}

function consolidarDeclaracao(input: ConsolidarDeclaracaoFiscalInput): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>('/transparencia/declaracoes-fiscais', input);
}

function transmitirDeclaracao(id: string): Promise<void> {
  return http.post<void>(`/transparencia/declaracoes-fiscais/${id}/transmissao`);
}

function homologarDeclaracao(id: string): Promise<void> {
  return http.post<void>(`/transparencia/declaracoes-fiscais/${id}/homologacao`);
}

function rejeitarDeclaracao(id: string, motivo: string): Promise<void> {
  return http.post<void>(`/transparencia/declaracoes-fiscais/${id}/rejeicao`, { motivo });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as declarações fiscais de um exercício, com filtros opcionais. */
export function useDeclaracoesFiscais(params: ListarDeclaracoesParams, enabled = true) {
  return useQuery({
    queryKey: transparenciaKeys.declaracoesLista(params),
    queryFn: ({ signal }) => listarDeclaracoes(params, signal),
    enabled: enabled && Number.isFinite(params.exercicio) && params.exercicio > 0,
  });
}

/** Detalhe de uma declaração fiscal. */
export function useDeclaracaoFiscal(id: string) {
  return useQuery({
    queryKey: transparenciaKeys.declaracao(id),
    queryFn: ({ signal }) => obterDeclaracao(id, signal),
    enabled: id.trim().length > 0,
  });
}

/** Consolida (cria) uma nova declaração fiscal e invalida as listas. */
export function useConsolidarDeclaracaoFiscal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: consolidarDeclaracao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.declaracoes() });
    },
  });
}

/** Dispara uma transição de estado da declaração (transmissão/homologação/rejeição). */
export function useTransicaoDeclaracaoFiscal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { id: string; acao: 'transmitir' | 'homologar' | 'rejeitar'; motivo?: string }) => {
      if (args.acao === 'transmitir') return transmitirDeclaracao(args.id);
      if (args.acao === 'homologar') return homologarDeclaracao(args.id);
      return rejeitarDeclaracao(args.id, args.motivo ?? '');
    },
    onSuccess: (_data, args) => {
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.declaracoes() });
      queryClient.invalidateQueries({ queryKey: transparenciaKeys.declaracao(args.id) });
    },
  });
}
