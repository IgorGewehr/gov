// Camada de API do agregado Veiculo (módulo Patrimonio — Frota).
// Segue o PADRÃO-OURO de src/modules/tributos/api.ts:
//   1. DTOs (espelham os Commands/Queries reais de PatrimonioEndpoints.cs);
//   2. query keys centralizadas para invalidação consistente;
//   3. funções de acesso usando o http client tipado (Authorization + ProblemDetails);
//   4. hooks TanStack Query (useQuery/useMutation) para TODAS as operações do agregado.
//
// Contrato HTTP real (rotas /api/patrimonio/...):
//   POST   /veiculos                                            -> { id }      IncorporarVeiculo
//   GET    /veiculos/{veiculoId}                                -> VeiculoDetalhe   ObterVeiculo
//   POST   /veiculos/{veiculoId}/abastecimentos                 -> 204         RegistrarAbastecimento
//   GET    /veiculos/{veiculoId}/abastecimentos?de&ate          -> [...]       ListarAbastecimentosDoVeiculo
//   POST   /veiculos/{veiculoId}/ordens-servico                 -> { id }      AbrirOrdemServico
//   POST   /veiculos/{veiculoId}/ordens-servico/{osId}/conclusao-> 204         ConcluirManutencao
//   POST   /veiculos/{veiculoId}/multas                         -> 204         RegistrarMulta
//   GET    /veiculos/multas-pendentes                           -> [...]       ListarMultasPendentes
//   POST   /veiculos/{veiculoId}/licenciamentos                 -> 204         RegistrarLicenciamento
//   GET    /veiculos/licenciamentos-pendentes?exercicio         -> [...]       ListarLicenciamentosPendentes
//   POST   /veiculos/{veiculoId}/motoristas                     -> 204         DesignarMotorista
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import type { ResultadoPaginado } from '../shared/paginacaoTipos';

// ---------------------------------------------------------------------------
// DTOs — leitura (Queries)
// ---------------------------------------------------------------------------

/** Situação própria do veículo (enum SituacaoBemPatrimonial usado pelo agregado Veiculo). */
export type SituacaoVeiculo = 'EmIncorporacao' | 'Tombado' | 'Cedido' | 'Baixada' | 'Alienada';

/** Projeção de ObterVeiculoQuery -> VeiculoDetalhe. */
export interface VeiculoDetalhe {
  id: string;
  numeroTombamento: string | null;
  placa: string;
  renavam: string;
  odometro: number;
  horimetro: number;
  valorContabil: number;
  situacao: SituacaoVeiculo;
  motoristaAtualId: string | null;
}

/**
 * Item da LISTA NAVEGÁVEL de veículos (Onda 0 — Navegabilidade).
 * Projeção de GET /patrimonio/veiculos?termo&situacao&pagina&tamanho -> VeiculoItemLista.
 * Busca casa por trecho em descrição, placa e RENAVAM.
 */
export interface VeiculoItemLista {
  id: string;
  placa: string;
  renavam: string;
  descricao: string;
  numeroTombamento: string | null;
  odometro: number;
  valorContabil: number;
  situacao: SituacaoVeiculo;
}

/** Filtros da lista navegável de veículos. */
export interface VeiculoFiltro {
  termo?: string;
  situacao?: SituacaoVeiculo;
  pagina: number;
  tamanho: number;
}

/** Projeção de ListarAbastecimentosDoVeiculoQuery -> AbastecimentoResumo. */
export interface AbastecimentoResumo {
  id: string;
  data: string;
  litros: number;
  valor: number;
  odometro: number;
}

/** Projeção de ListarMultasPendentesQuery -> MultaResumo. */
export interface MultaResumo {
  id: string;
  veiculoId: string;
  placa: string;
  codigoInfracaoCtb: string;
  valor: number;
  dataInfracao: string;
}

/** Projeção de ListarLicenciamentosPendentesQuery -> LicenciamentoResumo. */
export interface LicenciamentoResumo {
  veiculoId: string;
  placa: string;
  exercicio: number;
  valorIpva: number;
  situacao: string;
}

// ---------------------------------------------------------------------------
// DTOs — escrita (Commands)
// ---------------------------------------------------------------------------

/** Corpo de POST /veiculos (IncorporarVeiculoCommand). */
export interface IncorporarVeiculoInput {
  descricao: string;
  valorInicial: number;
  valorResidual: number;
  vidaUtilMeses: number;
  dataIncorporacao: string;
  origem: string;
  placa: string;
  renavam: string;
  odometroInicial: number;
  horimetroInicial: number;
}

/** Corpo de POST /veiculos/{id}/abastecimentos (RegistrarAbastecimentoPayload). */
export interface RegistrarAbastecimentoInput {
  data: string;
  litros: number;
  valor: number;
  odometro: number;
  horimetro: number;
  motoristaId?: string | null;
}

