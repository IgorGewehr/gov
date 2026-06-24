// Camada de API do submódulo GIA mensal de ISS — declaração do prestador (PARIDADE-PoC SW-A10).
// Espelha o contrato REAL da Minimal API /api/tributos:
//   POST /api/tributos/iss/contribuintes/{id}/gia -> EntregarGiaIss -> ResultadoGiaIss [tributos.gerenciar]
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Entradas de comando
// ---------------------------------------------------------------------------

/** Linha de serviço declarado na GIA (back: ServicoGiaInput). */
export interface ServicoGiaInput {
  /** Item da lista de serviços LC 116 (ex.: "7.02"). */
  itemListaServico: string;
  descricao: string;
  /** Base de cálculo (valor do serviço, R$). */
  baseCalculo: number;
  /** Alíquota aplicável em % (ex.: 2.0 = 2%). */
  aliquotaPercentual: number;
  /** Se o ISS desta linha foi retido na fonte pelo tomador. */
  retidoNaFonte: boolean;
}

/** EntregarGiaPayload (declaração mensal -> lançamento ISS devido). */
export interface EntregarGiaInput {
  ano: number;
  mes: number;
  /** Fundamento legal da obrigação acessória (CTM), obrigatório. */
  fundamentoLegal: string;
  /** Vencimento do ISS devido ("yyyy-MM-dd"), obrigatório. */
  vencimentoIssDevido: string;
  servicos: ServicoGiaInput[];
}

// ---------------------------------------------------------------------------
// Projeção de leitura
// ---------------------------------------------------------------------------

/** Resultado da entrega da GIA (espelha ResultadoGiaIss). */
export interface ResultadoGiaIss {
  declaracaoId: string;
  lancamentoId: string | null;
  quantidadeServicos: number;
  totalServicos: number;
  issDevido: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP + hook
// ---------------------------------------------------------------------------

function entregarGia(contribuinteId: string, input: EntregarGiaInput): Promise<ResultadoGiaIss> {
  return http.post<ResultadoGiaIss>(`/tributos/iss/contribuintes/${contribuinteId}/gia`, input);
}

/** ENTREGA a GIA mensal de ISS do contribuinte (constitui o lançamento do ISS devido). */
export function useEntregarGia(contribuinteId: string) {
  return useMutation({
    mutationFn: (input: EntregarGiaInput) => entregarGia(contribuinteId, input),
  });
}
