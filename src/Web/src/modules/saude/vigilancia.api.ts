// Camada de API da VIGILÂNCIA SANITÁRIA (VISA) — módulo Saúde. DTOs + acesso HTTP +
// hooks TanStack Query. Contrato REAL (SaudeEndpointsVigilancia.cs), sob /saude/vigilancia/*:
//   POST /estabelecimentos                              -> { id }  (saude.vigilancia.gerenciar)
//   GET  /estabelecimentos?termo&ramo&risco&situacao&pagina&tamanho -> ResultadoPaginado (.ver)
//   PUT  /estabelecimentos/{id}/classificacao           -> 204     (.gerenciar)
//   POST /estabelecimentos/{id}/interdicao              -> 204     (.gerenciar)
//   POST /estabelecimentos/{id}/levantamento-interdicao -> 204     (.gerenciar)
//   POST /inspecoes                                     -> { id }  (.inspecionar)
//   GET  /inspecoes/agenda?de&ate&situacao              -> InspecaoDto[] (.ver)
//   GET  /inspecoes/{id}                                -> InspecaoDto  (.ver)
//   POST /inspecoes/{id}/itens                          -> { id }  (.inspecionar)
//   POST /inspecoes/{id}/conclusao                      -> { resultado } (.inspecionar)
//   POST /inspecoes/{id}/cancelamento                   -> 204     (.inspecionar)
//   POST /inspecoes/{inspecaoId}/autos                  -> { id }  (.autuar)
//   GET  /autos?status                                  -> AutoVisaDto[] (.ver)
//   POST /autos/{id}/defesa | /julgamento | /regularizacao -> 204  (.autuar)
//   POST /licencas                                      -> { id }  (.licenciar)
//   GET  /estabelecimentos/{id}/licencas                -> LicencaSanitariaDto[] (.ver)
//   GET  /licencas/a-vencer?dias                        -> LicencaSanitariaDto[] (.ver)
//   POST /licencas/{id}/renovacao                       -> { id }  (.licenciar)
//   POST /licencas/{id}/cassacao                        -> 204     (.licenciar)
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import type { ResultadoPaginado } from './paciente.api';
import type { IdResponse } from './saude.keys';
import { visaKeys } from './vigilancia.keys';

// --- Enums (nomes idênticos ao backend Domain/Vigilancia/Enums.cs) ---

export type RamoVisa =
  | 'Alimentacao'
  | 'Alimentos'
  | 'Farmacia'
  | 'Saude'
  | 'Estetica'
  | 'Hospedagem'
  | 'InstituicaoColetiva'
  | 'Outro';
export type GrauRiscoSanitario = 'Baixo' | 'Medio' | 'Alto';
export type SituacaoEstabelecimentoVisa = 'Ativo' | 'Inativo' | 'Interditado';
export type SituacaoInspecao = 'Aberta' | 'Concluida' | 'Cancelada';
export type ConformidadeItem = 'Conforme' | 'NaoConforme' | 'NaoAplicavel';
export type ResultadoInspecao = 'Aprovado' | 'AprovadoComPendencias' | 'Reprovado';
export type TipoAutoVisa = 'Intimacao' | 'Infracao' | 'ImposicaoPenalidade';
export type SituacaoAutoVisa =
  | 'Lavrado'
  | 'DefesaApresentada'
  | 'Deferido'
  | 'Indeferido'
  | 'Regularizado';
export type SituacaoLicenca = 'Vigente' | 'Vencida' | 'Cassada';

// --- DTOs de leitura (espelham VigilanciaDtos.cs) ---

/** EstabelecimentoFiscalizavelDto. */
export interface EstabelecimentoVisaDto {
  id: string;
  documento: string;
  ehPessoaJuridica: boolean;
  razaoSocial: string;
  ramo: RamoVisa;
  risco: GrauRiscoSanitario;
  situacao: SituacaoEstabelecimentoVisa;
  municipio: string;
}

/** ItemInspecaoDto. */
export interface ItemInspecaoDto {
  requisito: string;
  conformidade: ConformidadeItem;
  observacao: string | null;
}