/** Corpo de POST /veiculos/{id}/ordens-servico (AbrirOrdemServicoPayload). */
export interface AbrirOrdemServicoInput {
  descricao: string;
  custoEstimado: number;
  odometro: number;
}

/** Corpo de POST /veiculos/{id}/ordens-servico/{osId}/conclusao (ConcluirManutencaoPayload). */
export interface ConcluirManutencaoInput {
  ordemServicoId: string;
  custoRealizado: number;
  dataConclusao: string;
}

/** Corpo de POST /veiculos/{id}/multas (RegistrarMultaPayload). */
export interface RegistrarMultaInput {
  codigoInfracaoCtb: string;
  valor: number;
  dataInfracao: string;
  motoristaId?: string | null;
}

/** Corpo de POST /veiculos/{id}/licenciamentos (RegistrarLicenciamentoPayload). */
export interface RegistrarLicenciamentoInput {
  exercicio: number;
  valorIpva: number;
  valorTaxa: number;
  data: string;
}

/** Corpo de POST /veiculos/{id}/motoristas (DesignarMotoristaPayload). */
export interface DesignarMotoristaInput {
  nome: string;
  cnh: string;
  categoriaCnh: string;
  validadeCnh: string;
}

/** Resposta de criação ({ id }). */
interface CriacaoResposta {
  id: string;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const veiculoKeys = {
  all: ['patrimonio', 'veiculos'] as const,
  detalhe: (veiculoId: string) => [...veiculoKeys.all, 'detalhe', veiculoId] as const,
  lista: (filtro: VeiculoFiltro) =>
    [
      ...veiculoKeys.all,
      'lista',
      filtro.termo ?? '',
      filtro.situacao ?? '',
      filtro.pagina,
      filtro.tamanho,
    ] as const,
  abastecimentos: (veiculoId: string, de: string, ate: string) =>
    [...veiculoKeys.all, 'abastecimentos', veiculoId, de, ate] as const,
  multasPendentes: () => [...veiculoKeys.all, 'multas-pendentes'] as const,
  licenciamentosPendentes: (exercicio: number) =>
    [...veiculoKeys.all, 'licenciamentos-pendentes', exercicio] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP — Queries
// ---------------------------------------------------------------------------

function obterVeiculo(veiculoId: string, signal?: AbortSignal): Promise<VeiculoDetalhe> {
  return http.get<VeiculoDetalhe>(`/patrimonio/veiculos/${veiculoId}`, { signal });
}

function listarVeiculos(
  filtro: VeiculoFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<VeiculoItemLista>> {
  return http.get<ResultadoPaginado<VeiculoItemLista>>('/patrimonio/veiculos', {
    query: {
      termo: filtro.termo,
      situacao: filtro.situacao,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function listarAbastecimentos(
  veiculoId: string,
  de: string,
  ate: string,
  signal?: AbortSignal,
): Promise<AbastecimentoResumo[]> {
  return http.get<AbastecimentoResumo[]>(`/patrimonio/veiculos/${veiculoId}/abastecimentos`, {
    query: { de, ate },
    signal,
  });
}

function listarMultasPendentes(signal?: AbortSignal): Promise<MultaResumo[]> {
  return http.get<MultaResumo[]>('/patrimonio/veiculos/multas-pendentes', { signal });
}

function listarLicenciamentosPendentes(exercicio: number, signal?: AbortSignal): Promise<LicenciamentoResumo[]> {
  return http.get<LicenciamentoResumo[]>('/patrimonio/veiculos/licenciamentos-pendentes', {
    query: { exercicio },
    signal,
  });
}

// ---------------------------------------------------------------------------
// Acesso HTTP — Commands
// ---------------------------------------------------------------------------

function incorporarVeiculo(input: IncorporarVeiculoInput): Promise<CriacaoResposta> {
  return http.post<CriacaoResposta>('/patrimonio/veiculos', input);
}

function registrarAbastecimento(veiculoId: string, input: RegistrarAbastecimentoInput): Promise<void> {
  return http.post<void>(`/patrimonio/veiculos/${veiculoId}/abastecimentos`, input);
}

function abrirOrdemServico(veiculoId: string, input: AbrirOrdemServicoInput): Promise<CriacaoResposta> {
  return http.post<CriacaoResposta>(`/patrimonio/veiculos/${veiculoId}/ordens-servico`, input);
}

function concluirManutencao(veiculoId: string, input: ConcluirManutencaoInput): Promise<void> {
  return http.post<void>(
    `/patrimonio/veiculos/${veiculoId}/ordens-servico/${input.ordemServicoId}/conclusao`,
    { custoRealizado: input.custoRealizado, dataConclusao: input.dataConclusao },
  );
}

function registrarMulta(veiculoId: string, input: RegistrarMultaInput): Promise<void> {
  return http.post<void>(`/patrimonio/veiculos/${veiculoId}/multas`, input);
}

function registrarLicenciamento(veiculoId: string, input: RegistrarLicenciamentoInput): Promise<void> {
  return http.post<void>(`/patrimonio/veiculos/${veiculoId}/licenciamentos`, input);
}

function designarMotorista(veiculoId: string, input: DesignarMotoristaInput): Promise<void> {
  return http.post<void>(`/patrimonio/veiculos/${veiculoId}/motoristas`, input);
}

// ---------------------------------------------------------------------------
// Hooks — Queries
// ---------------------------------------------------------------------------

/**
 * LISTA NAVEGÁVEL de veículos (Onda 0) — busca por descrição/placa/RENAVAM + filtro
 * de situação, paginada. Mantém os dados anteriores ao paginar/filtrar.
 */
export function useVeiculosLista(filtro: VeiculoFiltro) {
  return useQuery({
    queryKey: veiculoKeys.lista(filtro),
    queryFn: ({ signal }) => listarVeiculos(filtro, signal),
    placeholderData: (anterior) => anterior,
  });
}

/** Detalhe de um veículo (ObterVeiculo). */
export function useVeiculo(veiculoId: string) {
  return useQuery({
    queryKey: veiculoKeys.detalhe(veiculoId),
    queryFn: ({ signal }) => obterVeiculo(veiculoId, signal),
    enabled: veiculoId.trim().length > 0,
  });
}

/** Abastecimentos do veículo num período (ListarAbastecimentosDoVeiculo). */
export function useAbastecimentosDoVeiculo(veiculoId: string, de: string, ate: string, enabled = true) {
  return useQuery({
    queryKey: veiculoKeys.abastecimentos(veiculoId, de, ate),
    queryFn: ({ signal }) => listarAbastecimentos(veiculoId, de, ate, signal),
    enabled: enabled && veiculoId.trim().length > 0 && de !== '' && ate !== '',
  });
}

/** Multas pendentes/em recurso de todos os veículos (ListarMultasPendentes). */
export function useMultasPendentes() {
  return useQuery({
    queryKey: veiculoKeys.multasPendentes(),
    queryFn: ({ signal }) => listarMultasPendentes(signal),
  });
}

/** Veículos sem licenciamento Regular num exercício (ListarLicenciamentosPendentes). */
export function useLicenciamentosPendentes(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: veiculoKeys.licenciamentosPendentes(exercicio),
    queryFn: ({ signal }) => listarLicenciamentosPendentes(exercicio, signal),
    enabled: enabled && Number.isInteger(exercicio) && exercicio > 0,
  });
}

// ---------------------------------------------------------------------------
// Hooks — Commands
// ---------------------------------------------------------------------------

/** Incorpora um novo veículo à frota (IncorporarVeiculo). */
export function useIncorporarVeiculo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: incorporarVeiculo,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: veiculoKeys.all });
    },
  });
}

