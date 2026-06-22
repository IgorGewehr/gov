using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Receitas;

/// <summary>Identificador forte de <see cref="ReceitaArrecadada"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ReceitaArrecadadaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ReceitaArrecadadaId"/>.</returns>
    public static ReceitaArrecadadaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Receita arrecadada registrada em Finanças a partir de um evento de integração de
/// outro módulo (ex.: Tributos quita uma Dívida Ativa → receita reconhecida aqui).
/// </summary>
public sealed class ReceitaArrecadada : Entity<ReceitaArrecadadaId>, IMustHaveTenant
{
    private ReceitaArrecadada()
    {
    }

    private ReceitaArrecadada(ReceitaArrecadadaId id, Guid tenantId, Guid origemId, ValorMonetario valor, DateOnly data)
        : base(id)
    {
        TenantId = tenantId;
        OrigemId = origemId;
        Valor = valor;
        Data = data;
        RaiseDomainEvent(new ReceitaArrecadadaRegistrada(id, valor.Valor, data));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Identificador de origem da receita (no módulo de origem).</summary>
    public Guid OrigemId { get; private set; }

    /// <summary>Valor arrecadado.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Data da arrecadação.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra uma receita arrecadada.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="origemId">Identificador de origem.</param>
    /// <param name="valor">Valor.</param>
    /// <param name="data">Data.</param>
    /// <returns>Nova <see cref="ReceitaArrecadada"/>.</returns>
    public static ReceitaArrecadada Registrar(Guid tenantId, Guid origemId, ValorMonetario valor, DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new ReceitaArrecadada(ReceitaArrecadadaId.New(), tenantId, origemId, valor, data);
    }
}
