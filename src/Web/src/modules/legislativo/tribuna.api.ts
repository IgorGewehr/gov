// Camada de API da Tribuna (modulo Legislativo, parte 2). DTOs + acesso HTTP
// tipado + hooks TanStack Query, cobrindo /api/legislativo/sessoes/{id}/tribuna:
// painel de oradores inscritos + controle do uso da palavra (cronometro).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/** Inscricao de um orador na Tribuna de uma sessao (back: InscricaoDto). */
export interface InscricaoResumo {
  /** Identificador da inscricao (back: InscricaoId). */
  inscricaoId: string;
  ordem: number;
  vereadorId: string;
  /** Fase de uso da palavra (back: Fase, string). */
  fase: string;
  situacao: string;
  /** Tempo concedido (segundos). */
  tempoConcedidoSegundos: number;
  /** Indica se a fala esta pausada (back: Pausada). */
  pausada: boolean;
  /** Tempo utilizado apos encerrar (segundos) — back: TempoUtilizadoSegundos, nullable. */
  tempoUtilizadoSegundos: number | null;
  /** Excedente apos encerrar (segundos) — back: ExcedenteSegundos, nullable. */
  excedenteSegundos: number | null;
  /** Instante de inicio do trecho em andamento (ISO) ou null se parado. */
  iniciadoEm: string | null;
}

/** Painel da Tribuna de uma sessao (back: TribunaDto). */
export interface TribunaPainel {
  /** Identificador da tribuna (back: TribunaId) — necessario nos comandos. */
  tribunaId: string;
  sessaoId: string;
  /** Tempo padrao por orador (segundos) — back: TempoPadraoSegundos. */
  tempoPadraoSegundos: number;
  /** Inscricao do orador em uso (back: OradorAtualId), se houver. */
  oradorAtualId: string | null;
  inscricoes: InscricaoResumo[];
}

/** Payload de inscricao de um orador (InscreverOradorPayload). */
export interface InscricaoInput {
  /** Tribuna alvo (back: TribunaId, obrigatorio). */
  tribunaId: string;
  vereadorId: string;
  /** Fase de uso da palavra (back: Fase, obrigatorio, int). */
  fase: number;
  tempoConcedidoSegundos: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function obterTribuna(sessaoId: string, signal?: AbortSignal): Promise<TribunaPainel> {
  return http.get<TribunaPainel>(`/legislativo/sessoes/${sessaoId}/tribuna`, { signal });
}

async function inscrever(sessaoId: string, input: InscricaoInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>(
    `/legislativo/sessoes/${sessaoId}/tribuna/inscricoes`,
    input,
  );
  return id;
}

function comandar(
  sessaoId: string,
  inscricaoId: string,
  verbo: string,
  tribunaId: string,
): Promise<void> {
  // O backend exige { tribunaId } no corpo (TribunaControlePayload).
  return http.post<void>(
    `/legislativo/sessoes/${sessaoId}/tribuna/inscricoes/${inscricaoId}/${verbo}`,
    { tribunaId },
  );
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/**
 * Painel da Tribuna com auto-refresh por polling ENQUANTO a sessao estiver
 * Aberta — mantem o cronometro dos oradores sincronizado com o servidor.
 * O backend NAO envia a situacao da sessao no painel; quem chama informa
 * `sessaoAberta` (resolvido via `useSessao`) para controlar o polling.
 */
export function useTribuna(sessaoId: string, sessaoAberta: boolean, intervaloMs = 2000) {
  return useQuery({
    queryKey: legislativoKeys.tribuna(sessaoId),
    queryFn: ({ signal }) => obterTribuna(sessaoId, signal),
    enabled: sessaoId.trim().length > 0,
    refetchInterval: sessaoAberta ? intervaloMs : false,
  });
}

/** Invalida o painel da Tribuna de uma sessao. */
function useInvalidarTribuna(sessaoId: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: legislativoKeys.tribuna(sessaoId) });
  };
}

/** Inscreve um orador na Tribuna da sessao. */
export function useInscreverOrador(sessaoId: string) {
  const invalidar = useInvalidarTribuna(sessaoId);
  return useMutation({
    mutationFn: (input: InscricaoInput) => inscrever(sessaoId, input),
    onSuccess: invalidar,
  });
}

/** Variaveis dos comandos de cronometro: inscricao alvo + tribuna (exigida pelo backend). */
export interface ComandoInscricaoVars {
  inscricaoId: string;
  tribunaId: string;
}

/** Fabrica de hooks de comando do cronometro (inicio/pausa/retomada/encerramento). */
function useComandoInscricao(sessaoId: string, verbo: string) {
  const invalidar = useInvalidarTribuna(sessaoId);
  return useMutation({
    mutationFn: ({ inscricaoId, tribunaId }: ComandoInscricaoVars) =>
      comandar(sessaoId, inscricaoId, verbo, tribunaId),
    onSuccess: invalidar,
  });
}

/** Inicia o uso da palavra (parte o cronometro). */
export const useIniciarFala = (sessaoId: string) => useComandoInscricao(sessaoId, 'inicio');
/** Pausa o uso da palavra (fecha o trecho em andamento). */
export const usePausarFala = (sessaoId: string) => useComandoInscricao(sessaoId, 'pausa');
/** Retoma o uso da palavra apos uma pausa. */
export const useRetomarFala = (sessaoId: string) => useComandoInscricao(sessaoId, 'retomada');
/** Encerra definitivamente o uso da palavra do orador. */
export const useEncerrarFala = (sessaoId: string) =>
  useComandoInscricao(sessaoId, 'encerramento');