/** InspecaoDto. */
export interface InspecaoDto {
  id: string;
  estabelecimentoId: string;
  dataInspecao: string;
  fiscalId: string | null;
  roteiro: string | null;
  situacao: SituacaoInspecao;
  resultado: ResultadoInspecao | null;
  pendencias: number;
  itens: ItemInspecaoDto[];
}

/** AutoVisaDto. */
export interface AutoVisaDto {
  id: string;
  estabelecimentoId: string;
  inspecaoId: string;
  tipo: TipoAutoVisa;
  numero: string;
  dataLavratura: string;
  prazoFinal: string;
  valorMulta: number | null;
  situacao: SituacaoAutoVisa;
}

/** LicencaSanitariaDto. */
export interface LicencaSanitariaDto {
  id: string;
  estabelecimentoId: string;
  numero: string;
  emitidaEm: string;
  validadeAte: string;
  situacao: SituacaoLicenca;
}

// --- Filtros e payloads (espelham os Command/Payload do backend) ---

export interface EstabelecimentoVisaFiltro {
  termo?: string;
  ramo?: string;
  risco?: string;
  situacao?: string;
  pagina: number;
  tamanho: number;
}

/** EnderecoVisaDto. */
export interface EnderecoVisaInput {
  logradouro: string;
  bairro: string;
  municipio: string;
  uf: string;
  cep: string;
}

/** CadastrarEstabelecimentoFiscalizavelCommand. */
export interface CadastrarEstabelecimentoVisaInput {
  documento: string;
  razaoSocial: string;
  ramo: RamoVisa;
  risco: GrauRiscoSanitario;
  endereco: EnderecoVisaInput;
}

/** ReclassificarPayload(Ramo, Risco). */
export interface ReclassificarInput {
  ramo: RamoVisa;
  risco: GrauRiscoSanitario;
}

/** AbrirInspecaoCommand(EstabelecimentoId, DataInspecao, FiscalId?, Roteiro?). */
export interface AbrirInspecaoInput {
  estabelecimentoId: string;
  dataInspecao: string;
  fiscalId?: string | null;
  roteiro?: string | null;
}

/** ItemInspecaoPayload(Requisito, Conformidade, Observacao?). */
export interface RegistrarItemInput {
  requisito: string;
  conformidade: ConformidadeItem;
  observacao?: string | null;
}

/** LavrarAutoPayload(EstabelecimentoId, Tipo, Numero, Fundamentacao, PrazoFinal, ValorMulta?). */
export interface LavrarAutoInput {
  estabelecimentoId: string;
  tipo: TipoAutoVisa;
  numero: string;
  fundamentacao: string;
  prazoFinal: string;
  valorMulta?: number | null;
}

/** EmitirLicencaCommand(EstabelecimentoId, Numero, ValidadeAte, InspecaoId?). */
export interface EmitirLicencaInput {
  estabelecimentoId: string;
  numero: string;
  validadeAte: string;
  inspecaoId?: string | null;
}

/** RenovarLicencaPayload(Numero, ValidadeAte, InspecaoId?). */
export interface RenovarLicencaInput {
  numero: string;
  validadeAte: string;
  inspecaoId?: string | null;
}

// --- Acesso HTTP ---

const BASE = '/saude/vigilancia';

function limpar(valor?: string): string | undefined {
  const v = valor?.trim();
  return v && v.length > 0 ? v : undefined;
}

