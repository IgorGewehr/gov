// Camada de API da MSC (Matriz de Saldos Contábeis) do módulo Finanças. Espelha o
// contrato REAL de FinancasEndpoints.cs -> MapearMscEDemonstracoes:
//   POST /financas/contabilidade/msc/gerar { exercicio, mes, poderOrgao? }
//        -> { eventId, quantidadeLinhas, jaExistia }  (financas.gerenciar)
//
// A geração é IDEMPOTENTE: gerar de novo o mesmo exercício/mês devolve jaExistia=true
// com o eventId e a quantidade já registrados. A TRANSMISSÃO ao SICONFI/TCE é etapa
// SEPARADA (não disparada aqui): aqui apenas materializamos a matriz a partir do
// balancete e publicamos o MSCGeradaIntegrationEvent via Outbox.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { http } from '../../../api/http';
import { contabilidadeKeys } from './contabilidade.api';

// ---------------------------------------------------------------------------
// DTOs (contrato REAL)
// ---------------------------------------------------------------------------

/** Comando de geração da MSC (GerarMscCommand). PoderOrgao é opcional (tabela PO). */
export interface GerarMscInput {
  exercicio: number;
  mes: number;
  /** Código Poder/Órgão (PO) do ente; opcional. */
  poderOrgao?: string;
}

/** Resultado da geração da MSC (GerarMscResultado). */
export interface GerarMscResultado {
  /** Identificador do evento de integração (MSCGeradaIntegrationEvent) publicado. */
  eventId: string;
  /** Quantidade de linhas da matriz gerada. */
  quantidadeLinhas: number;
  /** true se a MSC do período já havia sido gerada (idempotência). */
  jaExistia: boolean;
}

// ---------------------------------------------------------------------------
// Função de acesso
// ---------------------------------------------------------------------------

function gerarMsc(input: GerarMscInput): Promise<GerarMscResultado> {
  return http.post<GerarMscResultado>('/financas/contabilidade/msc/gerar', input);
}

// ---------------------------------------------------------------------------
// Hook TanStack Query
// ---------------------------------------------------------------------------

/**
 * Gera (ou recupera, se já existir) a MSC de um período. Invalida o cache da
 * contabilidade em caso de sucesso, pois a matriz deriva do balancete.
 */
export function useGerarMsc() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: gerarMsc,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contabilidadeKeys.all });
    },
  });
}
