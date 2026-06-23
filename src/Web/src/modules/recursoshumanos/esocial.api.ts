// Camada de API do eSocial (módulo RecursosHumanos). Segue o PADRÃO-OURO: DTOs no
// topo (fiéis ao contrato), funções de acesso via http client tipado, hooks Query.
// Contrato real: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
// (grupo /api/recursoshumanos/esocial) + commands em ...Application/ESocial/*.cs.
//
// IMPORTANTE (homologação): a TRANSMISSÃO real depende de credenciais de homologação
// (Produção Restrita) do ente. Por ora o gateway é SIMULADO (ESocialGatewaySimulado) —
// a UI deixa isso explícito. Nenhuma transmissão tem efeito jurídico nesse modo.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs (espelham EventoESocialDto + commands do backend; enums como string)
// ---------------------------------------------------------------------------

/** Tipo de evento eSocial (string do enum TipoEventoESocial — leiautes S-1.3). */
export type TipoEventoESocial =
  | 'S1000Empregador'
  | 'S1005Estabelecimento'
  | 'S1010Rubrica'
  | 'S2200Admissao'
  | 'S2299Desligamento'
  | 'S1200Remuneracao'
  | 'S1202RemuneracaoRpps'
  | 'S1210Pagamentos'
  | 'S1299Fechamento';

/** Estado do evento na máquina de estados (EstadoEventoESocial). */
export type EstadoEventoESocial =
  | 'Gerado'
  | 'Assinado'
  | 'Transmitido'
  | 'Processado'
  | 'Rejeitado'
  | 'RejeitadoLocal';

/** Ambiente de transmissão (AmbienteESocial — tpAmb do ideEvento). */
export type AmbienteESocial = 'Producao' | 'ProducaoRestrita';

/**
 * Projeção de leitura de um evento eSocial (EventoESocialDto): estado da máquina,
 * XML gerado (fiel ao leiaute), protocolo do lote e recibo (nrRecibo) por evento.
 */
export interface EventoESocial {
  id: string;
  tipo: string;
  idEvento: string;
  estado: string;
  ambiente: string;
  hashXmlGerado: string;
  protocoloLote: string | null;
  numeroRecibo: string | null;
  assinado: boolean;
  xml: string;
}

/** Entrada do S-1005 (estabelecimento). */
export interface GerarS1005Input {
  cnpjEstabelecimento: string;
  cnaePreponderante: string;
}

/** Entrada do S-1010 (rubrica vigente na competência). */
export interface GerarS1010Input {
  codigoRubrica: string;
  ano: number;
  mes: number;
}

/** Entrada do S-2200 (admissão a partir do agregado Servidor). */
export interface GerarS2200Input {
  servidorId: string;
  codCateg: string;
  codCargo: string;
  vrSalFx: number;
}

/** Entrada do S-2299 (desligamento). */
export interface GerarS2299Input {
  servidorId: string;
  mtvDeslig: string;
}

/** Resposta dos endpoints de geração em lote (folha) — `{ ids }`. */
export interface IdsResponse {
  ids: string[];
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

const BASE = '/recursoshumanos/esocial';

function listarEventos(signal?: AbortSignal): Promise<EventoESocial[]> {
  return http.get<EventoESocial[]>(`${BASE}/eventos`, { signal });
}

function gerarS1000(): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/eventos/s1000`);
}

function gerarS1005(input: GerarS1005Input): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/eventos/s1005`, input);
}

function gerarS1010(input: GerarS1010Input): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/eventos/s1010`, input);
}

function gerarS2200(input: GerarS2200Input): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/eventos/s2200`, input);
}

function gerarS2299(input: GerarS2299Input): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/eventos/s2299`, input);
}

function gerarRemuneracaoFolha(folhaId: string): Promise<IdsResponse> {
  return http.post<IdsResponse>(`${BASE}/folhas/${folhaId}/remuneracao`);
}

function gerarPagamentosFolha(folhaId: string): Promise<IdsResponse> {
  return http.post<IdsResponse>(`${BASE}/folhas/${folhaId}/pagamentos`);
}

function gerarFechamentoFolha(folhaId: string): Promise<CriacaoResponse> {
  return http.post<CriacaoResponse>(`${BASE}/folhas/${folhaId}/fechamento-esocial`);
}

function assinarEvento(eventoId: string): Promise<void> {
  return http.post<void>(`${BASE}/eventos/${eventoId}/assinatura`);
}

function transmitir(): Promise<{ transmitidos: number }> {
  return http.post<{ transmitidos: number }>(`${BASE}/transmissao`);
}

function consultarRetornos(): Promise<{ processados: number }> {
  return http.post<{ processados: number }>(`${BASE}/retornos`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista todos os eventos eSocial do tenant (inspeção/auditoria). */
export function useEventosESocial() {
  return useQuery({
    queryKey: rhKeys.esocialEventos(),
    queryFn: ({ signal }) => listarEventos(signal),
  });
}

function invalidarEventos(queryClient: ReturnType<typeof useQueryClient>): void {
  void queryClient.invalidateQueries({ queryKey: rhKeys.esocial() });
}

/** Gera o S-1000 (empregador) a partir da configuração do ente. Idempotente. */
export function useGerarS1000() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarS1000,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Gera o S-1005 (estabelecimento). */
export function useGerarS1005() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarS1005,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Gera o S-1010 (rubrica vigente). */
export function useGerarS1010() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarS1010,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Gera o S-2200 (admissão) a partir de um servidor. */
export function useGerarS2200() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarS2200,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Gera o S-2299 (desligamento) a partir de um servidor desligado. */
export function useGerarS2299() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarS2299,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Gera S-1200/1202 (remuneração) de TODOS os servidores de uma folha fechada. */
export function useGerarRemuneracaoFolha() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarRemuneracaoFolha,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Gera S-1210 (pagamentos) de TODOS os servidores de uma folha paga. */
export function useGerarPagamentosFolha() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarPagamentosFolha,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Gera o S-1299 (fechamento da competência). */
export function useGerarFechamentoFolha() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarFechamentoFolha,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Assina (A1 via Cofre) um evento Gerado → Assinado. Idempotente. */
export function useAssinarEvento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: assinarEvento,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Empacota os Assinados em lote(s) e transmite (gateway simulado). */
export function useTransmitirEventos() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: transmitir,
    onSuccess: () => invalidarEventos(queryClient),
  });
}

/** Consulta/processa os retornos (recibo por evento, ou erros). */
export function useConsultarRetornos() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: consultarRetornos,
    onSuccess: () => invalidarEventos(queryClient),
  });
}
