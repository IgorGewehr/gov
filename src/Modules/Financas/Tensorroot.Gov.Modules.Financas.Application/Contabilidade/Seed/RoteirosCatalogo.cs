using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Seed;

/// <summary>Definição declarativa de um roteiro (evento contábil) do seed.</summary>
/// <param name="Fato">Fato coberto.</param>
/// <param name="Codigo">Código do roteiro.</param>
/// <param name="Nome">Nome.</param>
/// <param name="Linhas">Linhas (partidas roteirizadas).</param>
public sealed record RoteiroSeed(FatoContabil Fato, string Codigo, string Nome, IReadOnlyList<LinhaRoteiro> Linhas);

/// <summary>
/// Mapa "evento do ciclo → partidas contábeis" (MCASP §2), parametrizado por tenant. Códigos das
/// classes 5/6 são fixos (confirmados na fonte); pares patrimoniais (1-4) usam <see cref="PapelConta"/>
/// resolvido pelo mapa do tenant. [validar-plano-oficial] nos pares patrimoniais (LCP/IPC 03).
/// </summary>
public static class RoteirosCatalogo
{
    private const NaturezaInformacao Orc = NaturezaInformacao.Orcamentaria;
    private const NaturezaInformacao Pat = NaturezaInformacao.Patrimonial;
    private const LadoPartida D = LadoPartida.Debito;
    private const LadoPartida C = LadoPartida.Credito;

    /// <summary>Roteiros do seed.</summary>
    /// <returns>Definições de roteiro.</returns>
    public static IReadOnlyList<RoteiroSeed> Roteiros() =>
    [
        // Fixacao (LOA/credito adicional) — Orcamentaria: D Dotacao (5) / C Credito Disponivel (6).
        new(FatoContabil.DotacaoAprovada, "EVT-DOT", "Fixacao da Despesa",
        [
            LinhaRoteiro.PorCodigo(D, Orc, "5.2.2.1.01"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.1.00.00"),
        ]),

        // Empenho — Orcamentaria: D Credito Disponivel / C Empenhado a Liquidar.
        new(FatoContabil.EmpenhoEmitido, "EVT-EMP", "Empenho",
        [
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.2.1.1.00.00"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.3.01.00"),
        ]),

        // Anulacao de empenho — inverso.
        new(FatoContabil.EmpenhoAnulado, "EVT-EMP-ANUL", "Anulacao de Empenho",
        [
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.2.1.3.01.00"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.1.00.00"),
        ]),

        // Liquidacao — 2 lancamentos (Orcamentario + Patrimonial).
        new(FatoContabil.DespesaLiquidada, "EVT-LIQ", "Liquidacao da Despesa",
        [
            // Orcamentario: D Empenhado a Liquidar / C Liquidado a Pagar.
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.2.1.3.01.00"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.3.03.00"),
            // Patrimonial: D VPD Servicos / C Fornecedores CP. [validar-plano-oficial]
            LinhaRoteiro.PorPapel(D, Pat, PapelConta.VpdServicos),
            LinhaRoteiro.PorPapel(C, Pat, PapelConta.FornecedoresCP),
        ]),

        // Estorno de liquidacao — inverso de ambos os blocos.
        new(FatoContabil.LiquidacaoEstornada, "EVT-LIQ-EST", "Estorno de Liquidacao",
        [
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.2.1.3.03.00"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.3.01.00"),
            LinhaRoteiro.PorPapel(D, Pat, PapelConta.FornecedoresCP),
            LinhaRoteiro.PorPapel(C, Pat, PapelConta.VpdServicos),
        ]),

        // Pagamento — 2 lancamentos (Orcamentario + Patrimonial/financeiro).
        new(FatoContabil.PagamentoEfetuado, "EVT-PAG", "Pagamento da Despesa",
        [
            // Orcamentario: D Liquidado a Pagar / C Liquidado Pago.
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.2.1.3.03.00"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.3.04.00"),
            // Patrimonial: D Fornecedores CP (baixa do passivo) / C Caixa. [validar-plano-oficial]
            LinhaRoteiro.PorPapel(D, Pat, PapelConta.FornecedoresCP),
            LinhaRoteiro.PorPapel(C, Pat, PapelConta.CaixaEquivalentes),
        ]),

        // Receita arrecadada — 2 lancamentos (Orcamentario + Patrimonial).
        new(FatoContabil.ReceitaArrecadada, "EVT-REC", "Arrecadacao de Receita",
        [
            // Orcamentario: D Receita a Realizar / C Receita Realizada.
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.1.1.01"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.1.2.01"),
            // Patrimonial: D Caixa / C VPA. [validar-plano-oficial]
            LinhaRoteiro.PorPapel(D, Pat, PapelConta.CaixaEquivalentes),
            LinhaRoteiro.PorPapel(C, Pat, PapelConta.VpaTributos),
        ]),

        // RP Nao Processado inscrito (empenhado nao liquidado) — Orcamentaria:
        // D Empenhado a Liquidar / C Empenhos a Liquidar Inscritos em RP Nao Processados.
        new(FatoContabil.RestoAPagarInscrito, "EVT-RP-INSC", "Inscricao de RP Nao Processado",
        [
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.2.1.3.01.00"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.3.05.00"),
        ]),

        // RP Processado inscrito (liquidado nao pago) — Orcamentaria:
        // D Liquidado a Pagar / C Empenhos Liquidados Inscritos em RP Processados.
        new(FatoContabil.RestoAPagarProcessadoInscrito, "EVT-RP-INSC-PROC", "Inscricao de RP Processado",
        [
            LinhaRoteiro.PorCodigo(D, Orc, "6.2.2.1.3.03.00"),
            LinhaRoteiro.PorCodigo(C, Orc, "6.2.2.1.3.07.00"),
        ]),
    ];

    /// <summary>
    /// Mapa default de papel → código PCASP usado para resolver as linhas patrimoniais. O tenant pode
    /// sobrepor; aqui ficam os defaults [validar-plano-oficial].
    /// </summary>
    /// <returns>Dicionário papel → código.</returns>
    public static IReadOnlyDictionary<PapelConta, string> MapaPapelCodigoDefault() => new Dictionary<PapelConta, string>
    {
        [PapelConta.CaixaEquivalentes] = "1.1.1.1.01",
        [PapelConta.FornecedoresCP] = "2.1.3.1.01",
        [PapelConta.VpdServicos] = "3.3.2.1.01",
        [PapelConta.VpaTributos] = "4.1.1.1.01",
        [PapelConta.CreditoDisponivel] = "6.2.2.1.1.00.00",
        [PapelConta.CreditoEmpenhadoALiquidar] = "6.2.2.1.3.01.00",
        [PapelConta.CreditoEmpenhadoEmLiquidacao] = "6.2.2.1.3.02.00",
        [PapelConta.CreditoLiquidadoAPagar] = "6.2.2.1.3.03.00",
        [PapelConta.CreditoLiquidadoPago] = "6.2.2.1.3.04.00",
        [PapelConta.DotacaoOrcamentaria] = "5.2.2.1.01",
        [PapelConta.ReceitaARealizar] = "6.2.1.1.01",
        [PapelConta.ReceitaRealizada] = "6.2.1.2.01",
        [PapelConta.RestoAPagarNaoProcessado] = "6.2.2.1.3.05.00",
    };
}
