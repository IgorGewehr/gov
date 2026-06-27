// Camada de API da SST / SAUDE OCUPACIONAL no modulo RecursosHumanos.
// Contrato real: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.Sst.cs
//   POST /sst/exames-ocupacionais                              -> registra ASO -> { id }
//   GET  /sst/servidores/{id}/exames-ocupacionais              -> ficha de saude (ASO)
//   GET  /sst/pcmso/agenda?ate=YYYY-MM-DD                      -> agenda do PCMSO
//   POST /sst/exames-ocupacionais/{id}/eventos/s2220           -> gera S-2220 -> { id }
//   POST /sst/exposicoes                                       -> inicia exposicao -> { id }
//   POST /sst/exposicoes/{id}/encerramento                     -> encerra (204)
//   GET  /sst/servidores/{id}/exposicoes                       -> exposicoes (PPP)
//   POST /sst/exposicoes/{id}/eventos/s2240                    -> gera S-2240 -> { id }
//   POST /sst/cat                                              -> comunica acidente -> { id }
//   GET  /sst/servidores/{id}/cat                              -> CAT do servidor
//   POST /sst/cat/{id}/eventos/s2210                           -> gera S-2210 -> { id }
//   GET  /sst/servidores/{id}/ppp                              -> PPP consolidado
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs (rotulos -> enums numericos do backend)
// ---------------------------------------------------------------------------

/** Tipo de exame ocupacional (0=adm,1=per,2=ret,3=mud,4=monit,9=dem). */
export type TipoExameOcupacional =
  | 'Admissional'
  | 'Periodico'
  | 'RetornoAoTrabalho'
  | 'MudancaDeRiscoOcupacional'
  | 'MonitoracaoPontual'
  | 'Demissional';

/** Resultado do ASO. */
export type ResultadoAso = 'Apto' | 'Inapto';

/** Situacao do registro de SST. */
export type SituacaoRegistroSst = 'Registrado' | 'Cancelado';

/** Tipo de CAT. */
export type TipoCat = 'Inicial' | 'Reabertura' | 'ComunicacaoObito';

/** Tipo de acidente. */
export type TipoAcidente = 'Tipico' | 'Doenca' | 'Trajeto';

/** Agente nocivo (item do PPP/S-2240). */
export interface AgenteNocivoDto {
  codigo: string;
  descricao: string;
  intensidade: number | null;
  unidadeMedida: string | null;
  utilizaEpc: boolean;
  utilizaEpi: boolean;
}

/** Projecao de leitura de um ASO. */
export interface ExameOcupacionalView {
  id: string;
  servidorId: string;
  tipo: TipoExameOcupacional;
  dataExame: string;
  resultado: ResultadoAso;
  medicoNome: string;
  medicoCrm: string;
  dataProximoExame: string | null;
  examesComplementares: string[];
  situacao: SituacaoRegistroSst;
}

/** Projecao de leitura de uma exposicao a agentes nocivos. */
export interface ExposicaoAgenteNocivoView {
  id: string;
  servidorId: string;
  inicioExposicao: string;
  fimExposicao: string | null;
  setorAtividade: string;
  agentes: AgenteNocivoDto[];
  situacao: SituacaoRegistroSst;
}

/** Projecao de leitura de uma CAT. */
export interface ComunicacaoAcidenteView {
  id: string;
  servidorId: string;
  tipoCat: TipoCat;
  tipoAcidente: TipoAcidente;
  dataHoraAcidente: string;
  houveObito: boolean;
  descricaoSituacao: string;
  cid: string | null;
  situacao: SituacaoRegistroSst;
}

/** PPP (Perfil Profissiografico Previdenciario) consolidado. */
export interface PerfilProfissiograficoView {
  servidorId: string;
  nome: string;
  matricula: string;
  exposicoes: ExposicaoAgenteNocivoView[];
  monitoracaoBiologica: ExameOcupacionalView[];
  possuiExposicaoEspecial: boolean;
}

/** Entrada do registro de ASO. */
export interface RegistrarExameInput {
  servidorId: string;
  /** Enum numerico do backend (0..9). */
  tipo: number;
  dataExame: string;
  /** 1=Apto, 2=Inapto. */
  resultado: number;
  medicoNome: string;
  medicoNrCrm: string;
  medicoUfCrm: string;
  dataProximoExame?: string | null;
  observacao?: string | null;
  examesComplementares?: string[] | null;
}

/** Entrada do registro de exposicao a agentes nocivos. */
export interface RegistrarExposicaoInput {
  servidorId: string;
  inicioExposicao: string;
  setorAtividade: string;
  agentes: AgenteNocivoDto[];
}

