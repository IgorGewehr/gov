// Camada de API do PAINEL DE FROTA (módulo Patrimonio — Onda 3a).
// Read models de gestão sobre o agregado Veiculo (custo/consumo por veículo e
// período, CNH a vencer e manutenções abertas). NÃO cria entidade — só projeção.
// Segue o PADRÃO-OURO de src/modules/tributos/api.ts.
//
// Contrato HTTP real (PatrimonioEndpoints.MapearPainelFrota, rotas /api/patrimonio/...):
//   GET /frota/painel?de&ate                       -> PainelFrotaResumo      ObterPainelFrota
//   GET /frota/veiculos/{veiculoId}/custos?de&ate   -> CustoVeiculoResumo|null ObterCustoPorVeiculo
//   GET /frota/cnh-vencendo?dias                     -> CnhVencendoResumo[]    ListarCnhVencendo
//   GET /frota/manutencoes/abertas                   -> ManutencaoAbertaResumo[] ListarManutencoesAbertas
import { useQuery } from '@tanstack/react-query';
import { http } from '../../../api/http';

// ---------------------------------------------------------------------------
// DTOs — espelham os records de Application/Frota (read models)
// ---------------------------------------------------------------------------

/** Projeção de CustoVeiculoResumo (custo/consumo de um veículo no período). */
export interface CustoVeiculoResumo {
  veiculoId: string;
  placa: string;
  descricao: string;
  gastoCombustivel: number;
  litrosAbastecidos: number;
  gastoManutencao: number;
  gastoMultas: number;
  custoTotal: number;
  kmRodados: number;
  consumoMedioKmL: number | null;
}

/** Projeção de PainelFrotaResumo (KPIs agregados + detalhe por veículo). */
export interface PainelFrotaResumo {
  de: string;
  ate: string;
  quantidadeVeiculos: number;
  gastoCombustivel: number;
  gastoManutencao: number;
  gastoMultas: number;
  gastoTotal: number;
  litrosTotais: number;
  consumoMedioFrotaKmL: number | null;
  manutencoesAbertas: number;
  veiculos: CustoVeiculoResumo[];
}

/** Projeção de CnhVencendoResumo (condutor com CNH a vencer/vencida). */
export interface CnhVencendoResumo {
  veiculoId: string;
  placa: string;
  motoristaId: string;
  nome: string;
  cnh: string;
  categoriaCnh: string;
  validadeCnh: string;
  diasParaVencer: number;
  vencida: boolean;
}

/** Projeção de ManutencaoAbertaResumo (OS de manutenção em aberto). */
export interface ManutencaoAbertaResumo {
  veiculoId: string;
  placa: string;
  ordemServicoId: string;
  descricao: string;
  custoEstimado: number;
  odometro: number;
}

// ---------------------------------------------------------------------------
// Query keys (fonte única para invalidação)
// ---------------------------------------------------------------------------

export const frotaKeys = {
  all: ['patrimonio', 'frota'] as const,
  painel: (de: string, ate: string) => [...frotaKeys.all, 'painel', de, ate] as const,
  cnhVencendo: (dias: number) => [...frotaKeys.all, 'cnh-vencendo', dias] as const,
  manutencoesAbertas: () => [...frotaKeys.all, 'manutencoes-abertas'] as const,
};

// ---------------------------------------------------------------------------
// Acesso HTTP — Queries
// ---------------------------------------------------------------------------

function obterPainel(de: string, ate: string, signal?: AbortSignal): Promise<PainelFrotaResumo> {
  return http.get<PainelFrotaResumo>('/patrimonio/frota/painel', { query: { de, ate }, signal });
}

function listarCnhVencendo(dias: number, signal?: AbortSignal): Promise<CnhVencendoResumo[]> {
  return http.get<CnhVencendoResumo[]>('/patrimonio/frota/cnh-vencendo', { query: { dias }, signal });
}

function listarManutencoesAbertas(signal?: AbortSignal): Promise<ManutencaoAbertaResumo[]> {
  return http.get<ManutencaoAbertaResumo[]>('/patrimonio/frota/manutencoes/abertas', { signal });
}

// ---------------------------------------------------------------------------
// Hooks — Queries
// ---------------------------------------------------------------------------

/** KPIs agregados da frota + custo/consumo por veículo no período (ObterPainelFrota). */
export function usePainelFrota(de: string, ate: string, enabled = true) {
  return useQuery({
    queryKey: frotaKeys.painel(de, ate),
    queryFn: ({ signal }) => obterPainel(de, ate, signal),
    enabled: enabled && de !== '' && ate !== '',
    placeholderData: (anterior) => anterior,
  });
}

/** Condutores com CNH a vencer/vencida na janela de dias (ListarCnhVencendo). */
export function useCnhVencendo(dias: number) {
  return useQuery({
    queryKey: frotaKeys.cnhVencendo(dias),
    queryFn: ({ signal }) => listarCnhVencendo(dias, signal),
    enabled: Number.isInteger(dias) && dias >= 0,
  });
}

/** Ordens de serviço de manutenção em aberto de toda a frota (ListarManutencoesAbertas). */
export function useManutencoesAbertas() {
  return useQuery({
    queryKey: frotaKeys.manutencoesAbertas(),
    queryFn: ({ signal }) => listarManutencoesAbertas(signal),
  });
}
