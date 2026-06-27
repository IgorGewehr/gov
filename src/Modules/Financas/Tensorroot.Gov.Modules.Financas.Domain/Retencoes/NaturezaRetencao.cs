namespace Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

/// <summary>
/// Natureza/tipo do tributo ou consignação retido na fonte sobre o pagamento a fornecedores/credores.
/// É consignação extra-orçamentária (Lei 4.320/64, art. 3º, parágrafo único c/c MCASP — ingresso e
/// dispêndio extra-orçamentário): o ente apenas detém o valor de terceiros até o recolhimento.
/// </summary>
public enum NaturezaRetencao
{
    /// <summary>Imposto de Renda Retido na Fonte sobre pagamentos a PJ (IN RFB 1.234/2012).</summary>
    IrrfPessoaJuridica = 1,

    /// <summary>Imposto de Renda Retido na Fonte sobre pagamentos a pessoa física (rendimentos do trabalho não assalariado, RRA).</summary>
    IrrfPessoaFisica = 2,

    /// <summary>Contribuição previdenciária (INSS) retida sobre cessão de mão de obra/empreitada (Lei 8.212/91, art. 31).</summary>
    InssRetido = 3,

    /// <summary>ISS retido na fonte pelo tomador (LC 116/2003, art. 6º; legislação municipal).</summary>
    IssRetido = 4,

    /// <summary>Contribuições sociais federais (CSLL/COFINS/PIS-PASEP) retidas (IN RFB 1.234/2012, código 5952).</summary>
    ContribuicoesFederais = 5,

    /// <summary>Caução / retenção contratual de garantia (Lei 14.133/2021, art. 96).</summary>
    CaucaoGarantia = 6,

    /// <summary>Outras consignações/retenções a recolher a terceiros.</summary>
    Outras = 99,
}
