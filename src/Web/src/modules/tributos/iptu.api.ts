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

/** Uso predominante do imóvel — enum TipoUsoImovel (back). */
export type UsoImovel =
  | 'Residencial'
  | 'Comercial'
  | 'Industrial'
  | 'Servicos'
  | 'Misto'
  | 'Territorial';

/** Valores INT do enum TipoUsoImovel (back: 1..6). */
export const USO_IMOVEL_VALOR: Record<UsoImovel, number> = {
  Residencial: 1,
  Comercial: 2,
  Industrial: 3,
  Servicos: 4,
  Misto: 5,
  Territorial: 6,
};

/**
 * Padrão construtivo do imóvel. NO BACKEND É UMA STRING LIVRE (NotEmpty,
 * MaxLength 30), não um enum — o cadastro envia o texto (ex.: "Normal").
 */
export type PadraoConstrutivo = 'Baixo' | 'Normal' | 'Alto' | 'Luxo';

// ---------------------------------------------------------------------------
// Projeções de leitura
// ---------------------------------------------------------------------------

/** Projeção de resumo (ObterImoveisDoContribuinte) — espelha ImovelResumo (back). */
export interface ImovelResumo {
  id: string;
  inscricaoMunicipal: string;
  cibCodigo: string | null;
  logradouro: string;
  zonaFiscal: string;
  areaTerreno: number;
  areaConstruida: number;
  /** Tipo de uso (back: TipoUso, string). */
  tipoUso: UsoImovel;
  ativo: boolean;
}

/**
 * Projeção da APURAÇÃO de IPTU (GET .../iptu/{exercicio}) — espelha ResultadoIptu
 * (achatado; o backend NÃO devolve memória de cálculo). `aliquotaPercentual` em %
 * (ex.: 1.0 = 1%).
 */
export interface ApuracaoIptu {
  imovelId: string;
  exercicio: number;
  valorVenal: number;
  valorTerreno: number;
  valorConstrucao: number;
  aliquotaPercentual: number;
  impostoBruto: number;
  valorIsencao: number;
  valorDesconto: number;
  impostoDevido: number;
}

/** Resultado do LANÇAMENTO de IPTU (POST .../iptu/lancar) — espelha ResultadoLancamentoIptu. */
export interface LancamentoIptuResultado {
  lancamentoId: string;
  damId: string;
  impostoDevido: number;
}

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** CadastrarImovelCommand — bind direto (camelCase do record do backend). */
export interface CadastrarImovelInput {
  /** Contribuinte proprietário/possuidor (back: ProprietarioId). */
  proprietarioId: string;
  /** Inscrição municipal cadastral (back: InscricaoMunicipal). */
  inscricaoMunicipal: string;
  logradouro: string;
  numero?: string | null;
  bairro: string;
  /** Setor/quadra/lote (back: SetorQuadraLote, obrigatório). */
  setorQuadraLote: string;
  /** Zona fiscal da PGV (back: ZonaFiscal). */
  zonaFiscal: string;
  /** Tipo de uso (back: TipoUso, enum int). */
  tipoUso: number;
  /** Padrão construtivo (back: PadraoConstrutivo) — STRING livre, não int. */
  padraoConstrutivo: string;
  anoConstrucao?: number | null;
  areaTerreno: number;
  areaConstruida: number;
}

/** Zona da PGV (back: ZonaPgvInput) — VUT por m² de terreno; VUC por m² construído. */
export interface ZonaPgvInput {
  zonaFiscal: string;
  valorM2Terreno: number;
  valorM2Construcao: number;
}

/** Fator de correção da PGV (back: FatorPgvInput). */
export interface FatorPgvInput {
  /** Categoria do fator (back: Tipo — TipoFatorPgv, int). */
  tipo: number;
  chave: string;
  /** Multiplicador (> 0) (back: Multiplicador). */
  multiplicador: number;
}

/** ConfigurarPlantaValoresCommand. */
export interface PublicarPgvInput {
  exercicio: number;
  /** Lei/decreto municipal que institui a PGV (back: FundamentoLegal, obrigatório). */
  fundamentoLegal: string;
  zonas: ZonaPgvInput[];
  fatores: FatorPgvInput[];
  publicar?: boolean;
}

/** Faixa de progressividade (back: FaixaAliquotaInput). `aliquotaPercentual` em % (1.0 = 1%). */
export interface FaixaAliquotaInput {
  valorVenalMinimo: number;
  valorVenalMaximo: number;
  aliquotaPercentual: number;
}

/**
 * ConfigurarTabelaAliquotaIptuCommand — UMA tabela por chamada (`edificado`
 * define predial × territorial). Chamar duas vezes p/ cobrir ambas.
 */
export interface PublicarAliquotasInput {
  exercicio: number;
  /** Tabela predial (true) ou territorial (false) (back: Edificado). */
  edificado: boolean;
  /** Lei municipal de alíquotas (back: FundamentoLegal, obrigatório). */
  fundamentoLegal: string;
  faixas: FaixaAliquotaInput[];
  publicar?: boolean;
}

/** LancarIptuPayload (apura e gera lançamento + DAM). */
export interface LancarIptuInput {
  exercicio: number;
  /** Vencimento da cota única / 1ª parcela (back: PrimeiroVencimento, "yyyy-MM-dd"). */
  primeiroVencimento: string;
  /** Número de parcelas (back: NumeroParcelas, default 1). */
  numeroParcelas: number;
  /** Percentual de isenção (back: PercentualIsencao, default 0). */
  percentualIsencao?: number;
  /** Percentual de desconto (back: PercentualDesconto, default 0). */
  percentualDesconto?: number;
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
