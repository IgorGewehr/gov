// Camada de API do PBF — acompanhamento de condicionalidades por familia (modulo AssistenciaSocial).
// Espelha o contrato real (AssistenciaSocialEndpoints.cs):
//  POST   /assistenciasocial/pbf/acompanhamentos                                  -> AbrirAcompanhamentoCondicionalidade (command -> { id })
//  POST   /assistenciasocial/pbf/acompanhamentos/{id}/condicionalidades           -> RegistrarCondicionalidade           (command -> { id })
//  POST   /assistenciasocial/pbf/condicionalidades/{registroId}/justificativa     -> JustificarDescumprimento            (204)
//  GET    /assistenciasocial/pbf/descumprimentos?ano&mes&efeitoMinimo             -> ObterDescumprimentos                (query)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs (espelham a Application: TipoCondicionalidade, StatusCondicionalidade,
//       EfeitoDescumprimento, AcompanhamentoResultado/CondicionalidadeResultado)
// ---------------------------------------------------------------------------

/** Enum TipoCondicionalidade — projetado por ToString() no backend. */
export type TipoCondicionalidade =
  | 'EducacaoFrequenciaEscolar'
  | 'SaudeVacinacaoNutricaoInfantil'
  | 'SaudePreNatalGestante';

/** Enum StatusCondicionalidade — projetado por ToString(). */
export type StatusCondicionalidade = 'Pendente' | 'Cumprida' | 'Descumprida' | 'Justificada';

/** Enum EfeitoDescumprimento — projetado por ToString() (gradacao gerencial local). */
export type EfeitoDescumprimento = 'Nenhum' | 'Advertencia' | 'Bloqueio' | 'Suspensao';

/** Linha de condicionalidade do acompanhamento (CondicionalidadeResultado). */
export interface CondicionalidadeResultado {
  registroId: string;
  tipo: TipoCondicionalidade;
  membroId: string;
  status: StatusCondicionalidade;
  observacao: string | null;
}

/** Resultado consolidado do acompanhamento de condicionalidades (AcompanhamentoResultado). */
export interface AcompanhamentoResultado {
  acompanhamentoId: string;
  familiaId: string;
  /** Competencia formatada pelo backend (mm/aaaa). */
  competencia: string;
  /** Efeito gradativo vigente, projetado de ToString(). */
  efeito: EfeitoDescumprimento;
  descumprimentosEfetivos: number;
  condicionalidades: CondicionalidadeResultado[];
}

/** Entrada do command AbrirAcompanhamentoCondicionalidade (POST /pbf/acompanhamentos). */
export interface AbrirAcompanhamentoInput {
  familiaId: string;
  ano: number;
  mes: number;
}

/** Entrada do command RegistrarCondicionalidade (POST /pbf/acompanhamentos/{id}/condicionalidades). */
export interface RegistrarCondicionalidadeInput {
  acompanhamentoId: string;
  tipo: TipoCondicionalidade;
  membroId: string;
  status: StatusCondicionalidade;
  observacao?: string | null;
}

/** Entrada do command JustificarDescumprimento (POST /pbf/condicionalidades/{registroId}/justificativa). */
export interface JustificarDescumprimentoInput {
  acompanhamentoId: string;
  registroId: string;
  motivo: string;
}

/** Resposta dos commands que retornam { id }. */
export interface IdResposta {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte unica para invalidacao)
// ---------------------------------------------------------------------------

export const pbfKeys = {
  all: ['assistenciasocial', 'pbf'] as const,
  descumprimentos: (ano: number, mes: number, efeitoMinimo: EfeitoDescumprimento | null) =>
    [...pbfKeys.all, 'descumprimentos', ano, mes, efeitoMinimo ?? 'todos'] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarDescumprimentos(
  ano: number,
  mes: number,
  efeitoMinimo: EfeitoDescumprimento | null,
  signal?: AbortSignal,
): Promise<AcompanhamentoResultado[]> {
  const params = new URLSearchParams({ ano: String(ano), mes: String(mes) });
  if (efeitoMinimo) params.append('efeitoMinimo', efeitoMinimo);
  return http.get<AcompanhamentoResultado[]>(
    `/assistenciasocial/pbf/descumprimentos?${params.toString()}`,
    { signal },
  );
}

function abrirAcompanhamento(input: AbrirAcompanhamentoInput): Promise<IdResposta> {
  return http.post<IdResposta>('/assistenciasocial/pbf/acompanhamentos', input);
}

function registrarCondicionalidade(input: RegistrarCondicionalidadeInput): Promise<IdResposta> {
  return http.post<IdResposta>(
    `/assistenciasocial/pbf/acompanhamentos/${input.acompanhamentoId}/condicionalidades`,
    {
      tipo: input.tipo,
      membroId: input.membroId,
      status: input.status,
      observacao: input.observacao ?? null,
    },
  );
}

function justificarDescumprimento(input: JustificarDescumprimentoInput): Promise<void> {
  // Endpoint retorna 204 No Content; o registroId vem na rota, o restante no corpo.
  return http.post<void>(
    `/assistenciasocial/pbf/condicionalidades/${input.registroId}/justificativa`,
    { acompanhamentoId: input.acompanhamentoId, motivo: input.motivo },
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/**
 * Lista as familias em descumprimento numa competencia (ObterDescumprimentos, tenant-scoped).
 * Base da busca ativa do CRAS. `enabled` controla o disparo sob demanda.
 */
export function useDescumprimentos(
  ano: number,
  mes: number,
  efeitoMinimo: EfeitoDescumprimento | null,
  enabled = true,
) {
  return useQuery({
    queryKey: pbfKeys.descumprimentos(ano, mes, efeitoMinimo),
    queryFn: ({ signal }) => listarDescumprimentos(ano, mes, efeitoMinimo, signal),
    enabled: enabled && ano > 0 && mes >= 1 && mes <= 12,
  });
}

/** Abre o acompanhamento de condicionalidades de uma familia numa competencia (idempotente). */
export function useAbrirAcompanhamento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirAcompanhamento,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pbfKeys.all });
    },
  });
}

/** Registra uma condicionalidade (educacao/saude) de um membro no acompanhamento. */
export function useRegistrarCondicionalidade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registrarCondicionalidade,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pbfKeys.all });
    },
  });
}

/** Justifica um descumprimento (motivo do CRAS) — reduz a gradacao do efeito. */
export function useJustificarDescumprimento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: justificarDescumprimento,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pbfKeys.all });
    },
  });
}
