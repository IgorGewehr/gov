using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte de uma <see cref="Multa"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MultaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MultaId"/>.</returns>
    public static MultaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Infração de trânsito (CTB, Lei 9.503/1997) atribuída a um veículo da frota
/// e, quando identificado, a um condutor (motorista).
/// </summary>
public sealed class Multa : Entity<MultaId>
{
    private Multa()
    {
    }

    private Multa(
        MultaId id,
        string codigoInfracaoCtb,
        ValorMonetario valor,
        DateOnly dataInfracao,
        Guid? motoristaId)
        : base(id)
    {
        CodigoInfracaoCtb = codigoInfracaoCtb;
        Valor = valor;
        DataInfracao = dataInfracao;
        MotoristaId = motoristaId;
        Situacao = SituacaoMulta.Pendente;
    }

    /// <summary>Código de infração do CTB.</summary>
    public string CodigoInfracaoCtb { get; private set; } = default!;

    /// <summary>Valor da multa.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Data da infração.</summary>
    public DateOnly DataInfracao { get; private set; }

    /// <summary>Condutor (motorista) responsável, quando identificado.</summary>
    public Guid? MotoristaId { get; private set; }

    /// <summary>Situação atual da multa.</summary>
    public SituacaoMulta Situacao { get; private set; }

    /// <summary>Registra uma nova multa em situação <see cref="SituacaoMulta.Pendente"/> (I-8).</summary>
    /// <param name="codigoInfracaoCtb">Código de infração do CTB.</param>
    /// <param name="valor">Valor da multa.</param>
    /// <param name="dataInfracao">Data da infração.</param>
    /// <param name="motoristaId">Condutor responsável, quando informado.</param>
    /// <returns>Nova <see cref="Multa"/>.</returns>
    /// <exception cref="ArgumentException">Se o código de infração for vazio.</exception>
    internal static Multa Registrar(
        string codigoInfracaoCtb,
        ValorMonetario valor,
        DateOnly dataInfracao,
        Guid? motoristaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoInfracaoCtb);
        ArgumentNullException.ThrowIfNull(valor);
        return new Multa(MultaId.New(), codigoInfracaoCtb, valor, dataInfracao, motoristaId);
    }
}
