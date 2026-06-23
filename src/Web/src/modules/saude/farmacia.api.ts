// Camada de API da FARMÁCIA (HÓRUS) — módulo Saúde. DTOs + acesso HTTP + hooks TanStack.
// Contrato REAL (SaudeEndpointsFarmaciaImunizacao.cs — grupo /api/saude; o http client já prefixa /api):
//   POST /saude/farmacia/medicamentos                                  -> CadastrarMedicamento -> { id }  (saude.farmacia.gerenciar)
//   GET  /saude/farmacia/medicamentos?termo&apenasAtivos&pagina&tamanho -> ResultadoPaginado<MedicamentoItemLista> (saude.farmacia.ver)
//   POST /saude/farmacia/estoque/{estabId}/{medId}/entradas {numeroLote,validade,quantidade,pontoDeRessuprimento} -> { id } (saude.farmacia.gerenciar)
//   GET  /saude/farmacia/estoque?estabId                               -> PosicaoEstoqueDto[]              (saude.farmacia.ver)
//   GET  /saude/farmacia/alertas/validade?dias                         -> AlertaValidadeDto[]              (saude.farmacia.ver)
//   POST /saude/farmacia/dispensacoes {pacienteId,estabelecimentoId,profissionalId,prescricaoId?,itens[]} -> { id } (saude.farmacia.dispensar)
//   POST /saude/farmacia/dispensacoes/{id}/estorno {motivo}            -> 204                              (saude.farmacia.dispensar)
//   GET  /saude/farmacia/pacientes/{pacienteId}/dispensacoes?de&ate    -> DispensacaoDto[]  (SENSÍVEL/LGPD) (saude.prontuario.ler)
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';
import type { ResultadoPaginado } from './paciente.api';

// --- DTOs ---

/** Item do catálogo de medicamentos (MedicamentoItemLista). */
export interface MedicamentoItemLista {
  id: string;
  principioAtivo: string;
  apresentacao: string;
  concentracao: string;
  forma: string;
  unidade: string;
  controle: string;
  exigeReceitaControlada: boolean;
  ativo: boolean;
}

/** Lote na posição de estoque (LoteDto). */
export interface LoteDto {
  numeroLote: string;
  validade: string;
  saldo: number;
  vencido: boolean;
}

/** Posição de estoque de um medicamento num estabelecimento (PosicaoEstoqueDto). */
export interface PosicaoEstoqueDto {
  estoqueId: string;
  estabelecimentoId: string;
  medicamentoId: string;
  principioAtivo: string;
  saldo: number;
  saldoValido: number;
  pontoDeRessuprimento: number;
  emRuptura: boolean;
  lotes: LoteDto[];
}

/** Alerta de lote a vencer/vencido (AlertaValidadeDto). */
export interface AlertaValidadeDto {
  estabelecimentoId: string;
  medicamentoId: string;
  principioAtivo: string;
  numeroLote: string;
  validade: string;
  saldo: number;
  vencido: boolean;
}

/** Item entregue numa dispensação (ItemDispensadoDto). */
export interface ItemDispensadoDto {
  medicamentoId: string;
  quantidade: number;
  posologia: string;
}

/** Registro de dispensação no histórico do paciente (DispensacaoDto). */
export interface DispensacaoDto {
  id: string;
  estabelecimentoId: string;
  dataHora: string;
  situacao: string;
  prescricaoId?: string | null;
  itens: ItemDispensadoDto[];
}

/** CadastrarMedicamentoCommand — Forma/Unidade/Controle = CÓDIGO numérico do enum. */
export interface CadastrarMedicamentoInput {
  principioAtivo: string;
  apresentacao: string;
  concentracao: string;
  forma: number;
  unidade: number;
  controle: number;
  codigoCatmat?: string | null;
}

/** EntradaEstoquePayload(NumeroLote, Validade, Quantidade, PontoDeRessuprimento). */
export interface EntradaEstoqueInput {
  numeroLote: string;
  validade: string;
  quantidade: number;
  pontoDeRessuprimento: number;
}

/** Linha de pedido da dispensação (ItemDispensacaoInput). */
export interface ItemDispensacaoInput {
  medicamentoId: string;
  quantidade: number;
  posologia: string;
}

/** DispensarMedicamentoCommand — vincula paciente + baixa FEFO na mesma transação. */
export interface DispensarMedicamentoInput {
  pacienteId: string;
  estabelecimentoId: string;
  profissionalId: string;
  prescricaoId?: string | null;
  itens: ItemDispensacaoInput[];
}

/** Filtros do catálogo de medicamentos (paginação 1-based). */
export interface MedicamentoBuscaFiltro {
  termo?: string;
  apenasAtivos?: boolean;
  pagina: number;
  tamanho: number;
}

