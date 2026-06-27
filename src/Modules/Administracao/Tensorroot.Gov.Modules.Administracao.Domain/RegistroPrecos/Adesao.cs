using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Identificador forte de uma <see cref="Adesao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AdesaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AdesaoId"/>.</returns>
    public static AdesaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Adesao (carona) a uma Ata de Registro de Precos por orgao nao participante (art. 86, §§ 2º a 5º, Lei
/// 14.133/2021; Dec. 11.462/2023), registrando o item da ata, o orgao aderente (identificado por CNPJ
/// para apurar o teto de 50% por aderente) e a quantidade aderida. Entidade filha da <see cref="Ata"/>.
/// </summary>
public sealed class Adesao : Entity<AdesaoId>
{
    private Adesao()
    {
    }

    private Adesao(
        AdesaoId id,
        ItemAtaId itemAtaId,
        ItemCatalogoId itemCatalogoId,
        string cnpjOrgaoAderente,
        string orgaoAderente,
        decimal quantidade,
        DateOnly data)
        : base(id)
    {
        ItemAtaId = itemAtaId;
        ItemCatalogoId = itemCatalogoId;
        CnpjOrgaoAderente = cnpjOrgaoAderente;
        OrgaoAderente = orgaoAderente;
        Quantidade = quantidade;
        Data = data;
    }

    /// <summary>Item da ata objeto da adesao (resolve o limite especifico do item).</summary>
    public ItemAtaId ItemAtaId { get; private set; }

    /// <summary>Item de catalogo objeto da adesao (rastreio/transmissao).</summary>
    public ItemCatalogoId ItemCatalogoId { get; private set; }

    /// <summary>CNPJ do orgao/entidade aderente — chave de apuracao do teto de 50% por aderente (art. 86, §4º).</summary>
    public string CnpjOrgaoAderente { get; private set; } = default!;

    /// <summary>Identificacao (nome) do orgao/entidade aderente (carona).</summary>
    public string OrgaoAderente { get; private set; } = default!;

    /// <summary>Quantidade aderida.</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Data da adesao.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra uma adesao (carona).</summary>
    /// <param name="itemAtaId">Item da ata objeto da adesao.</param>
    /// <param name="itemCatalogoId">Item de catalogo objeto da adesao.</param>
    /// <param name="cnpjOrgaoAderente">CNPJ do orgao aderente (obrigatorio; identifica o aderente para o teto por orgao).</param>
    /// <param name="orgaoAderente">Nome do orgao aderente (obrigatorio).</param>
    /// <param name="quantidade">Quantidade aderida (positiva).</param>
    /// <param name="data">Data da adesao.</param>
    /// <returns>Nova <see cref="Adesao"/>.</returns>
    /// <exception cref="ArgumentException">Se o CNPJ ou o nome do orgao aderente forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    public static Adesao Registrar(
        ItemAtaId itemAtaId,
        ItemCatalogoId itemCatalogoId,
        string cnpjOrgaoAderente,
        string orgaoAderente,
        decimal quantidade,
        DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjOrgaoAderente);
        ArgumentException.ThrowIfNullOrWhiteSpace(orgaoAderente);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        return new Adesao(
            AdesaoId.New(),
            itemAtaId,
            itemCatalogoId,
            cnpjOrgaoAderente.Trim(),
            orgaoAderente.Trim(),
            quantidade,
            data);
    }
}
