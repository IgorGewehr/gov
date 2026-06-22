// Camada de API do submódulo IPTU / Cadastro Imobiliário (Tributos M6). Espelha o
// contrato REAL provado da Minimal API /api/tributos:
//   POST /api/tributos/imoveis                                  -> CadastrarImovel        -> { id }            [tributos.gerenciar]
//   GET  /api/tributos/contribuintes/{id}/imoveis               -> ObterImoveisDoContribuinte -> ImovelResumo[] [tributos.ver]
//   POST /api/tributos/pgv                                      -> PublicarPlantaDeValores -> { id }           [tributos.gerenciar]
//   POST /api/tributos/iptu/aliquotas                           -> PublicarTabelaAliquotas -> { id }           [tributos.gerenciar]
//   GET  /api/tributos/imoveis/{id}/iptu/{exercicio}            -> ApurarIptu             -> ApuracaoIptu      [tributos.ver]
//   POST /api/tributos/imoveis/{id}/iptu/lancar                 -> LancarIptu             -> LancamentoIptuResultado [tributos.gerenciar]
//
// Convenções (PADRÃO-OURO): percentuais em FRAÇÃO DECIMAL (0,02 = 2%); enums por
// valor INT no payload (espelham IsInEnum do validator); DTOs no topo; query keys
// centralizadas; hooks TanStack Query por operação.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Enums (string no domínio; INT no payload via *_VALOR)
// ---------------------------------------------------------------------------

/** Uso predominante do imóvel — enum UsoImovel. */
export type UsoImovel = 'Residencial' | 'Comercial' | 'Industrial' | 'Servicos' | 'Territorial';

export const USO_IMOVEL_VALOR: Record<UsoImovel, number> = {
  Residencial: 1,
  Comercial: 2,
  Industrial: 3,
  Servicos: 4,
  Territorial: 5,
};

/** Padrão construtivo do imóvel — enum PadraoConstrutivo. */
export type PadraoConstrutivo = 'Baixo' | 'Normal' | 'Alto' | 'Luxo';

export const PADRAO_CONSTRUTIVO_VALOR: Record<PadraoConstrutivo, number> = {
  Baixo: 1,
  Normal: 2,
  Alto: 3,
  Luxo: 4,
};

/** Regime de alíquota da tabela de IPTU — enum RegimeAliquota. */
export type RegimeAliquota = 'Unica' | 'Progressiva';

export const REGIME_ALIQUOTA_VALOR: Record<RegimeAliquota, number> = {
  Unica: 1,
  Progressiva: 2,
};

// ---------------------------------------------------------------------------
// Projeções de leitura
// ---------------------------------------------------------------------------

/** Projeção de resumo (ObterImoveisDoContribuinte). */
export interface ImovelResumo {
  id: string;
  contribuinteId: string;
  inscricaoImobiliaria: string;
  logradouro: string;
  numero: string;
  bairro: string;
  zona: string;
  uso: UsoImovel;
  padrao: PadraoConstrutivo;
  anoConstrucao: number | null;
  areaTerreno: number;
  areaConstruida: number;
}

/** Linha da memória de cálculo (ApurarIptu). */
export interface MemoriaCalculoLinha {
  rotulo: string;
  detalhe: string;
  valor: number;
}

/** Projeção da APURAÇÃO de IPTU (GET .../iptu/{exercicio}). */
export interface ApuracaoIptu {
  imovelId: string;
  exercicio: number;
  valorTerreno: number;
  valorConstrucao: number;
  valorVenal: number;
  aliquotaPercentual: number;
  impostoBruto: number;
  impostoDevido: number;
  descontoCotaUnica: number;
  valorCotaUnica: number;
  memoria: MemoriaCalculoLinha[];
}

/** Parcela do DAM gerado pelo lançamento (LancarIptu). */
export interface ParcelaDam {
  numero: number;
  valor: number;
  vencimento: string;
}

/** Resultado do LANÇAMENTO de IPTU (POST .../iptu/lancar). */
export interface LancamentoIptuResultado {
  lancamentoId: string;
  exercicio: number;
  valorTotal: number;
  parcelas: ParcelaDam[];
}

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** CadastrarImovelCommand. */
export interface CadastrarImovelInput {
  contribuinteId: string;
  inscricaoImobiliaria: string;
  logradouro: string;
  numero: string;
  bairro: string;
  zona: string;
  uso: number;
  padrao: number;
  anoConstrucao: number | null;
  areaTerreno: number;
  areaConstruida: number;
}

/** Zona da Planta Genérica de Valores (VUT por m² de terreno; VUC por m² construído). */
export interface ZonaPgvInput {
  zona: string;
  vut: number;
  vuc: number;
}

/** Fator de correção da PGV (fração decimal multiplicadora). */
export interface FatorPgvInput {
  chave: string;
  descricao: string;
  fator: number;
}

