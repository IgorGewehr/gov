// Camada de API do AUTOSSERVICO "Minha Folha" (modulo RecursosHumanos). Dado-proprio:
// NENHUM endpoint aceita servidorId — o servidor e SEMPRE resolvido do PROPRIO usuario
// autenticado (JWT) no backend. A UI nunca envia servidorId.
// Contrato real:
//   src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs (grupo /minha-folha)
//   ...Application/MinhaFolha/{ObterMeuContracheque,ObterMeuEspelhoDePonto,
//                              ObterMinhasFerias,ObterMeuInformeDeRendimentos}.cs
import { useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs (espelham os records de retorno do backend — NAO inventar campos)
// ---------------------------------------------------------------------------

/** Tipo de folha aceito pelo endpoint do contracheque (enum TipoFolha do backend). */
export type TipoFolhaMinha = 'Mensal' | 'DecimoTerceiro' | 'Ferias' | 'Rescisao';

/** Linha (rubrica) do meu contracheque — LinhaMeuContracheque. */
export interface LinhaMeuContracheque {
  rubrica: string;
  tipo: string;
  valor: number;
}

/** Meu contracheque numa competencia/tipo — MeuContrachequeDto. */
export interface MeuContracheque {
  servidorId: string;
  competencia: string;
  tipo: string;
  linhas: LinhaMeuContracheque[];
  totalProventos: number;
  totalDescontos: number;
  liquidoAPagar: number;
}

/** Meu espelho de ponto numa competencia — MeuEspelhoDePontoDto. */
export interface MeuEspelhoDePonto {
  servidorId: string;
  competencia: string;
  minutosTrabalhados: number;
  minutosDevidos: number;
  minutosExtras: number;
  minutosFalta: number;
  saldoBancoHorasMinutos: number;
  fechada: boolean;
}

/** Uma folha de ferias minha no ano — MinhasFeriasItemDto. */
export interface MinhasFeriasItem {
  competencia: string;
  totalProventos: number;
  totalDescontos: number;
  liquidoAPagar: number;
  situacao: string;
}

/** Minhas ferias num ano — MinhasFeriasDto. */
export interface MinhasFerias {
  servidorId: string;
  ano: number;
  folhas: MinhasFeriasItem[];
}

/** Meu informe de rendimentos anual — MeuInformeRendimentosDto. */
export interface MeuInformeRendimentos {
  servidorId: string;
  anoCalendario: number;
  rendimentosTributaveis: number;
  previdenciaOficial: number;
  impostoRetidoNaFonte: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP (o http client ja prefixa /api)
// ---------------------------------------------------------------------------

function obterMeuContracheque(
  ano: number,
  mes: number,
  tipo: TipoFolhaMinha,
  signal?: AbortSignal,
): Promise<MeuContracheque | null> {
  return http.get<MeuContracheque | null>('/recursoshumanos/minha-folha/contracheque', {
    signal,
    query: { ano, mes, tipo },
  });
}

function obterMeuEspelhoPonto(
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<MeuEspelhoDePonto | null> {
  return http.get<MeuEspelhoDePonto | null>('/recursoshumanos/minha-folha/espelho-ponto', {
    signal,
    query: { ano, mes },
  });
}

function obterMinhasFerias(ano: number, signal?: AbortSignal): Promise<MinhasFerias> {
  return http.get<MinhasFerias>('/recursoshumanos/minha-folha/ferias', {
    signal,
    query: { ano },
  });
}

function obterMeuInformeRendimentos(
  ano: number,
  signal?: AbortSignal,
): Promise<MeuInformeRendimentos> {
  return http.get<MeuInformeRendimentos>('/recursoshumanos/minha-folha/informe-rendimentos', {
    signal,
    query: { ano },
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query (disparo sob demanda via `enabled`)
// ---------------------------------------------------------------------------

/** Meu contracheque por competencia e tipo de folha. */
export function useMeuContracheque(
  ano: number,
  mes: number,
  tipo: TipoFolhaMinha,
  enabled = true,
) {
  return useQuery({
    queryKey: rhKeys.meuContracheque(ano, mes, tipo),
    queryFn: ({ signal }) => obterMeuContracheque(ano, mes, tipo, signal),
    enabled,
  });
}

/** Meu espelho de ponto (apuracao da jornada) numa competencia. */
export function useMeuEspelhoPonto(ano: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: rhKeys.meuEspelhoPonto(ano, mes),
    queryFn: ({ signal }) => obterMeuEspelhoPonto(ano, mes, signal),
    enabled,
  });
}

/** Minhas ferias num ano. */
export function useMinhasFerias(ano: number, enabled = true) {
  return useQuery({
    queryKey: rhKeys.minhasFerias(ano),
    queryFn: ({ signal }) => obterMinhasFerias(ano, signal),
    enabled,
  });
}

/** Meu informe de rendimentos anual. */
export function useMeuInformeRendimentos(ano: number, enabled = true) {
  return useQuery({
    queryKey: rhKeys.meuInformeRendimentos(ano),
    queryFn: ({ signal }) => obterMeuInformeRendimentos(ano, signal),
    enabled,
  });
}
