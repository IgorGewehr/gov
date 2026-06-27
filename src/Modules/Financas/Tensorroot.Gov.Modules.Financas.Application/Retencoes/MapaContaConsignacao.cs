using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>
/// Mapa "natureza da retenção → conta de consignação a recolher" (PCASP 2.1.8.8.1.xx). Liga cada
/// tributo/consignação retido ao passivo extra-orçamentário correspondente. Convenção de seed
/// (parametrizável por tenant via roteiro/plano — CLAUDE.md §7). [validar-plano-oficial]
/// </summary>
public static class MapaContaConsignacao
{
    private const string IrrfARecolher = "2.1.8.8.1.04";
    private const string InssARecolher = "2.1.8.8.1.05";
    private const string IssARecolher = "2.1.8.8.1.06";
    private const string OutrasARecolher = "2.1.8.8.1.99";

    /// <summary>Resolve o código da conta de consignação a recolher para a natureza informada.</summary>
    /// <param name="natureza">Natureza da retenção.</param>
    /// <returns>Código PCASP da conta de consignação.</returns>
    public static string CodigoConta(NaturezaRetencao natureza) => natureza switch
    {
        NaturezaRetencao.IrrfPessoaJuridica => IrrfARecolher,
        NaturezaRetencao.IrrfPessoaFisica => IrrfARecolher,
        NaturezaRetencao.InssRetido => InssARecolher,
        NaturezaRetencao.IssRetido => IssARecolher,
        NaturezaRetencao.ContribuicoesFederais => OutrasARecolher,
        NaturezaRetencao.CaucaoGarantia => OutrasARecolher,
        _ => OutrasARecolher,
    };

    /// <summary>Conta de Fornecedores a Pagar (contrapartida da reclassificação na liquidação).</summary>
    public const string CodigoFornecedores = "2.1.3.1.01";

    /// <summary>Conta de Caixa e Equivalentes (contrapartida do recolhimento).</summary>
    public const string CodigoCaixa = "1.1.1.1.01";
}
