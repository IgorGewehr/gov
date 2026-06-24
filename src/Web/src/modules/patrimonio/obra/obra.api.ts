// Camada de API das OBRAS (módulo Patrimonio — W9.3, Lei 14.133/2021). Segue o
// PADRÃO-OURO do módulo (requisicao.api.ts): DTOs -> query keys -> funções http
// tipadas -> hooks TanStack Query. Rotas REAIS em PatrimonioEndpoints.Obras.cs:
//   POST /api/patrimonio/obras                                  (AbrirObraCommand)            -> { id }
//   GET  /api/patrimonio/obras?termo&situacao&pagina&tamanho    (BuscarObras)                 -> ResultadoPaginado<ObraLinha>
//   GET  /api/patrimonio/obras/{obraId}                         (ObterObra)                   -> ObraDetalhe
//   POST /api/patrimonio/obras/{obraId}/cronograma             (DefinirCronogramaPayload)    -> 204
//   POST /api/patrimonio/obras/{obraId}/ordem-inicio           (OrdemInicioPayload)          -> 204
//   POST /api/patrimonio/obras/{obraId}/fiscal                 (DesignarFiscalPayload)       -> 204
//   POST /api/patrimonio/obras/{obraId}/rdos                   (RdoPayload)                  -> { id }
//   POST /api/patrimonio/obras/{obraId}/medicoes              (MedicaoPayload)              -> { id }
//   POST /api/patrimonio/obras/{obraId}/medicoes/{id}/aprovacao (AprovarMedicaoPayload)      -> 204
//   POST /api/patrimonio/obras/{obraId}/medicoes/{id}/rejeicao  (MotivoPayload)              -> 204
//   POST /api/patrimonio/obras/{obraId}/ocorrencias           (OcorrenciaPayload)           -> { id }
//   POST /api/patrimonio/obras/{obraId}/paralisacao           (ParalisacaoPayload)          -> 204
//   POST /api/patrimonio/obras/{obraId}/reinicio              (DataPayload)                 -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import type { ResultadoPaginado } from '../shared/paginacaoTipos';

// ---------------------------------------------------------------------------
// Enums de domínio (numéricos no contrato; nome PT-BR via ToString() na leitura)
// ---------------------------------------------------------------------------

/** SituacaoObra: Planejada=1, EmExecucao=2, Paralisada=3, Concluida=4, Incorporada=5, Rescindida=6. */
export const SITUACAO_OBRA = {
  Planejada: 1,
  EmExecucao: 2,
  Paralisada: 3,
  Concluida: 4,
  Incorporada: 5,
  Rescindida: 6,
} as const;
export type SituacaoObraValor = (typeof SITUACAO_OBRA)[keyof typeof SITUACAO_OBRA];
export type SituacaoObraNome =
  | 'Planejada'
  | 'EmExecucao'
  | 'Paralisada'
  | 'Concluida'
  | 'Incorporada'
  | 'Rescindida';

/** RegimeExecucao (art. 46): 1..6. */
export const REGIME_EXECUCAO = {
  EmpreitadaPorPrecoUnitario: 1,
  EmpreitadaPorPrecoGlobal: 2,
  Tarefa: 3,
  EmpreitadaIntegral: 4,
  ContratacaoIntegrada: 5,
  ContratacaoSemiIntegrada: 6,
} as const;
export type RegimeExecucaoValor = (typeof REGIME_EXECUCAO)[keyof typeof REGIME_EXECUCAO];

/** TipoOcorrenciaFiscalizacao (art. 117): Notificacao=1, Advertencia=2, RegistroTecnico=3. */
export const TIPO_OCORRENCIA = {
  Notificacao: 1,
  Advertencia: 2,
  RegistroTecnico: 3,
} as const;
export type TipoOcorrenciaValor = (typeof TIPO_OCORRENCIA)[keyof typeof TIPO_OCORRENCIA];

