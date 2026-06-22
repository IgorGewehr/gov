// Helpers de apresentação das Demonstrações Contábeis (DCASP) e da MSC.
import type {
  BalancoFinanceiro,
  BalancoOrcamentario,
  BalancoPatrimonial,
  Demonstrativo,
  LinhaDemonstrativo,
  VariacoesPatrimoniais,
} from './demonstracoes.api';

/** Tolerância para considerar dois totais "fechados" (centavos). */
const TOLERANCIA_FECHAMENTO = 0.005;

/** Aba de demonstrativo (slug da rota + rótulo + anexo legal). */
export interface AbaDemonstrativo {
  slug: Demonstrativo;
  sigla: string;
  rotulo: string;
  anexo: string;
}

/** As 4 demonstrações expostas pela API M3 (na ordem da prestação de contas). */
export const ABAS_DEMONSTRATIVO: AbaDemonstrativo[] = [
  {
    slug: 'balanco-orcamentario',
    sigla: 'BO',
    rotulo: 'Balanço Orçamentário',
    anexo: 'Anexo 12 (Lei 4.320/64)',
  },
  {
    slug: 'balanco-financeiro',
    sigla: 'BF',
    rotulo: 'Balanço Financeiro',
    anexo: 'Anexo 13 (Lei 4.320/64)',
  },
  {
    slug: 'balanco-patrimonial',
    sigla: 'BP',
    rotulo: 'Balanço Patrimonial',
    anexo: 'Anexo 14 (Lei 4.320/64)',
  },
  {
    slug: 'variacoes-patrimoniais',
    sigla: 'DVP',
    rotulo: 'Variações Patrimoniais',
    anexo: 'Anexo 15 (Lei 4.320/64)',
  },
];

/** true se os dois lados estão equilibrados dentro da tolerância (ex.: Ativo = Passivo+PL). */
export function fechado(a: number, b: number): boolean {
  return Math.abs(a - b) <= TOLERANCIA_FECHAMENTO;
}

/** true quando o resultado/saldo é negativo (déficit), considerando a tolerância. */
export function ehDeficit(resultado: number): boolean {
  return resultado < -TOLERANCIA_FECHAMENTO;
}

/**
 * Rótulo do resultado de equilíbrio conforme o sinal: superávit (≥0) ou déficit (<0).
 * Usado em BO (orçamentário), BP (financeiro) e DVP (patrimonial).
 */
export function rotuloResultado(resultado: number, base: string): string {
  return ehDeficit(resultado) ? `Déficit ${base}` : `Superávit ${base}`;
}

/** Resolve a aba a partir do slug (ou a primeira como padrão). */
export function abaPorSlug(slug: string | null | undefined): AbaDemonstrativo {
  return ABAS_DEMONSTRATIVO.find((a) => a.slug === slug) ?? ABAS_DEMONSTRATIVO[0];
}

// ---------------------------------------------------------------------------
// Normalização para exibição (quadro + linha de equilíbrio)
// ---------------------------------------------------------------------------

/** Quadro normalizado para a tabela genérica da tela (título, colunas, linhas, total). */
export interface QuadroExibivel {
  titulo: string;
  colunas: string[];
  linhas: LinhaDemonstrativo[];
  /** Linha de total/equilíbrio destacada ao pé do quadro (opcional). */
  total?: LinhaDemonstrativo;
}

/** Linha única com um valor (atalho para totais). */
function linhaValor(rotulo: string, valor: number): LinhaDemonstrativo {
  return { linha: rotulo, valores: [valor] };
}

/** Normaliza o Balanço Orçamentário em quadros (Receitas, Despesas, Resultado). */
export function quadrosBalancoOrcamentario(d: BalancoOrcamentario): QuadroExibivel[] {
  return [
    {
      titulo: d.receitas.quadro,
      colunas: d.receitas.colunas,
      linhas: d.receitas.linhas,
      total: linhaValor('Total da receita realizada', d.totalReceitaRealizada),
    },
    {
      titulo: d.despesas.quadro,
      colunas: d.despesas.colunas,
      linhas: d.despesas.linhas,
      total: linhaValor('Total da despesa empenhada', d.totalDespesaEmpenhada),
    },
    {
      titulo: 'Resultado orçamentário',
      colunas: ['Valor'],
      linhas: [],
      total: linhaValor(
        rotuloResultado(d.resultadoOrcamentario, 'orçamentário'),
        d.resultadoOrcamentario,
      ),
    },
  ];
}

/** Normaliza o Balanço Financeiro em quadros (Ingressos × Dispêndios). */
export function quadrosBalancoFinanceiro(d: BalancoFinanceiro): QuadroExibivel[] {
  return [
    {
      titulo: 'Ingressos',
      colunas: ['Valor'],
      linhas: d.ingressos,
      total: linhaValor('Total dos ingressos', d.totalIngressos),
    },
    {
      titulo: 'Dispêndios',
      colunas: ['Valor'],
      linhas: d.dispendios,
      total: linhaValor('Total dos dispêndios', d.totalDispendios),
    },
  ];
}

/** Normaliza o Balanço Patrimonial em quadros (Ativo × Passivo+PL + superávit financeiro). */
export function quadrosBalancoPatrimonial(d: BalancoPatrimonial): QuadroExibivel[] {
  return [
    {
      titulo: 'Ativo',
      colunas: ['Valor'],
      linhas: d.ativo,
      total: linhaValor('Total do ativo', d.totalAtivo),
    },
    {
      titulo: 'Passivo + Patrimônio Líquido',
      colunas: ['Valor'],
      linhas: d.passivoPatrimonioLiquido,
      total: linhaValor('Total do passivo + PL', d.totalPassivoPl),
    },
    {
      titulo: 'Superávit/déficit financeiro (art. 105, Lei 4.320/64)',
      colunas: ['Valor'],
      linhas: [
        linhaValor('Ativo financeiro', d.ativoFinanceiro),
        linhaValor('Passivo financeiro', d.passivoFinanceiro),
      ],
      total: linhaValor(
        rotuloResultado(d.superavitFinanceiro, 'financeiro'),
        d.superavitFinanceiro,
      ),
    },
  ];
}

/** Normaliza a DVP em quadros (VPA × VPD + resultado patrimonial). */
export function quadrosVariacoesPatrimoniais(d: VariacoesPatrimoniais): QuadroExibivel[] {
  return [
    {
      titulo: 'Variações Patrimoniais Aumentativas (VPA)',
      colunas: ['Valor'],
      linhas: d.variacoesAumentativas,
      total: linhaValor('Total das VPA', d.totalVpa),
    },
    {
      titulo: 'Variações Patrimoniais Diminutivas (VPD)',
      colunas: ['Valor'],
      linhas: d.variacoesDiminutivas,
      total: linhaValor('Total das VPD', d.totalVpd),
    },
    {
      titulo: 'Resultado patrimonial do período',
      colunas: ['Valor'],
      linhas: [],
      total: linhaValor(
        rotuloResultado(d.resultadoPatrimonial, 'patrimonial'),
        d.resultadoPatrimonial,
      ),
    },
  ];
}
