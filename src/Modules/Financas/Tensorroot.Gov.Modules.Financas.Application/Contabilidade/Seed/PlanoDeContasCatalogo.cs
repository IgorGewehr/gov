using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Seed;

/// <summary>
/// Catálogo mínimo do PCASP (Federação) usado para semear o plano de contas por tenant na ativação
/// do módulo. Contém as contas-chave do ciclo da despesa/receita (classes 5/6) confirmadas na fonte
/// e as patrimoniais essenciais (classes 1-4) modeladas fiéis ao conceito.
/// [validar-plano-oficial]: substituir pelo elenco completo do PCASP 2026 (Portaria STN/MF 3.133/2025).
/// </summary>
public static class PlanoDeContasCatalogo
{
    /// <summary>Contas do seed, em ordem topológica (pais antes dos filhos).</summary>
    /// <returns>Definições de conta.</returns>
    public static IReadOnlyList<ContaSeed> Contas() =>
    [
        // ---- Classe 1 — Ativo (Patrimonial, Devedora) ----
        Sint("1", "Ativo", "Bens e direitos.", "Debita pelos ingressos; credita pelas baixas.", IndicadorSuperavitFinanceiro.Financeiro),
        Sint("1.1", "Ativo Circulante", "Ativos realizaveis ate 12 meses.", "Debita por ingressos.", IndicadorSuperavitFinanceiro.Financeiro),
        Sint("1.1.1", "Caixa e Equivalentes de Caixa", "Disponibilidades imediatas.", "Debita por entradas; credita por saidas.", IndicadorSuperavitFinanceiro.Financeiro),
        Sint("1.1.1.1", "Caixa e Equivalentes em Moeda Nacional", "Disponibilidades em BRL.", "Debita por entradas.", IndicadorSuperavitFinanceiro.Financeiro),
        Anal("1.1.1.1.01", "Caixa e Equivalentes — Conta Movimento", "Conta corrente do ente.", "Debita por recebimentos; credita por pagamentos.", IndicadorSuperavitFinanceiro.Financeiro),

        // ---- Classe 2 — Passivo e PL (Patrimonial, Credora) ----
        Sint("2", "Passivo e Patrimonio Liquido", "Obrigacoes e PL.", "Credita por incorrer; debita por baixar.", IndicadorSuperavitFinanceiro.Financeiro),
        Sint("2.1", "Passivo Circulante", "Obrigacoes ate 12 meses.", "Credita por incorrer.", IndicadorSuperavitFinanceiro.Financeiro),
        Sint("2.1.3", "Fornecedores e Contas a Pagar de Curto Prazo", "Obrigacoes com fornecedores.", "Credita na liquidacao; debita no pagamento.", IndicadorSuperavitFinanceiro.Financeiro),
        Anal("2.1.3.1.01", "Fornecedores Nacionais a Pagar", "Fornecedores nacionais.", "Credita na liquidacao; debita no pagamento.", IndicadorSuperavitFinanceiro.Financeiro),

        // ---- Classe 3 — VPD (Patrimonial, Devedora, encerra) ----
        Sint("3", "Variacao Patrimonial Diminutiva", "Reducoes do PL.", "Debita por incorrer.", encerramento: true),
        Sint("3.3", "Uso de Bens, Servicos e Consumo de Capital Fixo", "Servicos/consumo.", "Debita por incorrer.", encerramento: true),
        Sint("3.3.2", "Servicos", "Servicos por competencia.", "Debita por incorrer.", encerramento: true),
        Anal("3.3.2.1.01", "Servicos — VPD", "Despesa com servicos por competencia.", "Debita no fato gerador.", encerramento: true),

        // ---- Classe 4 — VPA (Patrimonial, Credora, encerra) ----
        Sint("4", "Variacao Patrimonial Aumentativa", "Aumentos do PL.", "Credita por reconhecer.", encerramento: true),
        Sint("4.1", "Impostos, Taxas e Contribuicoes de Melhoria", "Receita tributaria por competencia.", "Credita no reconhecimento.", encerramento: true),
        Sint("4.1.1", "Impostos", "Receita de impostos por competencia.", "Credita no reconhecimento.", encerramento: true),
        Anal("4.1.1.1.01", "Impostos — VPA", "Receita de impostos.", "Credita no reconhecimento.", encerramento: true),

        // ---- Classe 5 — Controles da Aprovacao (Orcamentaria, Devedora, encerra) ----
        Sint("5", "Controles da Aprovacao do Planejamento e Orcamento", "Previsao/fixacao.", "Debita na aprovacao.", encerramento: true),
        Sint("5.2", "Fixacao da Despesa", "Fixacao orcamentaria da despesa.", "Debita pela fixacao.", encerramento: true),
        Sint("5.2.2", "Fixacao da Despesa", "Fixacao detalhada.", "Debita pela fixacao.", encerramento: true),
        Sint("5.2.2.1", "Fixacao da Despesa", "Dotacao fixada.", "Debita pela fixacao.", encerramento: true),
        Anal("5.2.2.1.01", "Dotacao Orcamentaria (Inicial e Adicional)", "Dotacao fixada na LOA/creditos.", "Debita na aprovacao da dotacao.", encerramento: true),

        // ---- Classe 6 — Controles da Execucao (Orcamentaria, Credora, encerra) ----
        Sint("6", "Controles da Execucao do Planejamento e Orcamento", "Execucao do orcamento.", "Credita na execucao.", encerramento: true),
        Sint("6.2", "Execucao do Planejamento e Orcamento", "Execucao da receita/despesa.", "Credita na execucao.", encerramento: true),
        // Receita (6.2.1)
        Sint("6.2.1", "Execucao da Receita", "Realizacao da receita.", "Credita na arrecadacao.", encerramento: true),
        Sint("6.2.1.1", "Receita a Realizar", "Saldo a arrecadar.", "Debita por arrecadar; credita na previsao.", encerramento: true),
        Anal("6.2.1.1.01", "Receita Orcamentaria a Realizar", "Saldo a arrecadar.", "Debita na arrecadacao.", encerramento: true),
        Sint("6.2.1.2", "Receita Realizada", "Receita arrecadada.", "Credita na arrecadacao.", encerramento: true),
        Anal("6.2.1.2.01", "Receita Orcamentaria Realizada", "Receita arrecadada.", "Credita na arrecadacao.", encerramento: true),
        // Despesa (6.2.2)
        Sint("6.2.2", "Execucao da Despesa", "Execucao da despesa.", "Credita/debita na cadeia da despesa.", encerramento: true),
        Sint("6.2.2.1", "Disponibilidade por Destinacao", "Cadeia de credito.", "Transfere saldo entre estagios.", encerramento: true),
        Sint("6.2.2.1.1", "Credito Disponivel", "Credito ainda nao empenhado.", "Credita na fixacao; debita no empenho.", encerramento: true),
        Anal("6.2.2.1.1.00.00", "Credito Disponivel", "Credito orcamentario disponivel.", "Credita na fixacao; debita no empenho.", encerramento: true),
        Sint("6.2.2.1.3", "Credito Utilizado", "Estagios de utilizacao do credito.", "Transfere saldo entre estagios.", encerramento: true),
        Anal("6.2.2.1.3.01.00", "Credito Empenhado a Liquidar", "Empenhado ainda nao liquidado.", "Credita no empenho; debita na liquidacao.", encerramento: true),
        Anal("6.2.2.1.3.02.00", "Credito Empenhado em Liquidacao", "Fato gerador ocorrido, sem conferencia.", "Credita ao entrar; debita ao liquidar.", encerramento: true),
        Anal("6.2.2.1.3.03.00", "Credito Empenhado Liquidado a Pagar", "Liquidado nao pago.", "Credita na liquidacao; debita no pagamento.", encerramento: true),
        Anal("6.2.2.1.3.04.00", "Credito Empenhado Liquidado Pago", "Liquidado pago.", "Credita no pagamento.", encerramento: true),
        Anal("6.2.2.1.3.05.00", "Empenhos a Liquidar Inscritos em RP Nao Processados", "RP nao processado.", "Credita na inscricao em RP.", encerramento: true),
    ];

    private static ContaSeed Sint(
        string codigo,
        string titulo,
        string funcao,
        string funcionamento,
        IndicadorSuperavitFinanceiro indicador = IndicadorSuperavitFinanceiro.NaoAplicavel,
        bool encerramento = false)
        => new(codigo, titulo, funcao, funcionamento, TipoConta.Sintetica, indicador, encerramento);

    private static ContaSeed Anal(
        string codigo,
        string titulo,
        string funcao,
        string funcionamento,
        IndicadorSuperavitFinanceiro indicador = IndicadorSuperavitFinanceiro.NaoAplicavel,
        bool encerramento = false)
        => new(codigo, titulo, funcao, funcionamento, TipoConta.Analitica, indicador, encerramento);
}