/** MotivoParalisacao: OrdemFiscalizacao=1, FaltaProjeto=2, Clima=3, Orcamentaria=4, Judicial=5. */
export const MOTIVO_PARALISACAO = {
  OrdemFiscalizacao: 1,
  FaltaProjeto: 2,
  Clima: 3,
  Orcamentaria: 4,
  Judicial: 5,
} as const;
export type MotivoParalisacaoValor =
  (typeof MOTIVO_PARALISACAO)[keyof typeof MOTIVO_PARALISACAO];

// ---------------------------------------------------------------------------
// DTOs de leitura (projeções dos handlers de Query)
// ---------------------------------------------------------------------------

/** ObraLinha — projeção enxuta de BuscarObrasQuery (lista/tabela). */
export interface ObraLinha {
  id: string;
  objeto: string;
  municipio: string;
  uf: string;
  valorContratado: number;
  percentualFisicoAcumulado: number;
  situacao: SituacaoObraNome;
}

/** EtapaCronogramaDto — etapa do cronograma físico-financeiro (curva S). */
export interface EtapaCronogramaDto {
  id: string;
  ordem: number;
  descricao: string;
  percentualFisicoPrevisto: number;
  valorPrevisto: number;
  percentualFisicoExecutado: number;
  valorMedido: number;
  situacao: 'Prevista' | 'EmAndamento' | 'Concluida';
}

/** MedicaoDto — boletim de medição periódica. */
export interface MedicaoDto {
  id: string;
  numero: number;
  competenciaAno: number;
  competenciaMes: number;
  periodoInicio: string;
  periodoFim: string;
  valorMedido: number;
  percentualFisicoNoPeriodo: number;
  situacao: 'Rascunho' | 'Aprovada' | 'Rejeitada';
  dataAprovacao?: string | null;
}

/** RdoDto — registro diário de obra. */
export interface RdoDto {
  id: string;
  data: string;
  condicaoTempo: string;
  efetivoMaoDeObra: number;
  atividadesExecutadas: string;
  ocorrencias?: string | null;
}

/** OcorrenciaDto — ocorrência da fiscalização (art. 117). */
export interface OcorrenciaDto {
  id: string;
  data: string;
  tipo: 'Notificacao' | 'Advertencia' | 'RegistroTecnico';
  descricao: string;
}

/** ObraDetalhe — ficha completa (ObterObraQuery). */
export interface ObraDetalhe {
  id: string;
  contratoId: string;
  fornecedorId: string;
  bemPatrimonialId?: string | null;
  objeto: string;
  municipio: string;
  uf: string;
  regimeExecucao: string;
  valorContratado: number;
  valorMedidoAcumulado: number;
  percentualFisicoAcumulado: number;
  situacao: SituacaoObraNome;
  dataAssinaturaContrato: string;
  dataInicioOrdemServico?: string | null;
  dataConclusao?: string | null;
  fiscalDesignadoId?: string | null;
  etapas: EtapaCronogramaDto[];
  medicoes: MedicaoDto[];
  registrosDiarios: RdoDto[];
  ocorrencias: OcorrenciaDto[];
}

/** Filtros da lista de obras. */
export interface ObraFiltro {
  termo?: string;
  situacao?: SituacaoObraValor;
  pagina: number;
  tamanho: number;
}

// ---------------------------------------------------------------------------
// DTOs de escrita (Commands / Payloads — espelham os records do endpoint)
// ---------------------------------------------------------------------------

/** AbrirObraCommand (corpo do POST /obras). */
export interface AbrirObraInput {
  contratoId: string;
  fornecedorId: string;
  objeto: string;
  municipio: string;
  uf: string;
  logradouro?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  geoCodigo?: string | null;
  regimeExecucao: RegimeExecucaoValor;
  valorContratado: number;
  dataAssinaturaContrato: string;
}