/** Entrada da comunicacao de acidente (CAT). */
export interface ComunicarAcidenteInput {
  servidorId: string;
  /** 1=Inicial,2=Reabertura,3=Obito. */
  tipoCat: number;
  /** 1=Tipico,2=Doenca,3=Trajeto. */
  tipoAcidente: number;
  dataHoraAcidente: string;
  descricaoSituacao: string;
  houveObito?: boolean;
  dataObito?: string | null;
  cid?: string | null;
  parteCorpoAtingida?: string | null;
  agenteCausador?: string | null;
  catOrigemId?: string | null;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

const BASE = '/recursoshumanos/sst';

// ---------------------------------------------------------------------------
// Hooks — ASO / PCMSO
// ---------------------------------------------------------------------------

/** Lista os ASO (monitoracao biologica) de um servidor. */
export function useExamesOcupacionais(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.sstExames(servidorId),
    queryFn: () => http.get<ExameOcupacionalView[]>(`${BASE}/servidores/${servidorId}/exames-ocupacionais`),
    enabled: servidorId.length > 0,
  });
}

/** Agenda do PCMSO: ASO com proximo exame vencido/a vencer ate a data. */
export function useAgendaPcmso(ate: string) {
  return useQuery({
    queryKey: rhKeys.sstAgendaPcmso(ate),
    queryFn: () => http.get<ExameOcupacionalView[]>(`${BASE}/pcmso/agenda`, { query: { ate } }),
    enabled: ate.length > 0,
  });
}

/** Registra um ASO e invalida a ficha do servidor. */
export function useRegistrarExame() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarExameInput) =>
      http.post<CriacaoResponse>(`${BASE}/exames-ocupacionais`, input),
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: rhKeys.sstExames(input.servidorId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.sstPpp(input.servidorId) });
    },
  });
}

/** Gera o evento eSocial S-2220 a partir de um ASO. */
export function useGerarS2220() {
  return useMutation({
    mutationFn: (exameId: string) =>
      http.post<CriacaoResponse>(`${BASE}/exames-ocupacionais/${exameId}/eventos/s2220`),
  });
}

// ---------------------------------------------------------------------------
// Hooks — Exposicoes / PPP
// ---------------------------------------------------------------------------

/** Lista as exposicoes a agentes nocivos (registros ambientais do PPP). */
export function useExposicoes(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.sstExposicoes(servidorId),
    queryFn: () => http.get<ExposicaoAgenteNocivoView[]>(`${BASE}/servidores/${servidorId}/exposicoes`),
    enabled: servidorId.length > 0,
  });
}

/** PPP consolidado do servidor. */
export function usePpp(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.sstPpp(servidorId),
    queryFn: () => http.get<PerfilProfissiograficoView>(`${BASE}/servidores/${servidorId}/ppp`),
    enabled: servidorId.length > 0,
  });
}

/** Inicia uma exposicao a agentes nocivos. */
export function useRegistrarExposicao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: RegistrarExposicaoInput) =>
      http.post<CriacaoResponse>(`${BASE}/exposicoes`, input),
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: rhKeys.sstExposicoes(input.servidorId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.sstPpp(input.servidorId) });
    },
  });
}

/** Encerra uma exposicao (gera o fim do periodo no PPP/S-2240). */
export function useEncerrarExposicao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ exposicaoId, fimExposicao }: { exposicaoId: string; fimExposicao: string }) =>
      http.post<void>(`${BASE}/exposicoes/${exposicaoId}/encerramento`, { fimExposicao }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.sstExposicoes(servidorId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.sstPpp(servidorId) });
    },
  });
}

/** Gera o evento eSocial S-2240 a partir de uma exposicao. */
export function useGerarS2240() {
  return useMutation({
    mutationFn: (exposicaoId: string) =>
      http.post<CriacaoResponse>(`${BASE}/exposicoes/${exposicaoId}/eventos/s2240`),
  });
}

// ---------------------------------------------------------------------------
// Hooks — CAT
// ---------------------------------------------------------------------------

/** Lista as CAT de um servidor. */
export function useComunicacoesAcidente(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.sstCat(servidorId),
    queryFn: () => http.get<ComunicacaoAcidenteView[]>(`${BASE}/servidores/${servidorId}/cat`),
    enabled: servidorId.length > 0,
  });
}

/** Comunica um acidente de trabalho (CAT). */
export function useComunicarAcidente() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ComunicarAcidenteInput) => http.post<CriacaoResponse>(`${BASE}/cat`, input),
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: rhKeys.sstCat(input.servidorId) });
    },
  });
}

/** Gera o evento eSocial S-2210 a partir de uma CAT. */
export function useGerarS2210() {
  return useMutation({
    mutationFn: (catId: string) => http.post<CriacaoResponse>(`${BASE}/cat/${catId}/eventos/s2210`),
  });
}