/** Registra um abastecimento e invalida o detalhe + listas afetadas (RegistrarAbastecimento). */
export function useRegistrarAbastecimento(veiculoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarAbastecimentoInput) => registrarAbastecimento(veiculoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: veiculoKeys.detalhe(veiculoId) });
      queryClient.invalidateQueries({ queryKey: veiculoKeys.all });
    },
  });
}

/** Abre uma ordem de serviço de manutenção (AbrirOrdemServico). */
export function useAbrirOrdemServico(veiculoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AbrirOrdemServicoInput) => abrirOrdemServico(veiculoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: veiculoKeys.detalhe(veiculoId) });
    },
  });
}

/** Conclui uma ordem de serviço aberta (ConcluirManutencao). */
export function useConcluirManutencao(veiculoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ConcluirManutencaoInput) => concluirManutencao(veiculoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: veiculoKeys.detalhe(veiculoId) });
    },
  });
}

/** Registra uma multa de trânsito (RegistrarMulta). */
export function useRegistrarMulta(veiculoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarMultaInput) => registrarMulta(veiculoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: veiculoKeys.detalhe(veiculoId) });
      queryClient.invalidateQueries({ queryKey: veiculoKeys.multasPendentes() });
    },
  });
}

/** Registra o licenciamento/IPVA de um exercício (RegistrarLicenciamento). */
export function useRegistrarLicenciamento(veiculoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarLicenciamentoInput) => registrarLicenciamento(veiculoId, input),
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: veiculoKeys.detalhe(veiculoId) });
      queryClient.invalidateQueries({ queryKey: veiculoKeys.licenciamentosPendentes(input.exercicio) });
    },
  });
}

/** Designa o motorista atual do veículo (DesignarMotorista). */
export function useDesignarMotorista(veiculoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: DesignarMotoristaInput) => designarMotorista(veiculoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: veiculoKeys.detalhe(veiculoId) });
    },
  });
}