/** EtapaCronogramaInput — etapa do cronograma (DefinirCronogramaPayload.Etapas). */
export interface EtapaCronogramaInput {
  ordem: number;
  descricao: string;
  percentualFisicoPrevisto: number;
  valorPrevisto: number;
  dataPrevistaInicio: string;
  dataPrevistaFim: string;
}

/** AvancoEtapaInput — avanço de etapa no período (MedicaoPayload.Avancos). */
export interface AvancoEtapaInput {
  etapaId: string;
  percentualFisicoNoPeriodo: number;
  valorNoPeriodo: number;
}

/** MedicaoPayload. */
export interface RegistrarMedicaoInput {
  competenciaAno: number;
  competenciaMes: number;
  periodoInicio: string;
  periodoFim: string;
  avancos: AvancoEtapaInput[];
}

/** RdoPayload. */
export interface RegistrarRdoInput {
  data: string;
  condicaoTempo: string;
  efetivoMaoDeObra: number;
  equipamentosMobilizados: string;
  atividadesExecutadas: string;
  responsavelTecnicoId: string;
  ocorrencias?: string | null;
}

/** AprovarMedicaoPayload. */
export interface AprovarMedicaoInput {
  fiscalId: string;
  dataAprovacao: string;
}

/** OcorrenciaPayload. */
export interface RegistrarOcorrenciaInput {
  data: string;
  tipo: TipoOcorrenciaValor;
  descricao: string;
  registradaPorId: string;
}

/** ParalisacaoPayload. */
export interface ParalisarObraInput {
  motivo: MotivoParalisacaoValor;
  data: string;
}

