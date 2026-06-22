using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

/// <summary>Identificador forte de uma <see cref="MovimentacaoPatrimonial"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MovimentacaoPatrimonialId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MovimentacaoPatrimonialId"/>.</returns>
    public static MovimentacaoPatrimonialId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Transferência interna de localização/responsável de um bem patrimonial,
/// sem baixa contábil (movimentação dentro do tenant).
/// </summary>
public sealed class MovimentacaoPatrimonial : Entity<MovimentacaoPatrimonialId>
{
    private MovimentacaoPatrimonial()
    {
    }

    private MovimentacaoPatrimonial(
        MovimentacaoPatrimonialId id,
        string localizacaoOrigem,
        string localizacaoDestino,
        Guid responsavelId,
        DateOnly data)
        : base(id)
    {
        LocalizacaoOrigem = localizacaoOrigem;
        LocalizacaoDestino = localizacaoDestino;
        ResponsavelId = responsavelId;
        Data = data;
    }

    /// <summary>Localização de origem.</summary>
    public string LocalizacaoOrigem { get; private set; } = default!;

    /// <summary>Localização de destino.</summary>
    public string LocalizacaoDestino { get; private set; } = default!;

    /// <summary>Responsável de destino pelo bem.</summary>
    public Guid ResponsavelId { get; private set; }

    /// <summary>Data da movimentação.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra uma nova movimentação patrimonial.</summary>
    /// <param name="localizacaoOrigem">Localização de origem.</param>
    /// <param name="localizacaoDestino">Localização de destino.</param>
    /// <param name="responsavelId">Responsável de destino.</param>
    /// <param name="data">Data da movimentação.</param>
    /// <returns>Nova <see cref="MovimentacaoPatrimonial"/>.</returns>
    public static MovimentacaoPatrimonial Registrar(
        string localizacaoOrigem,
        string localizacaoDestino,
        Guid responsavelId,
        DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(localizacaoDestino);
        return new MovimentacaoPatrimonial(
            MovimentacaoPatrimonialId.New(),
            localizacaoOrigem ?? string.Empty,
            localizacaoDestino,
            responsavelId,
            data);
    }
}
