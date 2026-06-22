// Camada de API do agregado Beneficio (modulo AssistenciaSocial).
// Replica o PADRAO-OURO de tributos/api.ts:
//  - DTOs no topo (espelham os Commands/Queries reais da Application);
//  - funcoes de acesso via http client tipado (Authorization + ProblemDetails->ApiError);
//  - query keys centralizadas para invalidacao consistente;
//  - hooks TanStack Query (useQuery/useMutation) para TODAS as operacoes do agregado.
//
// Contrato real (AssistenciaSocialEndpoints.cs):
//  POST   /assistenciasocial/beneficios/elegibilidade            -> AvaliarElegibilidadeBeneficio  (command)
//  POST   /assistenciasocial/beneficios/{id}/cesta-basica        -> EntregarCestaBasica            (command)
//  GET    /assistenciasocial/familias/{familiaId}/beneficios     -> ObterBeneficiosDaFamilia       (query)
//  GET    /assistenciasocial/beneficios/concessoes?ano&mes       -> ObterConcessoesPorCompetencia  (query)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham a Application: TipoBeneficio, SituacaoBeneficio, BeneficioResumo,
//       AvaliarElegibilidadeBeneficioCommand + DadosElegibilidadeDto, EntregarCestaBasicaCommand)
// ---------------------------------------------------------------------------

/** Enum TipoBeneficio (Bpc/Pbf/Eventual) — projetado por ToString() no backend. */
export type TipoBeneficio = 'Bpc' | 'Pbf' | 'Eventual';

/** Enum SituacaoBeneficio (EmAvaliacao/Concedida/Indeferida) — projetado por ToString(). */
export type SituacaoBeneficio = 'EmAvaliacao' | 'Concedida' | 'Indeferida';

/** Projecao de leitura (BeneficioResumo) retornada pelas duas queries. */
export interface BeneficioResumo {
  id: string;
  familiaId: string;
  /** Tipo projetado de ToString(): 'Bpc' | 'Pbf' | 'Eventual'. */
  tipo: TipoBeneficio;
  /** Competencia formatada pelo backend (mm/aaaa). */
  competencia: string;
  /** Valor concedido, quando houver (nulo em cesta basica / em avaliacao). */
  valor: number | null;
  /** Situacao projetada de ToString(). */
  situacao: SituacaoBeneficio;
  /** Motivo do indeferimento, quando Indeferida. */
  motivoIndeferimento: string | null;
  /** Data da decisao (ISO yyyy-mm-dd), quando decidido. */
  dataDecisao: string | null;
}

/**
 * Dados faticos do requerente para a avaliacao de elegibilidade (DadosElegibilidadeDto).
 * Subsidiam a decisao do dominio; nao trafegam no barramento (minimizacao — LGPD art. 11).
 */
export interface DadosElegibilidadeInput {
  /** Idade do requerente em anos (criterio BPC idoso >= 65). */
  idade: number;
  /** Indica deficiencia (PCD) declarada. */
  possuiDeficiencia: boolean;
  /** Avaliacao biopsicossocial concluida (requisito BPC/PCD). */
  possuiAvaliacaoBiopsicossocial: boolean;
  /** Beneficio concomitante da Seguridade (veda BPC — I-3). */
  acumulaSeguridadeSocial: boolean;
  /** Inscricao no CadUnico (preferencia em eventual — I-4). */
  inscritoCadUnico: boolean;
}

/** Entrada do command AvaliarElegibilidadeBeneficio (POST /beneficios/elegibilidade). */
export interface AvaliarElegibilidadeInput {
  familiaId: string;
  tipo: TipoBeneficio;
  /** Competencia ano/mes (VO Competencia no backend). */
  competencia: { ano: number; mes: number };
  dados: DadosElegibilidadeInput;
  /** Valor a conceder quando elegivel (nulo em cesta basica / provisao em especie — I-9). */
  valor?: number | null;
}