interface CriadoResponse {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys
// ---------------------------------------------------------------------------

export const obraKeys = {
  all: ['patrimonio', 'obra'] as const,
  detalhe: (id: string) => [...obraKeys.all, 'detalhe', id] as const,
  lista: (filtro: ObraFiltro) =>
    [
      ...obraKeys.all,
      'lista',
      filtro.termo ?? '',
      filtro.situacao ?? '',
      filtro.pagina,
      filtro.tamanho,
    ] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function buscarObras(
  filtro: ObraFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<ObraLinha>> {
  return http.get<ResultadoPaginado<ObraLinha>>('/patrimonio/obras', {
    query: {
      termo: filtro.termo,
      situacao: filtro.situacao,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function obterObra(obraId: string, signal?: AbortSignal): Promise<ObraDetalhe> {
  return http.get<ObraDetalhe>(`/patrimonio/obras/${obraId}`, { signal });
}

function abrirObra(input: AbrirObraInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/patrimonio/obras', input);
}

function definirCronograma(obraId: string, etapas: EtapaCronogramaInput[]): Promise<void> {
  return http.post<void>(`/patrimonio/obras/${obraId}/cronograma`, { etapas });
}

function emitirOrdemInicio(obraId: string, data: string): Promise<void> {
  return http.post<void>(`/patrimonio/obras/${obraId}/ordem-inicio`, { data });
}

function registrarRdo(obraId: string, input: RegistrarRdoInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>(`/patrimonio/obras/${obraId}/rdos`, input);
}

function registrarMedicao(
  obraId: string,
  input: RegistrarMedicaoInput,
): Promise<CriadoResponse> {
  return http.post<CriadoResponse>(`/patrimonio/obras/${obraId}/medicoes`, input);
}

function aprovarMedicao(
  obraId: string,
  medicaoId: string,
  input: AprovarMedicaoInput,
): Promise<void> {
  return http.post<void>(`/patrimonio/obras/${obraId}/medicoes/${medicaoId}/aprovacao`, input);
}

function rejeitarMedicao(obraId: string, medicaoId: string, motivo: string): Promise<void> {
  return http.post<void>(`/patrimonio/obras/${obraId}/medicoes/${medicaoId}/rejeicao`, { motivo });
}

function designarFiscal(
  obraId: string,
  fiscalId: string,
  desde: string,
  atoDesignacao: string,
): Promise<void> {
  return http.post<void>(`/patrimonio/obras/${obraId}/fiscal`, { fiscalId, desde, atoDesignacao });
}

function registrarOcorrencia(
  obraId: string,
  input: RegistrarOcorrenciaInput,
): Promise<CriadoResponse> {
  return http.post<CriadoResponse>(`/patrimonio/obras/${obraId}/ocorrencias`, input);
}

function paralisarObra(obraId: string, input: ParalisarObraInput): Promise<void> {
  return http.post<void>(`/patrimonio/obras/${obraId}/paralisacao`, input);
}

function reiniciarObra(obraId: string, data: string): Promise<void> {
  return http.post<void>(`/patrimonio/obras/${obraId}/reinicio`, { data });
}

// ---------------------------------------------------------------------------
// Hooks — Consultas
// ---------------------------------------------------------------------------

/** Lista paginada de obras por objeto/município, com filtro por situação. */
export function useObrasLista(filtro: ObraFiltro) {
  return useQuery({
    queryKey: obraKeys.lista(filtro),
    queryFn: ({ signal }) => buscarObras(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** Ficha (detalhe) de uma obra por Id. */
export function useObra(id: string) {
  return useQuery({
    queryKey: obraKeys.detalhe(id),
    queryFn: ({ signal }) => obterObra(id, signal),
    enabled: id.trim().length > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks — Comandos
// ---------------------------------------------------------------------------

/** AbrirObra (Planejada); invalida a lista. */
export function useAbrirObra() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: abrirObra,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...obraKeys.all, 'lista'] });
    },
  });
}

/** DefinirCronograma (curva S — I-3); invalida o detalhe e a lista. */
export function useDefinirCronograma(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (etapas: EtapaCronogramaInput[]) => definirCronograma(obraId, etapas),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** EmitirOrdemInicio (Planejada -> EmExecucao). */
export function useEmitirOrdemInicio(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: string) => emitirOrdemInicio(obraId, data),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** RegistrarRdo (fiscalização contínua — I-8/I-9). */
export function useRegistrarRdo(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarRdoInput) => registrarRdo(obraId, input),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** RegistrarMedicao (boletim — rascunho). */
export function useRegistrarMedicao(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarMedicaoInput) => registrarMedicao(obraId, input),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** AprovarMedicao (libera liquidação em Finanças — I-1/I-8/I-10). */
export function useAprovarMedicao(obraId: string, medicaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AprovarMedicaoInput) => aprovarMedicao(obraId, medicaoId, input),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** RejeitarMedicao (não compõe o medido acumulado). */
export function useRejeitarMedicao(obraId: string, medicaoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (motivo: string) => rejeitarMedicao(obraId, medicaoId, motivo),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** DesignarFiscal (art. 117 / I-10). */
export function useDesignarFiscal(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { fiscalId: string; desde: string; atoDesignacao: string }) =>
      designarFiscal(obraId, input.fiscalId, input.desde, input.atoDesignacao),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** RegistrarOcorrencia (fiscalização). */
export function useRegistrarOcorrencia(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarOcorrenciaInput) => registrarOcorrencia(obraId, input),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** ParalisarObra (EmExecucao -> Paralisada — I-15). */
export function useParalisarObra(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ParalisarObraInput) => paralisarObra(obraId, input),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

/** ReiniciarObra (Paralisada -> EmExecucao). */
export function useReiniciarObra(obraId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: string) => reiniciarObra(obraId, data),
    onSuccess: () => invalidarObra(queryClient, obraId),
  });
}

function invalidarObra(queryClient: ReturnType<typeof useQueryClient>, obraId: string): void {
  queryClient.invalidateQueries({ queryKey: obraKeys.detalhe(obraId) });
  queryClient.invalidateQueries({ queryKey: [...obraKeys.all, 'lista'] });
}
