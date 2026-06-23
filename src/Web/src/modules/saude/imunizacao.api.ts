// Camada de API da IMUNIZAÇÃO (SI-PNI) — módulo Saúde. DTOs + acesso HTTP + hooks TanStack.
// Contrato REAL (SaudeEndpointsFarmaciaImunizacao.cs — grupo /api/saude; o http client já prefixa /api):
//   POST /saude/imunizacao/imunobiologicos {nome,sigla,totalDoses,intervaloDiasProximaDose,doseUnica,medicamentoEstoqueId?} -> { id } (saude.imunizacao.gerenciar)
//   GET  /saude/imunizacao/imunobiologicos                       -> ImunobiologicoItemLista[]      (saude.imunizacao.ver)
//   GET  /saude/imunizacao/pacientes/{pacienteId}/carteira       -> CarteiraVacinacaoDto (SENSÍVEL) (saude.prontuario.ler)
//   POST /saude/imunizacao/pacientes/{pacienteId}/doses {imunobiologicoId,tipoDose,numeroDose,lote,aplicadorId,dataAplicacao,estabelecimentoId?} -> { id } (saude.imunizacao.aplicar)
//   GET  /saude/imunizacao/aprazamentos/vencidos                 -> AprazamentoVencidoDto[] (SENSÍVEL) (saude.prontuario.ler)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';

// --- DTOs ---

/** Item do catálogo de imunobiológicos (ImunobiologicoItemLista). */
export interface ImunobiologicoItemLista {
  id: string;
  nome: string;
  sigla: string;
  totalDoses: number;
  intervaloDiasProximaDose: number;
  doseUnica: boolean;
}

/** Dose aplicada na carteira de vacinação (DoseAplicadaDto). */
export interface DoseAplicadaDto {
  imunobiologicoId: string;
  sigla: string;
  tipoDose: string;
  numeroDose: number;
  lote: string;
  dataAplicacao: string;
  proximaDoseAprazada?: string | null;
}

/** Carteira de vacinação do paciente (CarteiraVacinacaoDto). */
export interface CarteiraVacinacaoDto {
  pacienteId: string;
  doses: DoseAplicadaDto[];
}

/** Aprazamento vencido — alvo de busca ativa (AprazamentoVencidoDto). */
export interface AprazamentoVencidoDto {
  pacienteId: string;
  imunobiologicoId: string;
  sigla: string;
  ultimaDose: number;
  dataAprazada: string;
}

/** CadastrarImunobiologicoCommand. */
export interface CadastrarImunobiologicoInput {
  nome: string;
  sigla: string;
  totalDoses: number;
  intervaloDiasProximaDose: number;
  doseUnica: boolean;
  medicamentoEstoqueId?: string | null;
}

/** AplicarDosePayload — TipoDose = CÓDIGO numérico do enum; datas ISO yyyy-mm-dd. */
export interface AplicarDoseInput {
  imunobiologicoId: string;
  tipoDose: number;
  numeroDose: number;
  lote: string;
  aplicadorId: string;
  dataAplicacao: string;
  estabelecimentoId?: string | null;
}

// --- Acesso HTTP ---

function listarImunobiologicos(signal?: AbortSignal): Promise<ImunobiologicoItemLista[]> {
  return http.get<ImunobiologicoItemLista[]>('/saude/imunizacao/imunobiologicos', { signal });
}

async function cadastrarImunobiologico(input: CadastrarImunobiologicoInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/imunizacao/imunobiologicos', input);
  return id;
}

function obterCarteira(pacienteId: string, signal?: AbortSignal): Promise<CarteiraVacinacaoDto> {
  return http.get<CarteiraVacinacaoDto>(`/saude/imunizacao/pacientes/${pacienteId}/carteira`, {
    signal,
  });
}

async function aplicarDose(pacienteId: string, payload: AplicarDoseInput): Promise<string> {
  const { id } = await http.post<IdResponse>(
    `/saude/imunizacao/pacientes/${pacienteId}/doses`,
    payload,
  );
  return id;
}

function listarAprazamentosVencidos(signal?: AbortSignal): Promise<AprazamentoVencidoDto[]> {
  return http.get<AprazamentoVencidoDto[]>('/saude/imunizacao/aprazamentos/vencidos', { signal });
}

// --- Hooks TanStack Query — QUERIES ---

/** Catálogo de imunobiológicos ativos (PNI) — dado operacional, sem LGPD. */
export function useImunobiologicos(enabled = true) {
  return useQuery({
    queryKey: saudeKeys.imunobiologicos(),
    queryFn: ({ signal }) => listarImunobiologicos(signal),
    enabled,
  });
}

/** Carteira de vacinação do paciente (SENSÍVEL — gera trilha de acesso no backend). */
export function useCarteiraVacinacao(pacienteId: string, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.carteiraVacinacao(pacienteId),
    queryFn: ({ signal }) => obterCarteira(pacienteId, signal),
    enabled: enabled && pacienteId.trim().length > 0,
  });
}

/** Busca ativa: aprazamentos vencidos (SENSÍVEL — gera trilha de acesso no backend). */
export function useAprazamentosVencidos(enabled = true) {
  return useQuery({
    queryKey: saudeKeys.aprazamentosVencidos(),
    queryFn: ({ signal }) => listarAprazamentosVencidos(signal),
    enabled,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Cadastra um imunobiológico no catálogo (PNI). Invalida o catálogo. */
export function useCadastrarImunobiologico() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarImunobiologico,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.imunobiologicos() });
    },
  });
}

/** Registra a aplicação de uma dose (calcula aprazamento). Invalida a imunização. */
export function useAplicarDose() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { pacienteId: string; payload: AplicarDoseInput }) =>
      aplicarDose(args.pacienteId, args.payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.imunizacao() });
    },
  });
}