function buscarEstabelecimentos(
  filtro: EstabelecimentoVisaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<EstabelecimentoVisaDto>> {
  return http.get<ResultadoPaginado<EstabelecimentoVisaDto>>(`${BASE}/estabelecimentos`, {
    query: {
      termo: limpar(filtro.termo),
      ramo: limpar(filtro.ramo),
      risco: limpar(filtro.risco),
      situacao: limpar(filtro.situacao),
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

async function cadastrarEstabelecimento(input: CadastrarEstabelecimentoVisaInput): Promise<string> {
  const { id } = await http.post<IdResponse>(`${BASE}/estabelecimentos`, input);
  return id;
}

function reclassificar(id: string, input: ReclassificarInput): Promise<void> {
  return http.put<void>(`${BASE}/estabelecimentos/${id}/classificacao`, input);
}

function interditar(id: string, motivo: string): Promise<void> {
  return http.post<void>(`${BASE}/estabelecimentos/${id}/interdicao`, { motivo });
}

function levantarInterdicao(id: string): Promise<void> {
  return http.post<void>(`${BASE}/estabelecimentos/${id}/levantamento-interdicao`);
}

async function abrirInspecao(input: AbrirInspecaoInput): Promise<string> {
  const { id } = await http.post<IdResponse>(`${BASE}/inspecoes`, input);
  return id;
}

function listarAgenda(
  de: string,
  ate: string,
  situacao: string | undefined,
  signal?: AbortSignal,
): Promise<InspecaoDto[]> {
  return http.get<InspecaoDto[]>(`${BASE}/inspecoes/agenda`, {
    query: { de, ate, situacao: limpar(situacao) },
    signal,
  });
}

function obterInspecao(id: string, signal?: AbortSignal): Promise<InspecaoDto> {
  return http.get<InspecaoDto>(`${BASE}/inspecoes/${id}`, { signal });
}

function registrarItem(inspecaoId: string, input: RegistrarItemInput): Promise<void> {
  return http.post<void>(`${BASE}/inspecoes/${inspecaoId}/itens`, input);
}

async function concluirInspecao(inspecaoId: string, houveInfracaoGrave: boolean): Promise<ResultadoInspecao> {
  const { resultado } = await http.post<{ resultado: ResultadoInspecao }>(
    `${BASE}/inspecoes/${inspecaoId}/conclusao`,
    { houveInfracaoGrave },
  );
  return resultado;
}

function cancelarInspecao(inspecaoId: string, motivo: string): Promise<void> {
  return http.post<void>(`${BASE}/inspecoes/${inspecaoId}/cancelamento`, { motivo });
}

async function lavrarAuto(inspecaoId: string, input: LavrarAutoInput): Promise<string> {
  const { id } = await http.post<IdResponse>(`${BASE}/inspecoes/${inspecaoId}/autos`, input);
  return id;
}

function listarAutos(status: string | undefined, signal?: AbortSignal): Promise<AutoVisaDto[]> {
  return http.get<AutoVisaDto[]>(`${BASE}/autos`, { query: { status: limpar(status) }, signal });
}

function apresentarDefesa(autoId: string, texto: string): Promise<void> {
  return http.post<void>(`${BASE}/autos/${autoId}/defesa`, { texto });
}

function julgarAuto(autoId: string, deferir: boolean): Promise<void> {
  return http.post<void>(`${BASE}/autos/${autoId}/julgamento`, { deferir });
}

function regularizarIntimacao(autoId: string): Promise<void> {
  return http.post<void>(`${BASE}/autos/${autoId}/regularizacao`);
}

async function emitirLicenca(input: EmitirLicencaInput): Promise<string> {
  const { id } = await http.post<IdResponse>(`${BASE}/licencas`, input);
  return id;
}

function listarLicencasDoEstabelecimento(estabId: string, signal?: AbortSignal): Promise<LicencaSanitariaDto[]> {
  return http.get<LicencaSanitariaDto[]>(`${BASE}/estabelecimentos/${estabId}/licencas`, { signal });
}

function listarLicencasAVencer(dias: number | undefined, signal?: AbortSignal): Promise<LicencaSanitariaDto[]> {
  return http.get<LicencaSanitariaDto[]>(`${BASE}/licencas/a-vencer`, { query: { dias }, signal });
}

async function renovarLicenca(licencaId: string, input: RenovarLicencaInput): Promise<string> {
  const { id } = await http.post<IdResponse>(`${BASE}/licencas/${licencaId}/renovacao`, input);
  return id;
}

function cassarLicenca(licencaId: string, motivo: string): Promise<void> {
  return http.post<void>(`${BASE}/licencas/${licencaId}/cassacao`, { motivo });
}

// --- Hooks — QUERIES ---

export function useBuscarEstabelecimentosVisa(filtro: EstabelecimentoVisaFiltro, enabled = true) {
  return useQuery({
    queryKey: visaKeys.estabelecimentosBusca(filtro),
    queryFn: ({ signal }) => buscarEstabelecimentos(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

export function useAgendaInspecoes(de: string, ate: string, situacao?: string) {
  return useQuery({
    queryKey: visaKeys.agenda(de, ate, situacao ?? null),
    queryFn: ({ signal }) => listarAgenda(de, ate, situacao, signal),
    enabled: de.length > 0 && ate.length > 0,
  });
}

export function useInspecao(id: string, enabled = true) {
  return useQuery({
    queryKey: visaKeys.inspecao(id),
    queryFn: ({ signal }) => obterInspecao(id, signal),
    enabled: enabled && id.trim().length > 0,
  });
}

export function useAutos(status?: string) {
  return useQuery({
    queryKey: visaKeys.autos(status ?? null),
    queryFn: ({ signal }) => listarAutos(status, signal),
  });
}

export function useLicencasDoEstabelecimento(estabId: string, enabled = true) {
  return useQuery({
    queryKey: visaKeys.licencasEstab(estabId),
    queryFn: ({ signal }) => listarLicencasDoEstabelecimento(estabId, signal),
    enabled: enabled && estabId.trim().length > 0,
  });
}

export function useLicencasAVencer(dias?: number) {
  return useQuery({
    queryKey: visaKeys.licencasAVencer(dias ?? null),
    queryFn: ({ signal }) => listarLicencasAVencer(dias, signal),
  });
}

// --- Hooks — COMMANDS ---

export function useCadastrarEstabelecimentoVisa() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cadastrarEstabelecimento,
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.estabelecimentos() }),
  });
}

export function useReclassificarEstabelecimento(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ReclassificarInput) => reclassificar(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.estabelecimentos() }),
  });
}

