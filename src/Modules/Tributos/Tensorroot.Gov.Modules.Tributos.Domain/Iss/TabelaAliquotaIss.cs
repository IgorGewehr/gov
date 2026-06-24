using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Iss;

/// <summary>Identificador forte do agregado <see cref="TabelaAliquotaIss"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaAliquotaIssId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaAliquotaIssId"/>.</returns>
    public static TabelaAliquotaIssId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tabela de alíquotas do ISS por item da lista de serviços (LC 116/2003), versionada por vigência
/// (LEI MUNICIPAL). Define, por item: a alíquota, se há retenção obrigatória na fonte e se há
/// substituição tributária. O <c>ApuradorIss</c> lê a tabela vigente na competência — nenhuma
/// alíquota é hardcoded. Ver M6-DESIGN §2.2.
/// </summary>
public sealed class TabelaAliquotaIss : AggregateRoot<TabelaAliquotaIssId>, IMustHaveTenant
{
    /// <summary>
    /// Sentinela de "item NAO classificado" na lista LC 116. IS-2 (fail-closed): este codigo nunca
    /// pode ser cadastrado na tabela, sob pena de uma nota de atividade desconhecida ser cobrada com
    /// alguma aliquota. O ISS so incide sobre atividade efetivamente classificada (CF art. 156 III).
    /// </summary>
    public static readonly IReadOnlySet<string> ItensNaoClassificaveis =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "00.00", "0000", "00", "0.00" };

    private readonly List<ItemAliquotaIss> _itens = [];

    private TabelaAliquotaIss()
    {
    }

    private TabelaAliquotaIss(TabelaAliquotaIssId id, Guid tenantId, int vigenciaInicioAaaaMm, string fundamentoLegal, string? municipioIbge)
        : base(id)
    {
        TenantId = tenantId;
        VigenciaInicioAaaaMm = vigenciaInicioAaaaMm;
        FundamentoLegal = fundamentoLegal;
        MunicipioIbge = municipioIbge;
        Vigente = false;
        RaiseDomainEvent(new TabelaAliquotaIssCriada(id, tenantId, vigenciaInicioAaaaMm));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Início de vigência no formato AAAAMM (competência a partir da qual a tabela vale).</summary>
    public int VigenciaInicioAaaaMm { get; private set; }

    /// <summary>
    /// Fundamento legal (CTM/lei municipal). // TODO(validar-oficial): preencher com o Código
    /// Tributário de Maximiliano de Almeida/RS.
    /// </summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>
    /// Código IBGE (7 dígitos) do município do tenant — o município competente para o ISS sob esta
    /// lei municipal (LC 116/2003 art. 3º). É o parâmetro de confronto com o
    /// <see cref="Nfse.NotaFiscalServico.MunicipioIncidenciaIbge"/> da nota: quando a nota declara
    /// incidência em OUTRO município (exceções art. 3º), a apuração recusa lançá-la como ISS próprio
    /// (fail-closed). Opcional/parametrizável por tenant: quando não informado, o confronto não é
    /// aplicado (comportamento legado preservado). // TODO(validar-oficial): preencher com o código
    /// IBGE de Maximiliano de Almeida/RS (4311981) na implantação do tenant.
    /// </summary>
    public string? MunicipioIbge { get; private set; }

    /// <summary>Indica se a tabela está vigente (publicada e imutável).</summary>
    public bool Vigente { get; private set; }

    /// <summary>Itens de alíquota por item da lista LC 116.</summary>
    public IReadOnlyCollection<ItemAliquotaIss> Itens => _itens;

    /// <summary>Cria uma tabela de alíquotas do ISS (ainda não vigente).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="vigenciaInicioAaaaMm">Início de vigência (AAAAMM).</param>
    /// <param name="fundamentoLegal">Lei municipal de alíquotas do ISS.</param>
    /// <param name="municipioIbge">Código IBGE (7 dígitos) do município do tenant (LC 116 art. 3º); opcional.</param>
    /// <returns>Nova <see cref="TabelaAliquotaIss"/>.</returns>
    public static TabelaAliquotaIss Criar(Guid tenantId, int vigenciaInicioAaaaMm, string fundamentoLegal, string? municipioIbge = null)
    {
        GarantirCompetenciaValida(vigenciaInicioAaaaMm);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        var ibge = string.IsNullOrWhiteSpace(municipioIbge) ? null : municipioIbge.Trim();
        return new TabelaAliquotaIss(TabelaAliquotaIssId.New(), tenantId, vigenciaInicioAaaaMm, fundamentoLegal.Trim(), ibge);
    }

    /// <summary>Acrescenta (ou rejeita duplicata de) um item de alíquota por item da lista LC 116.</summary>
    /// <param name="itemListaServico">Item da lista LC 116 (ex.: "7.02").</param>
    /// <param name="aliquotaPercentual">Alíquota em %.</param>
    /// <param name="retencaoObrigatoria">Retenção na fonte obrigatória por lei municipal.</param>
    /// <param name="substituicaoTributaria">Substituição tributária por lei municipal.</param>
    /// <exception cref="InvalidOperationException">Se a tabela já estiver vigente ou o item já existir.</exception>
    public void DefinirItem(string itemListaServico, decimal aliquotaPercentual, bool retencaoObrigatoria = false, bool substituicaoTributaria = false)
    {
        GarantirEditavel();
        ArgumentException.ThrowIfNullOrWhiteSpace(itemListaServico);
        var chave = itemListaServico.Trim();

        // IS-2 — FAIL-CLOSED: proibir o sentinela de "item nao classificado". Sem isto, uma nota de
        // atividade desconhecida casaria com este item e seria cobrada (fail-open). O ISS exige item
        // efetivamente classificado na lista LC 116.
        if (ItensNaoClassificaveis.Contains(chave))
        {
            throw new InvalidOperationException(
                $"O item '{chave}' representa atividade NAO classificada e nao pode ser cadastrado na " +
                "tabela de ISS (fail-closed): notas sem item LC 116 valido nao podem ser cobradas.");
        }

        if (_itens.Any(i => string.Equals(i.ItemListaServico, chave, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"O item '{chave}' já está definido nesta tabela de ISS.");
        }

        _itens.Add(ItemAliquotaIss.Criar(Id, chave, aliquotaPercentual, retencaoObrigatoria, substituicaoTributaria));
    }

    /// <summary>Publica a tabela (torna vigente e imutável).</summary>
    /// <exception cref="InvalidOperationException">Se não houver itens.</exception>
    public void Publicar()
    {
        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("A tabela de alíquotas do ISS exige ao menos um item antes de publicar.");
        }

        Vigente = true;
        RaiseDomainEvent(new TabelaAliquotaIssPublicada(Id, TenantId, VigenciaInicioAaaaMm));
    }

    /// <summary>Obtém o item de alíquota para um item da lista LC 116, ou <c>null</c> se não definido.</summary>
    /// <param name="itemListaServico">Item da lista LC 116.</param>
    /// <returns>O item de alíquota, ou <c>null</c>.</returns>
    public ItemAliquotaIss? ObterItem(string itemListaServico)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemListaServico);
        var chave = itemListaServico.Trim();
        return _itens.FirstOrDefault(i => string.Equals(i.ItemListaServico, chave, StringComparison.OrdinalIgnoreCase));
    }

    private void GarantirEditavel()
    {
        if (Vigente)
        {
            throw new InvalidOperationException("A tabela de ISS já está vigente e não pode ser alterada (crie nova versão por vigência).");
        }
    }

    private static void GarantirCompetenciaValida(int aaaaMm)
    {
        var ano = aaaaMm / 100;
        var mes = aaaaMm % 100;
        if (ano < 1900 || mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(aaaaMm), aaaaMm, "Vigência inválida; use o formato AAAAMM.");
        }
    }
}
