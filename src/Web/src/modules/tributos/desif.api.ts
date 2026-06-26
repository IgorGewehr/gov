// Camada de API do submódulo DES-IF — Apuração Mensal do ISSQN das instituições financeiras (ABRASF).
// Espelha o contrato REAL da Minimal API /api/tributos:
//   POST /api/tributos/desif/contribuintes/{id}/apuracao-mensal -> EntregarDesif -> ResultadoDesif [tributos.gerenciar]
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

/** Subtítulo COSIF tributável declarado na DES-IF (back: SubtituloDesifInput — Registro 0430). */
export interface SubtituloDesifInput {
  /** Conta/subtítulo do Plano Contábil COSIF (ex.: "7.1.7.99.00-8"). */
  contaCosif: string;
  /** Código de tributação da tabela DES-IF (Anexo 6 ABRASF). */
  codigoTributacaoDesif: string;
  /** Item da lista de serviços LC 116 correlato (ex.: "15.01"). */
  itemListaServico: string;
  descricao: string;
  /** Receita tributável do subtítulo (base de cálculo, R$). */
  baseCalculo: number;
  /** Alíquota aplicável em % (ex.: 5.0 = 5%). */
  aliquotaPercentual: number;
}

/** EntregarDesifPayload (apuração mensal -> lançamento do ISSQN a recolher). */
export interface EntregarDesifInput {
  ano: number;
  mes: number;
  /** Fundamento legal da obrigação acessória (CTM), obrigatório. */
  fundamentoLegal: string;
  /** Deduções da receita declarada (Registro 0440, R$). */
  deducoesReceita: number;
  /** Incentivos fiscais autorizados em lei (R$). */
  incentivosFiscais: number;
  /** Depósitos judiciais — suspendem a exigibilidade (R$). */
  depositosJudiciais: number;
  /** Vencimento do ISSQN a recolher ("yyyy-MM-dd"), obrigatório. */
  vencimentoIssqn: string;
  subtitulos: SubtituloDesifInput[];
}

/** Resultado da entrega da DES-IF (espelha ResultadoDesif). */
export interface ResultadoDesif {
  declaracaoId: string;
  lancamentoId: string | null;
  quantidadeSubtitulos: number;
  receitaTributavelTotal: number;
  issqnDevidoBruto: number;
  issqnARecolher: number;
}

function entregarDesif(contribuinteId: string, input: EntregarDesifInput): Promise<ResultadoDesif> {
  return http.post<ResultadoDesif>(
    `/tributos/desif/contribuintes/${contribuinteId}/apuracao-mensal`,
    input,
  );
}

/** ENTREGA a DES-IF mensal (Módulo 2) do contribuinte (constitui o lançamento do ISSQN a recolher). */
export function useEntregarDesif(contribuinteId: string) {
  return useMutation({
    mutationFn: (input: EntregarDesifInput) => entregarDesif(contribuinteId, input),
  });
}
