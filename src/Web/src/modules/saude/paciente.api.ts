// Camada de API do agregado Paciente (módulo Saúde) — PEP/CADSUS. DTOs + funções de
// acesso HTTP + hooks TanStack Query para CADA endpoint de paciente.
// Contrato REAL (SaudeEndpoints.cs):
//   POST /saude/pacientes                                  -> CadastrarPaciente            -> { id }
//   GET  /saude/pacientes/por-cns/{cns}                    -> ObterPacientePorCns          -> PacienteResumo | null
//   GET  /saude/pacientes/{id}/historico-clinico           -> ObterHistoricoClinico        -> HistoricoClinico
//   PUT  /saude/pacientes/{id}                             -> AtualizarCadastroPaciente    -> 204
//   POST /saude/pacientes/{id}/confirmacao-cadsus          -> ConfirmarCadastroNoCadsus    -> 204
//   POST /saude/pacientes/{id}/condicoes                   -> RegistrarCondicaoDeSaude     -> 204
//   POST /saude/pacientes/{id}/alergias                    -> RegistrarAlergia             -> 204
//   POST /saude/pacientes/{id}/inativacao                  -> InativarPaciente             -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';

// --- DTOs ---

/** Sexo (1 = Feminino, 2 = Masculino, 9 = Ignorado) — conforme CADSUS. */
export type Sexo = 1 | 2 | 9;

export interface IdentificacaoInput {
  nome: string;
  dataNascimento: string; // ISO yyyy-mm-dd (DateOnly)
  sexo: Sexo;
  nomeSocial?: string | null;
  cpf?: string | null;
}

export interface EnderecoInput {
  logradouro: string;
  numero: string;
  bairro: string;
  municipio: string;
  uf: string;
  cep: string;
}

export interface CadastrarPacienteInput {
  cns: string;
  identificacao: IdentificacaoInput;
  endereco: EnderecoInput;
}

/** Projeção minimizada do paciente (LGPD: minimização de dados). */
export interface PacienteResumo {
  id: string;
  cns: string;
  nome: string;
  nomeSocial: string | null;
  dataNascimento: string;
  sexo: string;
  cnsConfirmado: boolean;
  situacao: string;
}

export interface CondicaoSaude {
  codigo: string;
  descricao: string;
  dataRegistro: string;
  ativa: boolean;
}

export interface Alergia {
  substancia: string;
  gravidade: string;
  dataRegistro: string;
}

export interface HistoricoClinico {
  pacienteId: string;
  condicoes: CondicaoSaude[];
  alergias: Alergia[];
}

/** AtualizarCadastroPacientePayload(Identificacao, Endereco) — PUT /pacientes/{id}. */
export interface AtualizarCadastroPacienteInput {
  identificacao: IdentificacaoInput;
  endereco: EnderecoInput;
}

/** RegistrarCondicaoPayload(Codigo, Descricao). */
export interface RegistrarCondicaoInput {
  codigo: string;
  descricao: string;
}

/** RegistrarAlergiaPayload(Substancia, Gravidade). */
export interface RegistrarAlergiaInput {
  substancia: string;
  gravidade: string;
}

/** InativarPacientePayload(Motivo). */
export interface InativarPacienteInput {
  motivo: string;
}

// --- Acesso HTTP ---

function obterPacientePorCns(cns: string, signal?: AbortSignal): Promise<PacienteResumo | null> {
  return http.get<PacienteResumo | null>(`/saude/pacientes/por-cns/${encodeURIComponent(cns)}`, { signal });
}

function obterHistoricoClinico(pacienteId: string, signal?: AbortSignal): Promise<HistoricoClinico> {
  return http.get<HistoricoClinico>(`/saude/pacientes/${pacienteId}/historico-clinico`, { signal });
}

async function cadastrarPaciente(input: CadastrarPacienteInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/pacientes', input);
  return id;
}

function atualizarCadastroPaciente(
  pacienteId: string,
  input: AtualizarCadastroPacienteInput,
): Promise<void> {
  return http.put<void>(`/saude/pacientes/${pacienteId}`, input);
}

function confirmarCadastroCadsus(pacienteId: string): Promise<void> {
  return http.post<void>(`/saude/pacientes/${pacienteId}/confirmacao-cadsus`);
}

function registrarCondicao(pacienteId: string, input: RegistrarCondicaoInput): Promise<void> {
  return http.post<void>(`/saude/pacientes/${pacienteId}/condicoes`, input);
}

function registrarAlergia(pacienteId: string, input: RegistrarAlergiaInput): Promise<void> {
  return http.post<void>(`/saude/pacientes/${pacienteId}/alergias`, input);
}

function inativarPaciente(pacienteId: string, input: InativarPacienteInput): Promise<void> {
  return http.post<void>(`/saude/pacientes/${pacienteId}/inativacao`, input);
}

// --- Hooks TanStack Query — QUERIES ---

/** Consulta o resumo de um paciente pelo CNS. `enabled` controla disparo sob demanda. */
export function usePacientePorCns(cns: string, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.pacientePorCns(cns),
    queryFn: ({ signal }) => obterPacientePorCns(cns, signal),
    enabled: enabled && cns.trim().length > 0,
  });
}

/** Histórico clínico (condições/alergias) de um paciente — dado sensível (LGPD art. 11). */
export function useHistoricoClinico(pacienteId: string) {
  return useQuery({
    queryKey: saudeKeys.historico(pacienteId),
    queryFn: ({ signal }) => obterHistoricoClinico(pacienteId, signal),
    enabled: pacienteId.trim().length > 0,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Cadastra um paciente no PEP e invalida as consultas afetadas. */
export function useCadastrarPaciente() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarPaciente,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.pacientes() });
    },
  });
}

/** Atualiza o cadastro civil/endereço do paciente (PUT). Invalida as consultas de paciente. */
export function useAtualizarCadastroPaciente(pacienteId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AtualizarCadastroPacienteInput) =>
      atualizarCadastroPaciente(pacienteId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.pacientes() });
    },
  });
}

/** Confirma o cadastro do paciente no CADSUS (POST). Invalida as consultas de paciente. */
export function useConfirmarCadastroCadsus(pacienteId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => confirmarCadastroCadsus(pacienteId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.pacientes() });
    },
  });
}

/** Registra uma condição de saúde (CID/CIAP) no histórico. Invalida o histórico clínico. */
export function useRegistrarCondicao(pacienteId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarCondicaoInput) => registrarCondicao(pacienteId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.historico(pacienteId) });
    },
  });
}

/** Registra uma alergia no histórico clínico. Invalida o histórico clínico. */
export function useRegistrarAlergia(pacienteId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarAlergiaInput) => registrarAlergia(pacienteId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.historico(pacienteId) });
    },
  });
}

/** Inativa o cadastro do paciente (óbito/transferência/duplicidade) — terminal. */
export function useInativarPaciente(pacienteId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: InativarPacienteInput) => inativarPaciente(pacienteId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.pacientes() });
    },
  });
}