export function useInterditarEstabelecimento(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (motivo: string) => interditar(id, motivo),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.estabelecimentos() }),
  });
}

/** Levanta a interdição; recebe o id do estabelecimento no mutate (uso por linha de tabela). */
export function useLevantarInterdicao() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => levantarInterdicao(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.estabelecimentos() }),
  });
}

export function useAbrirInspecao() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: abrirInspecao,
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.inspecoes() }),
  });
}

export function useRegistrarItem(inspecaoId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarItemInput) => registrarItem(inspecaoId, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.inspecao(inspecaoId) }),
  });
}

export function useConcluirInspecao(inspecaoId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (houveInfracaoGrave: boolean) => concluirInspecao(inspecaoId, houveInfracaoGrave),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.inspecoes() }),
  });
}

export function useCancelarInspecao(inspecaoId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (motivo: string) => cancelarInspecao(inspecaoId, motivo),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.inspecoes() }),
  });
}

export function useLavrarAuto(inspecaoId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: LavrarAutoInput) => lavrarAuto(inspecaoId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: visaKeys.autos() });
      qc.invalidateQueries({ queryKey: visaKeys.inspecao(inspecaoId) });
    },
  });
}

export function useApresentarDefesa(autoId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (texto: string) => apresentarDefesa(autoId, texto),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.autos() }),
  });
}

export function useJulgarAuto(autoId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (deferir: boolean) => julgarAuto(autoId, deferir),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.autos() }),
  });
}

export function useRegularizarIntimacao(autoId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => regularizarIntimacao(autoId),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.autos() }),
  });
}

export function useEmitirLicenca() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: emitirLicenca,
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.licencas() }),
  });
}

export function useRenovarLicenca(licencaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: RenovarLicencaInput) => renovarLicenca(licencaId, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.licencas() }),
  });
}

export function useCassarLicenca(licencaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (motivo: string) => cassarLicenca(licencaId, motivo),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.licencas() }),
  });
}
