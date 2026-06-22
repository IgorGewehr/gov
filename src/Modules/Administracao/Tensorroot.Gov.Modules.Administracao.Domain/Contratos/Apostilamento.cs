using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>Identificador forte de um <see cref="Apostilamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ApostilamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ApostilamentoId"/>.</returns>
    public static ApostilamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Apostilamento — registro de alteracao que dispensa termo aditivo (reajuste, dotacao, correcao;
/// Lei 14.133/2021, art. 136). Entidade-filha do agregado <see cref="Contrato"/>.
/// </summary>
public sealed class Apostilamento : Entity<ApostilamentoId>
{
    private Apostilamento()
    {
    }

    private Apostilamento(ApostilamentoId id, int numero, TipoApostilamento tipo, string descricao, DateOnly dataRegistro)
        : base(id)
    {
        Numero = numero;
        Tipo = tipo;
        Descricao = descricao;
        DataRegistro = dataRegistro;
    }

    /// <summary>Numero sequencial do apostilamento no contrato.</summary>
    public int Numero { get; private set; }

    /// <summary>Tipo do apostilamento.</summary>
    public TipoApostilamento Tipo { get; private set; }

    /// <summary>Descricao da alteracao apostilada.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Data do registro do apostilamento.</summary>
    public DateOnly DataRegistro { get; private set; }

    /// <summary>Registra um apostilamento.</summary>
    /// <param name="numero">Numero sequencial.</param>
    /// <param name="tipo">Tipo do apostilamento.</param>
    /// <param name="descricao">Descricao da alteracao (obrigatoria).</param>
    /// <param name="dataRegistro">Data do registro.</param>
    /// <returns>Novo <see cref="Apostilamento"/>.</returns>
    /// <exception cref="ArgumentException">Se a descricao for vazia.</exception>
    internal static Apostilamento Registrar(int numero, TipoApostilamento tipo, string descricao, DateOnly dataRegistro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        return new Apostilamento(ApostilamentoId.New(), numero, tipo, descricao, dataRegistro);
    }
}