/** PublicarPlantaDeValoresCommand. */
export interface PublicarPgvInput {
  exercicio: number;
  zonas: ZonaPgvInput[];
  fatores: FatorPgvInput[];
}

/** Faixa de alíquota progressiva (limite superior do valor venal -> alíquota fração). */
export interface FaixaAliquotaInput {
  ateValorVenal: number;
  aliquota: number;
}

/** PublicarTabelaAliquotasCommand (predial × territorial; única ou progressiva). */
export interface PublicarAliquotasInput {
  exercicio: number;
  regime: number;
  aliquotaPredial: number;
  aliquotaTerritorial: number;
  faixasPredial: FaixaAliquotaInput[];
  faixasTerritorial: FaixaAliquotaInput[];
  descontoCotaUnica: number;
  quantidadeParcelas: number;
}

/** LancarIptuCommand (apura e gera lançamento + DAM parcelado). */
export interface LancarIptuInput {
  exercicio: number;
  quantidadeParcelas: number;
  vencimentoPrimeiraParcela: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const iptuKeys = {
  all: ['tributos', 'iptu'] as const,
  imoveis: () => [...iptuKeys.all, 'imoveis'] as const,
  imoveisPorContribuinte: (contribuinteId: string) =>
    [...iptuKeys.imoveis(), 'contribuinte', contribuinteId] as const,
  apuracoes: () => [...iptuKeys.all, 'apuracao'] as const,
  apuracao: (imovelId: string, exercicio: number) =>
    [...iptuKeys.apuracoes(), imovelId, exercicio] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarImoveisPorContribuinte(
  contribuinteId: string,
  signal?: AbortSignal,
): Promise<ImovelResumo[]> {
  return http.get<ImovelResumo[]>(`/tributos/contribuintes/${contribuinteId}/imoveis`, { signal });
}

function cadastrarImovel(input: CadastrarImovelInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/imoveis', input);
}

function publicarPgv(input: PublicarPgvInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/pgv', input);
}

function publicarAliquotas(input: PublicarAliquotasInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/iptu/aliquotas', input);
}

function apurarIptu(imovelId: string, exercicio: number, signal?: AbortSignal): Promise<ApuracaoIptu> {
  return http.get<ApuracaoIptu>(`/tributos/imoveis/${imovelId}/iptu/${exercicio}`, { signal });
}

function lancarIptu(imovelId: string, input: LancarIptuInput): Promise<LancamentoIptuResultado> {
  return http.post<LancamentoIptuResultado>(`/tributos/imoveis/${imovelId}/iptu/lancar`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — QUERIES
// ---------------------------------------------------------------------------

/** Lista os imóveis de um contribuinte. `enabled` controla disparo sob demanda. */
export function useImoveisPorContribuinte(contribuinteId: string, enabled = true) {
  return useQuery({
    queryKey: iptuKeys.imoveisPorContribuinte(contribuinteId),
    queryFn: ({ signal }) => listarImoveisPorContribuinte(contribuinteId, signal),
    enabled: enabled && contribuinteId.trim().length > 0,
  });
}

/** APURA o IPTU de um imóvel num exercício (memória de cálculo). Sob demanda. */
export function useApurarIptu(imovelId: string, exercicio: number, enabled = true) {
  return useQuery({
    queryKey: iptuKeys.apuracao(imovelId, exercicio),
    queryFn: ({ signal }) => apurarIptu(imovelId, exercicio, signal),
    enabled: enabled && imovelId.trim().length > 0 && exercicio > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Cadastra um imóvel vinculado a um contribuinte. Invalida a lista do contribuinte. */
export function useCadastrarImovel(contribuinteIdParaInvalidar?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarImovel,
    onSuccess: () => {
      if (contribuinteIdParaInvalidar && contribuinteIdParaInvalidar.trim().length > 0) {
        queryClient.invalidateQueries({
          queryKey: iptuKeys.imoveisPorContribuinte(contribuinteIdParaInvalidar),
        });
      } else {
        queryClient.invalidateQueries({ queryKey: iptuKeys.imoveis() });
      }
    },
  });
}

/** Publica a Planta Genérica de Valores (zonas VUT/VUC + fatores) de um exercício. */
export function usePublicarPgv() {
  return useMutation({ mutationFn: publicarPgv });
}

/** Publica a tabela de alíquotas (única/progressiva, predial × territorial). */
export function usePublicarAliquotas() {
  return useMutation({ mutationFn: publicarAliquotas });
}

/** Lança o IPTU do imóvel (gera lançamento + DAM parcelado). Invalida a apuração. */
export function useLancarIptu(imovelId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: LancarIptuInput) => lancarIptu(imovelId, input),
    onSuccess: (_resultado, variables) => {
      queryClient.invalidateQueries({ queryKey: iptuKeys.apuracao(imovelId, variables.exercicio) });
    },
  });
}
