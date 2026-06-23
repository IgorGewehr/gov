// Camada de API do CICLO ANUAL da folha (13º salário, férias e rescisão) do módulo
// RecursosHumanos. Cada geração produz UMA folha do Tipo próprio (DecimoTerceiro/Ferias/
// Rescisao) — o demonstrativo da folha resultante é visto pelo contracheque existente.
// Segue o PADRÃO-OURO: DTOs no topo, acesso via http client tipado, hooks de mutation.
//
// Contrato real (NÃO inventado): casado campo-a-campo com os commands em
//   src/Modules/RecursosHumanos/...Application/CicloAnual/GerarDecimoTerceiro.cs
//   .../GerarFerias.cs  .../GerarVerbasRescisorias.cs
// e as rotas em ...Infrastructure/RecursosHumanosEndpoints.cs (grupo /ciclo-anual).
// Cada endpoint retorna { folhaId } (a folha gerada/reaberta da competência).
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs — espelham os records de comando do backend (mesmos nomes de campo).
// ---------------------------------------------------------------------------

/** Resposta das gerações do ciclo anual (a folha do tipo próprio gerada/reaberta). */
export interface FolhaGeradaResponse {
  folhaId: string;
}

/**
 * Entrada da geração do 13º salário (GerarDecimoTerceiroCommand). Parcela 1 = adiantamento
 * sem descontos; parcela 2 = integral com INSS/RPPS/IRRF próprios do 13º (base separada).
 */
export interface GerarDecimoTerceiroInput {
  servidorId: string;
  ano: number;
  mesCompetencia: number;
  /** 1 = adiantamento (1ª parcela); 2 = integral com descontos (2ª parcela). */
  parcela: number;
  remuneracaoBase: number;
  /** Admissão/exercício (DateOnly AAAA-MM-DD): início da contagem de avos no ano. */
  admissao: string;
}

/**
 * Entrada da geração de férias (GerarFeriasCommand): remuneração + 1/3 constitucional e,
 * opcionalmente, abono pecuniário (venda de até 1/3) com seu terço.
 */
export interface GerarFeriasInput {
  servidorId: string;
  ano: number;
  mes: number;
  remuneracaoMensal: number;
  /** Dias de férias gozados (0..30). */
  diasGozados: number;
  /** Dias convertidos em abono pecuniário (0..10). */
  diasVendidos: number;
}

/**
 * Entrada da geração de verbas rescisórias (GerarVerbasRescisoriasCommand): verbas devidas
 * compostas pela matriz (tipo de desligamento × regime). Aviso/multa só para celetista.
 */
export interface GerarRescisaoInput {
  servidorId: string;
  /** Data de encerramento do vínculo (DateOnly AAAA-MM-DD) — define a competência. */
  dataDesligamento: string;
  /** Tipo de desligamento (enum numérico TipoDesligamento). */
  tipoDesligamento: number;
  /** Regime jurídico do vínculo (1 = Estatutário, 2 = Celetista). */
  regime: number;
  vencimento: number;
  /** Dias trabalhados no mês do desligamento (saldo de salário, 0..31). */
  diasTrabalhadosNoMes: number;
  /** Admissão/exercício (DateOnly): início da contagem de avos do 13º no ano. */
  admissao: string;
  /** Início do período aquisitivo de férias em curso (DateOnly). */
  inicioPeriodoAquisitivoFerias: string;
  /** Dias de férias vencidas (períodos completos não gozados). */
  diasFeriasVencidas: number;
  /** Valor do aviso prévio (só celetista) — informado pelo operador. */
  valorAvisoPrevio: number;
  /** Valor da multa de 40% do FGTS (só celetista) — informado pelo operador. */
  valorMultaFgts: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function gerarDecimoTerceiro(input: GerarDecimoTerceiroInput): Promise<FolhaGeradaResponse> {
  return http.post<FolhaGeradaResponse>(
    '/recursoshumanos/ciclo-anual/decimo-terceiro',
    input,
  );
}

function gerarFerias(input: GerarFeriasInput): Promise<FolhaGeradaResponse> {
  return http.post<FolhaGeradaResponse>('/recursoshumanos/ciclo-anual/ferias', input);
}

function gerarRescisao(input: GerarRescisaoInput): Promise<FolhaGeradaResponse> {
  return http.post<FolhaGeradaResponse>('/recursoshumanos/ciclo-anual/rescisao', input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query (mutations) — invalidam as consultas de folha.
// ---------------------------------------------------------------------------

/** Gera a folha de 13º salário (parcela 1 ou 2) e invalida as consultas de folha. */
export function useGerarDecimoTerceiro() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarDecimoTerceiro,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}

/** Gera a folha de férias (gozadas + 1/3 + abono opcional) e invalida as folhas. */
export function useGerarFerias() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarFerias,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}

/** Gera a folha de rescisão (verbas por tipo de desligamento × regime) e invalida as folhas. */
export function useGerarRescisao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarRescisao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.folhas() });
    },
  });
}
