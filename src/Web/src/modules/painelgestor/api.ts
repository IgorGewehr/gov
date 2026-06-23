// Camada de API do PAINEL DO GESTOR (dashboard executivo — visão prefeito/gestor).
// Espelha FIELMENTE o contrato REAL de PainelGestorEndpoints.cs + PainelGestorDtos.cs
// (grupo /api/painelgestor):
//   GET /painelgestor/indicadores/{exercicio} -> PainelGestorDto   [painel.ver]
//
// Convenções (PADRÃO-OURO de tributos/educação): DTOs no topo em camelCase, como o
// System.Text.Json serializa os records C#; enums por valor INT no payload;
// percentuais vêm do backend como FRAÇÃO (0..1); hooks por operação. Cada KPI
// degrada graciosamente (zeros / Indeterminado) — o backend nunca inventa número.
import { useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';

/** Permissão de gestor que libera leitura do painel (catálogo: painel.ver). */
export const PERM_PAINEL_VER = 'painel.ver';

/** Query keys do módulo (escopadas por exercício). */
export const painelGestorKeys = {
  all: ['painelgestor'] as const,
  indicadores: (exercicio: number) => ['painelgestor', 'indicadores', exercicio] as const,
};

// ---------------------------------------------------------------------------
// Enum SituacaoLimite — INT no payload; espelha PainelGestor.Domain.Limites.SituacaoLimite
// ---------------------------------------------------------------------------

/** Semáforo de conformidade (0=Indeterminado, 1=Adequado, 2=Alerta, 3=Excedido). */
export enum SituacaoLimite {
  Indeterminado = 0,
  Adequado = 1,
  Alerta = 2,
  Excedido = 3,
}

// ---------------------------------------------------------------------------
// DTOs de retorno (espelham os records C# de PainelGestorDtos.cs — 5 KPIs)
// ---------------------------------------------------------------------------

/** (a) ExecucaoOrcamentariaDto — empenhado/liquidado/pago vs dotação. Frações 0..1. */
export interface ExecucaoOrcamentariaDto {
  dotacaoAtualizada: number;
  empenhado: number;
  liquidado: number;
  pago: number;
  percentualEmpenhado: number;
  percentualLiquidado: number;
  percentualPago: number;
}

/** (b) MinimoConstitucionalDto — situação de um mínimo setorial (Saúde/Educação). */
export interface MinimoConstitucionalDto {
  /** Setor: "Saude" | "Educacao". */
  setor: string;
  receitaBase: number;
  aplicado: number;
  /** Fração 0..1. */
  percentualAplicado: number;
  /** Fração 0..1 (mínimo vigente — 0,15 Saúde / 0,25 Educação). */
  percentualMinimo: number;
  /** Situação: "Atingido" | "NaoAtingido". */
  situacao: string;
}

/** (c) ArrecadacaoDto — arrecadação tributária + posição da dívida ativa. */
export interface ArrecadacaoDto {
  arrecadacaoTributaria: number;
  dividaAtivaSaldoInscrito: number;
  dividaAtivaSaldoAjuizado: number;
  dividaAtivaRecuperada: number;
}

/** (d) DespesaPessoalLrfDto — custo de pessoal e % da RCL (LRF) com semáforo. */
export interface DespesaPessoalLrfDto {
  despesaPessoal: number;
  receitaCorrenteLiquida: number;
  /** Fração 0..1. */
  percentualDaRcl: number;
  /** Limites vigentes (frações 0..1). */
  limiteLegal: number;
  limitePrudencial: number;
  limiteAlerta: number;
  /** Valor INT do enum SituacaoLimite. */
  situacao: SituacaoLimite;
}

/** (e) PrestacaoContasDto — prontidão das remessas TCE-RS do exercício. */
export interface PrestacaoContasDto {
  remessasEnviadas: number;
  remessasComPrazoVencido: number;
  emDia: boolean;
  /** Valor INT do enum SituacaoLimite. */
  situacao: SituacaoLimite;
}

/** PainelGestorDto consolidado — os 5 KPIs reprodutíveis do exercício. */
export interface PainelGestorDto {
  exercicio: number;
  execucaoOrcamentaria: ExecucaoOrcamentariaDto;
  minimos: MinimoConstitucionalDto[];
  arrecadacao: ArrecadacaoDto;
  pessoalLrf: DespesaPessoalLrfDto;
  prestacaoContas: PrestacaoContasDto;
}

// ---------------------------------------------------------------------------
// Acesso HTTP + Hook TanStack Query
// ---------------------------------------------------------------------------

function obterIndicadores(exercicio: number, signal?: AbortSignal): Promise<PainelGestorDto> {
  return http.get<PainelGestorDto>(`/painelgestor/indicadores/${exercicio}`, { signal });
}

/** Indicadores consolidados do gestor de um exercício (5 KPIs). */
export function usePainelGestor(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: painelGestorKeys.indicadores(exercicio),
    queryFn: ({ signal }) => obterIndicadores(exercicio, signal),
    enabled: enabled && Number.isFinite(exercicio) && exercicio > 0,
  });
}
