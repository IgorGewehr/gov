using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

/// <summary>Identificador forte do agregado <see cref="RubricaConsignavel"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RubricaConsignavelId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RubricaConsignavelId"/>.</returns>
    public static RubricaConsignavelId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Parametrizacao por tenant de uma RUBRICA CONSIGNAVEL (analoga a <c>RegraAfastamento</c>): define a
/// <see cref="CategoriaConsignavel"/> (prioridade no corte por margem), em QUAL <see cref="GrupoMargem"/>
/// (balde de limite) a consignacao consome e se a propria rubrica <see cref="ContaParaMargem"/> (compoe a
/// base de calculo da margem — tipicamente vencimento + permanentes). O usuario escolhe a rubrica; o efeito
/// na folha e o consumo de margem vem desta parametrizacao (nunca <em>hardcoded</em> — CLAUDE.md S7/S16).
/// Raiz de agregado.
/// </summary>
public sealed class RubricaConsignavel : AggregateRoot<RubricaConsignavelId>, IMustHaveTenant
{
    private RubricaConsignavel()
    {
    }

    private RubricaConsignavel(
        RubricaConsignavelId id,
        Guid tenantId,
        string codigo,
        string descricao,
        CategoriaConsignavel categoria,
        GrupoMargem grupoMargem,
        bool contaParaMargem)
        : base(id)
    {
        TenantId = tenantId;
        Codigo = codigo;
        Descricao = descricao;
        Categoria = categoria;
        GrupoMargem = grupoMargem;
        ContaParaMargem = contaParaMargem;
        Ativa = true;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Codigo da rubrica consignavel (referencia ao catalogo S-1010 da folha), unico por tenant.</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Descricao legivel da rubrica consignavel.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Categoria (prioridade no corte por margem: obrigatoria &gt; facultativa &gt; beneficio).</summary>
    public CategoriaConsignavel Categoria { get; private set; }

    /// <summary>Balde de margem (reserva legal) que a consignacao desta rubrica consome.</summary>
    public GrupoMargem GrupoMargem { get; private set; }

    /// <summary>
    /// Se <c>true</c>, a verba desta rubrica (provento) COMPOE a base de calculo da margem consignavel
    /// (ex.: vencimento, gratificacoes permanentes); proventos eventuais nao contam.
    /// </summary>
    public bool ContaParaMargem { get; private set; }

    /// <summary>Indica se a rubrica consignavel esta ativa (false = nao aceita novas averbacoes).</summary>
    public bool Ativa { get; private set; }

    /// <summary>
    /// Define/cria uma rubrica consignavel parametrizada para o tenant.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Codigo da rubrica (referencia S-1010).</param>
    /// <param name="descricao">Descricao legivel.</param>
    /// <param name="categoria">Categoria (prioridade de manutencao).</param>
    /// <param name="grupoMargem">Balde de margem consumido.</param>
    /// <param name="contaParaMargem">Se a propria rubrica compoe a base da margem.</param>
    /// <returns>Nova <see cref="RubricaConsignavel"/> ativa.</returns>
    /// <exception cref="ArgumentException">Se codigo/descricao forem vazios.</exception>
    public static RubricaConsignavel Definir(
        Guid tenantId,
        string codigo,
        string descricao,
        CategoriaConsignavel categoria,
        GrupoMargem grupoMargem,
        bool contaParaMargem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        return new RubricaConsignavel(
            RubricaConsignavelId.New(),
            tenantId,
            codigo.Trim(),
            descricao.Trim(),
            categoria,
            grupoMargem,
            contaParaMargem);
    }

    /// <summary>Desativa a rubrica consignavel para novas averbacoes (mantem o historico).</summary>
    public void Desativar() => Ativa = false;
}