/** Janela do histórico de dispensação do paciente (datas ISO yyyy-mm-dd). */
export interface DispensacaoPacienteFiltro {
  de?: string;
  ate?: string;
}

// --- Acesso HTTP ---

function texto(valor?: string | null): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function buscarMedicamentos(
  filtro: MedicamentoBuscaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<MedicamentoItemLista>> {
  return http.get<ResultadoPaginado<MedicamentoItemLista>>('/saude/farmacia/medicamentos', {
    query: {
      termo: texto(filtro.termo),
      apenasAtivos: filtro.apenasAtivos,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

async function cadastrarMedicamento(input: CadastrarMedicamentoInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/farmacia/medicamentos', input);
  return id;
}

function obterPosicaoEstoque(estabId: string, signal?: AbortSignal): Promise<PosicaoEstoqueDto[]> {
  return http.get<PosicaoEstoqueDto[]>('/saude/farmacia/estoque', { query: { estabId }, signal });
}

function listarAlertasValidade(dias: number | undefined, signal?: AbortSignal): Promise<AlertaValidadeDto[]> {
  return http.get<AlertaValidadeDto[]>('/saude/farmacia/alertas/validade', {
    query: { dias },
    signal,
  });
}

async function registrarEntrada(
  estabId: string,
  medId: string,
  payload: EntradaEstoqueInput,
): Promise<string> {
  const { id } = await http.post<IdResponse>(
    `/saude/farmacia/estoque/${estabId}/${medId}/entradas`,
    payload,
  );
  return id;
}

async function dispensar(input: DispensarMedicamentoInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/farmacia/dispensacoes', input);
  return id;
}

function estornarDispensacao(dispensacaoId: string, motivo: string): Promise<void> {
  return http.post<void>(`/saude/farmacia/dispensacoes/${dispensacaoId}/estorno`, { motivo });
}

function listarDispensacoesPaciente(
  pacienteId: string,
  filtro: DispensacaoPacienteFiltro,
  signal?: AbortSignal,
): Promise<DispensacaoDto[]> {
  return http.get<DispensacaoDto[]>(`/saude/farmacia/pacientes/${pacienteId}/dispensacoes`, {
    query: { de: texto(filtro.de), ate: texto(filtro.ate) },
    signal,
  });
}

// --- Hooks TanStack Query — QUERIES ---

/** Busca paginada do catálogo de medicamentos (REMUME) — dado operacional, sem LGPD. */
export function useBuscarMedicamentos(filtro: MedicamentoBuscaFiltro, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.medicamentosBusca(filtro),
    queryFn: ({ signal }) => buscarMedicamentos(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

/** Posição de estoque de um estabelecimento (saldo, lotes, ruptura). */
export function usePosicaoEstoque(estabId: string, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.estoque(estabId),
    queryFn: ({ signal }) => obterPosicaoEstoque(estabId, signal),
    enabled: enabled && estabId.trim().length > 0,
  });
}

/** Alertas de validade (lotes a vencer/vencidos) no horizonte de `dias`. */
export function useAlertasValidade(dias: number | undefined, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.alertasValidade(dias),
    queryFn: ({ signal }) => listarAlertasValidade(dias, signal),
    enabled,
  });
}

/** Histórico de dispensação do paciente (SENSÍVEL — gera trilha de acesso no backend). */
export function useDispensacoesPaciente(
  pacienteId: string,
  filtro: DispensacaoPacienteFiltro,
  enabled = true,
) {
  return useQuery({
    queryKey: saudeKeys.dispensacoesPaciente(pacienteId, filtro),
    queryFn: ({ signal }) => listarDispensacoesPaciente(pacienteId, filtro, signal),
    enabled: enabled && pacienteId.trim().length > 0,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Cadastra um medicamento no catálogo (REMUME) e invalida as listas. */
export function useCadastrarMedicamento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarMedicamento,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.farmacia() });
    },
  });
}

/** Registra entrada de lote (com validade) no estoque de uma UBS. Invalida estoque e alertas. */
export function useRegistrarEntrada() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { estabId: string; medId: string; payload: EntradaEstoqueInput }) =>
      registrarEntrada(args.estabId, args.medId, args.payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.farmacia() });
    },
  });
}

/** Dispensa medicamentos ao paciente (baixa FEFO). Invalida estoque, alertas e histórico. */
export function useDispensar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: dispensar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.farmacia() });
    },
  });
}

/** Estorna uma dispensação (devolve o saldo aos lotes). Invalida estoque e histórico. */
export function useEstornarDispensacao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { dispensacaoId: string; motivo: string }) =>
      estornarDispensacao(args.dispensacaoId, args.motivo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.farmacia() });
    },
  });
}
