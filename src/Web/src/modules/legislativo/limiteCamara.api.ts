// Camada de API do LIMITE DE DESPESA DA CAMARA (art. 29-A da CF/88) — prestacao
// LOCAL (W9.5). Cobre /api/legislativo/limite-camara:
//   abrir (POST)          -> AbrirApuracaoArt29ACommand: base de receita do ano
//                            anterior (informada OU de Financas), repasse e despesas
//                            discriminadas por natureza. Demonstrativo nasce Rascunho.
//   demonstrativo (GET)   -> ObterDemonstrativoArt29AQuery por exercicio: teto (caput),
//                            subteto da folha (§1), realizado x limite e SEMAFOROS.
//   consolidar (POST)     -> ConsolidarApuracaoArt29ACommand: congela para a prestacao.
// A transmissao oficial ao TCE-RS e M10 — aqui e estimativa/prestacao local.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys, type CriacaoResponse } from './legislativo.shared';

// ---------------------------------------------------------------------------
// DTOs (espelham os records do backend — vide LegislativoEndpoints.Normas.cs +
// ObterDemonstrativoArt29A.cs / AbrirApuracaoArt29A.cs)
// ---------------------------------------------------------------------------

/** Parcela de despesa discriminada no demonstrativo (back: DespesaCamaraDto). */
export interface DespesaCamara {
  /** Natureza (Enum.ToString(): PessoalAtivo/InativosPensionistas/OutrasDespesas). */
  natureza: string;
  valor: number;
  descricao: string | null;
}

/** Demonstrativo do art. 29-A (back: DemonstrativoArt29ADto). */
export interface DemonstrativoArt29A {
  apuracaoId: string;
  /** Exercicio orcamentario sob teto. */
  exercicio: number;
  /** Exercicio da receita de base (anterior). */
  exercicioBaseReceita: number;
  /** Situacao (Enum.ToString(): Rascunho/Consolidada). */
  situacao: string;
  populacao: number;
  /** Percentual-limite da faixa aplicada (fracao 0..1). */
  percentualFaixa: number;
  receitaTributaria: number;
  transferencias: number;
  /** Base total (tributaria + transferencias). */
  baseReceita: number;
  /** Teto da despesa total (base x percentual). */
  tetoDespesaTotal: number;
  despesaTotalRealizada: number;
  /** Folga do teto (negativa se excede). */
  margemTeto: number;
  /** Fracao de utilizacao do teto (0..1+). */
  utilizacaoTeto: number;
  /** Semaforo do teto (Adequado/Atencao/Excedido). */
  semaforoTeto: string;
  /** Repasse/duodecimo recebido. */
  repasseRecebido: number;
  /** Subteto da folha (repasse x §1, 70%). */
  subtetoFolha: number;
  folhaRealizada: number;
  margemSubtetoFolha: number;
  utilizacaoSubtetoFolha: number;
  /** Semaforo do subteto de folha. */
  semaforoFolha: string;
  /** Inativos/pensionistas integraram o teto (EC 109). */
  inativosNoTeto: boolean;
  /** Estouro de qualquer limite. */
  irregular: boolean;
  despesas: DespesaCamara[];
}

/** Entrada de uma parcela de despesa na abertura (back: DespesaCamaraInput). */
export interface DespesaCamaraInput {
  /** Natureza: 1=PessoalAtivo, 2=InativosPensionistas, 3=OutrasDespesas. */
  natureza: number;
  valor: number;
  descricao?: string | null;
}

/** Payload de abertura da apuracao (back: AbrirApuracaoArt29ACommand). */
export interface AbrirApuracaoInput {
  exercicio: number;
  /** Populacao do municipio (define a faixa do caput). */
  populacao: number;
  /** Repasse/duodecimo recebido (base do subteto §1). */
  repasseRecebido: number;
  /** Receita tributaria do exercicio anterior (nula => obtem de Financas). */
  receitaTributaria?: number | null;
  /** Transferencias do exercicio anterior (nula => obtem de Financas). */
  transferencias?: number | null;
  despesas: DespesaCamaraInput[];
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

const BASE = '/legislativo/limite-camara/apuracoes';

async function obterDemonstrativo(
  exercicio: number,
  signal?: AbortSignal,
): Promise<DemonstrativoArt29A | null> {
  // O backend devolve 200 com null quando nao ha apuracao do exercicio (Results.Ok(null)).
  const dto = await http.get<DemonstrativoArt29A | null>(`${BASE}/${exercicio}`, { signal });
  return dto ?? null;
}

async function abrirApuracao(input: AbrirApuracaoInput): Promise<string> {
  const { id } = await http.post<CriacaoResponse>(BASE, input);
  return id;
}

function consolidar(apuracaoId: string): Promise<DemonstrativoArt29A> {
  return http.post<DemonstrativoArt29A>(`${BASE}/${apuracaoId}/consolidacao`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/**
 * Demonstrativo do art. 29-A de um exercicio. `data === null` => ainda nao ha
 * apuracao aberta (a tela mostra o estado vazio + acao de abrir).
 */
export function useDemonstrativoArt29A(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: legislativoKeys.limiteCamaraDemonstrativo(exercicio),
    queryFn: ({ signal }) => obterDemonstrativo(exercicio, signal),
    enabled: enabled && Number.isInteger(exercicio) && exercicio > 0,
  });
}

/** Abre a apuracao do exercicio e invalida o demonstrativo correspondente. */
export function useAbrirApuracaoArt29A() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirApuracao,
    onSuccess: (_id, variaveis) => {
      queryClient.invalidateQueries({
        queryKey: legislativoKeys.limiteCamaraDemonstrativo(variaveis.exercicio),
      });
    },
  });
}

/**
 * Consolida (fecha) a apuracao — congela os numeros para a prestacao. Invalida o
 * demonstrativo do exercicio retornado para refletir a situacao Consolidada.
 */
export function useConsolidarApuracaoArt29A() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: consolidar,
    onSuccess: (demonstrativo) => {
      queryClient.invalidateQueries({
        queryKey: legislativoKeys.limiteCamaraDemonstrativo(demonstrativo.exercicio),
      });
    },
  });
}