/** Resposta do command de avaliacao: { id } do beneficio criado. */
export interface AvaliarElegibilidadeResposta {
  id: string;
}

/** Entrada do command EntregarCestaBasica (POST /beneficios/{id}/cesta-basica). */
export interface EntregarCestaBasicaInput {
  beneficioId: string;
  /** Quantidade de cestas (>= 1 — I-8). */
  quantidade: number;
}

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao)
// ---------------------------------------------------------------------------

export const beneficioKeys = {
  all: ['assistenciasocial', 'beneficios'] as const,
  porFamilia: (familiaId: string) => [...beneficioKeys.all, 'familia', familiaId] as const,
  concessoes: (ano: number, mes: number) =>
    [...beneficioKeys.all, 'concessoes', ano, mes] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarBeneficiosDaFamilia(
  familiaId: string,
  signal?: AbortSignal,
): Promise<BeneficioResumo[]> {
  return http.get<BeneficioResumo[]>(
    `/assistenciasocial/familias/${familiaId}/beneficios`,
    { signal },
  );
}

function listarConcessoesPorCompetencia(
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<BeneficioResumo[]> {
  const params = new URLSearchParams({ ano: String(ano), mes: String(mes) });
  return http.get<BeneficioResumo[]>(
    `/assistenciasocial/beneficios/concessoes?${params.toString()}`,
    { signal },
  );
}

function avaliarElegibilidade(
  input: AvaliarElegibilidadeInput,
): Promise<AvaliarElegibilidadeResposta> {
  return http.post<AvaliarElegibilidadeResposta>(
    '/assistenciasocial/beneficios/elegibilidade',
    input,
  );
}

function entregarCestaBasica(input: EntregarCestaBasicaInput): Promise<void> {
  // Endpoint retorna 204 No Content; payload carrega apenas a quantidade.
  return http.post<void>(
    `/assistenciasocial/beneficios/${input.beneficioId}/cesta-basica`,
    { quantidade: input.quantidade },
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/**
 * Lista os beneficios de uma familia (ObterBeneficiosDaFamilia, tenant-scoped).
 * `enabled` controla o disparo sob demanda (apos a consulta).
 */
export function useBeneficiosDaFamilia(familiaId: string, enabled = true) {
  return useQuery({
    queryKey: beneficioKeys.porFamilia(familiaId),
    queryFn: ({ signal }) => listarBeneficiosDaFamilia(familiaId, signal),
    enabled: enabled && familiaId.trim().length > 0,
  });
}

/**
 * Lista as concessoes de uma competencia (ObterConcessoesPorCompetencia — apenas Concedida).
 * `enabled` controla o disparo sob demanda.
 */
export function useConcessoesPorCompetencia(ano: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: beneficioKeys.concessoes(ano, mes),
    queryFn: ({ signal }) => listarConcessoesPorCompetencia(ano, mes, signal),
    enabled: enabled && ano > 0 && mes >= 1 && mes <= 12,
  });
}

/**
 * Avalia a elegibilidade e registra a concessao/indeferimento (AvaliarElegibilidadeBeneficio).
 * Invalida a lista de beneficios da familia afetada.
 */
export function useAvaliarElegibilidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: avaliarElegibilidade,
    onSuccess: (_resposta, variables) => {
      queryClient.invalidateQueries({
        queryKey: beneficioKeys.porFamilia(variables.familiaId),
      });
      queryClient.invalidateQueries({
        queryKey: beneficioKeys.concessoes(variables.competencia.ano, variables.competencia.mes),
      });
    },
  });
}

/**
 * Registra a entrega de cesta basica sobre um beneficio eventual concedido (EntregarCestaBasica).
 * Invalida a lista da familia para refletir a entrega.
 */
export function useEntregarCestaBasica(familiaId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: entregarCestaBasica,
    onSuccess: () => {
      if (familiaId.trim().length > 0) {
        queryClient.invalidateQueries({ queryKey: beneficioKeys.porFamilia(familiaId) });
      }
    },
  });
}
