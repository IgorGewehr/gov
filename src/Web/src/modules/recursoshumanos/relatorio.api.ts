// Camada de API dos RELATÓRIOS gerenciais da folha (Onda 3a — ONDA3-DESIGN §4.1).
// Somente leitura (GET), gated por 'recursoshumanos.ver'. Os DTOs espelham os read
// models do backend — NÃO inventar campos.
// Contrato real:
//   src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.Relatorios.cs
//     (grupo /relatorios; o http client já prefixa /api → usamos /recursoshumanos)
//   ...Application/Relatorios/RelatoriosViews.cs (records de retorno)
// Observação: folha-por-secretaria e demonstrativo-tce retornam 404 (corpo nulo) quando
// não há folha mensal na competência — modelado como `… | null`.
import { useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs — Folha por secretaria/UO e por fonte (FolhaPorSecretariaView)
// ---------------------------------------------------------------------------

/** Linha da folha agregada por secretaria/unidade de lotação (LinhaFolhaSecretaria). */
export interface LinhaFolhaSecretaria {
  unidade: string;
  quantidadeServidores: number;
  totalProventos: number;
  totalDescontos: number;
  totalLiquido: number;
}

/** Linha da folha agregada por fonte/regime previdenciário (LinhaFolhaFonte). */
export interface LinhaFolhaFonte {
  fonte: string;
  quantidadeServidores: number;
  totalProventos: number;
  totalDescontos: number;
  totalLiquido: number;
}

/** Folha por secretaria/UO e por fonte numa competência (FolhaPorSecretariaView). */
export interface FolhaPorSecretariaView {
  competencia: string;
  situacaoFolha: string;
  quantidadeServidores: number;
  totalProventos: number;
  totalDescontos: number;
  totalLiquido: number;
  porSecretaria: LinhaFolhaSecretaria[];
  porFonte: LinhaFolhaFonte[];
}

// ---------------------------------------------------------------------------
// DTOs — Evolução mensal da despesa de pessoal (EvolucaoDespesaPessoalView)
// ---------------------------------------------------------------------------

/** Ponto da série mensal da despesa de pessoal (PontoEvolucaoDespesa). */
export interface PontoEvolucaoDespesa {
  ano: number;
  mes: number;
  competencia: string;
  situacaoFolha: string;
  quantidadeServidores: number;
  despesaBruta: number;
  totalDescontos: number;
  totalLiquido: number;
}

/** Evolução mensal da despesa de pessoal num intervalo (EvolucaoDespesaPessoalView). */
export interface EvolucaoDespesaPessoalView {
  competenciaInicial: string;
  competenciaFinal: string;
  despesaBrutaAcumulada: number;
  despesaBrutaMediaMensal: number;
  mesesComFolha: number;
  serie: PontoEvolucaoDespesa[];
}

// ---------------------------------------------------------------------------
// DTOs — Mapa de cargos (MapaCargosView)
// ---------------------------------------------------------------------------

/** Linha do mapa de cargos — um cargo do quadro (LinhaMapaCargo). */
export interface LinhaMapaCargo {
  cargoId: string;
  denominacao: string;
  tipo: string;
  situacao: string;
  unidade: string;
  vencimento: number;
  vagasAutorizadas: number;
  vagasOcupadas: number;
  vagasDisponiveis: number;
}

/** Totalizador do mapa de cargos por tipo (TotalMapaCargoPorTipo). */
export interface TotalMapaCargoPorTipo {
  tipo: string;
  vagasAutorizadas: number;
  vagasOcupadas: number;
  vagasDisponiveis: number;
}

/** Mapa de cargos do quadro de pessoal (MapaCargosView). */
export interface MapaCargosView {
  totalVagasAutorizadas: number;
  totalVagasOcupadas: number;
  totalVagasDisponiveis: number;
  porTipo: TotalMapaCargoPorTipo[];
  cargos: LinhaMapaCargo[];
}

// ---------------------------------------------------------------------------
// DTOs — Demonstrativo TCE (DemonstrativoTceView)
// ---------------------------------------------------------------------------

/** Linha do demonstrativo TCE por fonte/regime (DemonstrativoTceFonte). */
export interface DemonstrativoTceFonte {
  fonte: string;
  quantidadeServidores: number;
  totalProventos: number;
  totalDescontos: number;
  totalLiquido: number;
}

/** Linha do demonstrativo TCE por secretaria/UO (DemonstrativoTceUnidade). */
export interface DemonstrativoTceUnidade {
  unidade: string;
  quantidadeServidores: number;
  totalProventos: number;
  totalDescontos: number;
  totalLiquido: number;
}

/** Demonstrativo de despesa de pessoal para o TCE numa competência (DemonstrativoTceView). */
export interface DemonstrativoTceView {
  competencia: string;
  situacaoFolha: string;
  quantidadeServidores: number;
  totalProventos: number;
  totalDescontos: number;
  totalLiquido: number;
  contribuicaoPrevidenciariaSegurado: number;
  porFonte: DemonstrativoTceFonte[];
  porSecretaria: DemonstrativoTceUnidade[];
}

// ---------------------------------------------------------------------------
// Acesso HTTP (o http client já prefixa /api)
// ---------------------------------------------------------------------------

function obterFolhaPorSecretaria(
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<FolhaPorSecretariaView | null> {
  return http.get<FolhaPorSecretariaView | null>('/recursoshumanos/relatorios/folha-por-secretaria', {
    signal,
    query: { ano, mes },
  });
}

function obterEvolucaoDespesa(
  anoDe: number,
  mesDe: number,
  anoAte: number,
  mesAte: number,
  signal?: AbortSignal,
): Promise<EvolucaoDespesaPessoalView> {
  return http.get<EvolucaoDespesaPessoalView>('/recursoshumanos/relatorios/evolucao-despesa', {
    signal,
    query: { anoDe, mesDe, anoAte, mesAte },
  });
}

function obterMapaCargos(signal?: AbortSignal): Promise<MapaCargosView> {
  return http.get<MapaCargosView>('/recursoshumanos/relatorios/mapa-cargos', { signal });
}

function obterDemonstrativoTce(
  ano: number,
  mes: number,
  signal?: AbortSignal,
): Promise<DemonstrativoTceView | null> {
  return http.get<DemonstrativoTceView | null>('/recursoshumanos/relatorios/demonstrativo-tce', {
    signal,
    query: { ano, mes },
  });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query (disparo sob demanda via `enabled`)
// ---------------------------------------------------------------------------

/** Folha por secretaria/UO e fonte numa competência. */
export function useFolhaPorSecretaria(ano: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: rhKeys.relFolhaPorSecretaria(ano, mes),
    queryFn: ({ signal }) => obterFolhaPorSecretaria(ano, mes, signal),
    enabled,
  });
}

/** Evolução mensal da despesa de pessoal num intervalo de competências. */
export function useEvolucaoDespesa(
  anoDe: number,
  mesDe: number,
  anoAte: number,
  mesAte: number,
  enabled = true,
) {
  return useQuery({
    queryKey: rhKeys.relEvolucaoDespesa(anoDe, mesDe, anoAte, mesAte),
    queryFn: ({ signal }) => obterEvolucaoDespesa(anoDe, mesDe, anoAte, mesAte, signal),
    enabled,
  });
}

/** Mapa de cargos do quadro de pessoal (ocupados x vagos). */
export function useMapaCargos(enabled = true) {
  return useQuery({
    queryKey: rhKeys.relMapaCargos(),
    queryFn: ({ signal }) => obterMapaCargos(signal),
    enabled,
  });
}

/** Demonstrativo de despesa de pessoal para o TCE numa competência. */
export function useDemonstrativoTce(ano: number, mes: number, enabled = true) {
  return useQuery({
    queryKey: rhKeys.relDemonstrativoTce(ano, mes),
    queryFn: ({ signal }) => obterDemonstrativoTce(ano, mes, signal),
    enabled,
  });
}
