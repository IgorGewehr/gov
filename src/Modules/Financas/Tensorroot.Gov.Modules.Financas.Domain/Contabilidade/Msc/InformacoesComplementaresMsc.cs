using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;

/// <summary>
/// Informacoes complementares (atributos) de uma linha da MSC (Value Object). Modela os 9 tipos do quadro
/// oficial da MSC SICONFI (Anexo II Portaria STN 642/2019). Todos os campos sao opcionais: no M3 apenas
/// <see cref="PoderOrgao"/> (do tenant) e <see cref="AtributoSuperavitFinanceiro"/> (do indicador F/P da
/// conta) sao preenchidos; os demais dependem de atributos (FR/ND/NR/FS/CO/DC/AI) que o ciclo orcamentario
/// ainda nao carrega na partida contabil (M3.x/M4). // TODO(validar-oficial): codigos/tamanhos das tabelas.
/// </summary>
public sealed class InformacoesComplementaresMsc : ValueObject
{
    private InformacoesComplementaresMsc(
        string? poderOrgao,
        string? atributoSuperavitFinanceiro,
        string? dividaConsolidada,
        string? fonteRecurso,
        string? codigoAcompanhamento,
        string? naturezaReceita,
        string? naturezaDespesa,
        string? funcaoSubfuncao,
        string? anoInscricaoRp)
    {
        PoderOrgao = poderOrgao;
        AtributoSuperavitFinanceiro = atributoSuperavitFinanceiro;
        DividaConsolidada = dividaConsolidada;
        FonteRecurso = fonteRecurso;
        CodigoAcompanhamento = codigoAcompanhamento;
        NaturezaReceita = naturezaReceita;
        NaturezaDespesa = naturezaDespesa;
        FuncaoSubfuncao = funcaoSubfuncao;
        AnoInscricaoRp = anoInscricaoRp;
    }

    /// <summary>Poder/Orgao (PO, 5 digitos). [validar-oficial] tabela PO.</summary>
    public string? PoderOrgao { get; }

    /// <summary>Atributo Superavit Financeiro (FP: 1=Financeiro, 2=Permanente). Classes 1/2.</summary>
    public string? AtributoSuperavitFinanceiro { get; }

    /// <summary>Divida Consolidada (DC, 1 digito). // TODO(validar-oficial).</summary>
    public string? DividaConsolidada { get; }

    /// <summary>Fonte/Destinacao de Recurso (FR, 4 digitos). [validar-oficial] Port. 710/2021.</summary>
    public string? FonteRecurso { get; }

    /// <summary>Cod. Acompanhamento Exec. Orcam. (CO, 4 digitos). // TODO(validar-oficial).</summary>
    public string? CodigoAcompanhamento { get; }

    /// <summary>Natureza da Receita (NR, 8 digitos). [validar-oficial] Port. 163/2001.</summary>
    public string? NaturezaReceita { get; }

    /// <summary>Natureza da Despesa (ND, 8 digitos). [validar-oficial] Port. 163/2001.</summary>
    public string? NaturezaDespesa { get; }

    /// <summary>Funcao + Subfuncao (FS, 5 digitos). [validar-oficial] Port. MOG 42/1999.</summary>
    public string? FuncaoSubfuncao { get; }

    /// <summary>Ano de Inscricao em Restos a Pagar (AI, 4 digitos). M3.x (RP).</summary>
    public string? AnoInscricaoRp { get; }

    /// <summary>Cria o bloco de informacoes complementares. Campos nao disponiveis ficam nulos.</summary>
    /// <param name="poderOrgao">Poder/Orgao (PO).</param>
    /// <param name="atributoSuperavitFinanceiro">Atributo F/P (FP).</param>
    /// <param name="dividaConsolidada">Divida Consolidada (DC).</param>
    /// <param name="fonteRecurso">Fonte de Recurso (FR).</param>
    /// <param name="codigoAcompanhamento">Cod. Acompanhamento (CO).</param>
    /// <param name="naturezaReceita">Natureza da Receita (NR).</param>
    /// <param name="naturezaDespesa">Natureza da Despesa (ND).</param>
    /// <param name="funcaoSubfuncao">Funcao/Subfuncao (FS).</param>
    /// <param name="anoInscricaoRp">Ano Inscricao RP (AI).</param>
    /// <returns>Novo <see cref="InformacoesComplementaresMsc"/>.</returns>
    public static InformacoesComplementaresMsc Criar(
        string? poderOrgao = null,
        string? atributoSuperavitFinanceiro = null,
        string? dividaConsolidada = null,
        string? fonteRecurso = null,
        string? codigoAcompanhamento = null,
        string? naturezaReceita = null,
        string? naturezaDespesa = null,
        string? funcaoSubfuncao = null,
        string? anoInscricaoRp = null)
        => new(
            poderOrgao,
            atributoSuperavitFinanceiro,
            dividaConsolidada,
            fonteRecurso,
            codigoAcompanhamento,
            naturezaReceita,
            naturezaDespesa,
            funcaoSubfuncao,
            anoInscricaoRp);

    /// <summary>
    /// Representacao textual canonica das informacoes complementares presentes (formato <c>CHAVE=valor</c>
    /// separado por <c>;</c>), para o campo de compatibilidade do contrato.
    /// </summary>
    /// <returns>Texto com os atributos preenchidos, ou <c>null</c> se nenhum.</returns>
    public string? ParaTexto()
    {
        var partes = new List<string>(9);
        Adicionar(partes, "PO", PoderOrgao);
        Adicionar(partes, "FP", AtributoSuperavitFinanceiro);
        Adicionar(partes, "DC", DividaConsolidada);
        Adicionar(partes, "FR", FonteRecurso);
        Adicionar(partes, "CO", CodigoAcompanhamento);
        Adicionar(partes, "NR", NaturezaReceita);
        Adicionar(partes, "ND", NaturezaDespesa);
        Adicionar(partes, "FS", FuncaoSubfuncao);
        Adicionar(partes, "AI", AnoInscricaoRp);
        return partes.Count == 0 ? null : string.Join(';', partes);
    }

    private static void Adicionar(List<string> partes, string chave, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            partes.Add($"{chave}={valor}");
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PoderOrgao;
        yield return AtributoSuperavitFinanceiro;
        yield return DividaConsolidada;
        yield return FonteRecurso;
        yield return CodigoAcompanhamento;
        yield return NaturezaReceita;
        yield return NaturezaDespesa;
        yield return FuncaoSubfuncao;
        yield return AnoInscricaoRp;
    }
}
