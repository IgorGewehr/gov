using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;

/// <summary>Identificador forte da entidade <see cref="ImovelBeneficiado"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ImovelBeneficiadoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ImovelBeneficiadoId"/>.</returns>
    public static ImovelBeneficiadoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Imóvel beneficiado por uma obra de Contribuição de Melhoria: registra a VALORIZAÇÃO individual
/// apurada (acréscimo de valor decorrente da obra — CTN art. 81) e a parcela rateada da contribuição.
/// O limite individual é a própria valorização; o rateio é proporcional a ela. Entidade filha do
/// agregado <see cref="ObraContribuicaoMelhoria"/>. Ver M6-DESIGN §3.5.
/// </summary>
public sealed class ImovelBeneficiado : Entity<ImovelBeneficiadoId>
{
    private ImovelBeneficiado()
    {
    }

    private ImovelBeneficiado(
        ImovelBeneficiadoId id,
        ObraContribuicaoMelhoriaId obraId,
        ImovelId imovelId,
        ContribuinteId proprietarioId,
        ValorMonetario valorizacaoIndividual)
        : base(id)
    {
        ObraId = obraId;
        ImovelId = imovelId;
        ProprietarioId = proprietarioId;
        ValorizacaoIndividual = valorizacaoIndividual;
        ContribuicaoRateada = ValorMonetario.Zero;
    }

    /// <summary>Obra dona do registro.</summary>
    public ObraContribuicaoMelhoriaId ObraId { get; private set; }

    /// <summary>Imóvel beneficiado.</summary>
    public ImovelId ImovelId { get; private set; }

    /// <summary>Contribuinte proprietário (sujeito passivo da contribuição).</summary>
    public ContribuinteId ProprietarioId { get; private set; }

    /// <summary>
    /// Valorização individual apurada (acréscimo de valor do imóvel decorrente da obra) — limite
    /// individual da contribuição (CTN art. 81).
    /// </summary>
    public ValorMonetario ValorizacaoIndividual { get; private set; } = default!;

    /// <summary>Parcela da contribuição rateada para este imóvel (definida no rateio da obra).</summary>
    public ValorMonetario ContribuicaoRateada { get; private set; } = default!;

    /// <summary>Cria um imóvel beneficiado da obra.</summary>
    /// <param name="obraId">Obra dona.</param>
    /// <param name="imovelId">Imóvel beneficiado.</param>
    /// <param name="proprietarioId">Contribuinte proprietário.</param>
    /// <param name="valorizacaoIndividual">Valorização individual apurada.</param>
    /// <returns>Novo <see cref="ImovelBeneficiado"/>.</returns>
    public static ImovelBeneficiado Criar(
        ObraContribuicaoMelhoriaId obraId,
        ImovelId imovelId,
        ContribuinteId proprietarioId,
        ValorMonetario valorizacaoIndividual)
    {
        ArgumentNullException.ThrowIfNull(valorizacaoIndividual);
        if (proprietarioId.Value == Guid.Empty)
        {
            throw new ArgumentException("O imóvel beneficiado deve ter um proprietário.", nameof(proprietarioId));
        }

        return new ImovelBeneficiado(ImovelBeneficiadoId.New(), obraId, imovelId, proprietarioId, valorizacaoIndividual);
    }

    /// <summary>Define a parcela rateada (chamado internamente pela obra durante o rateio).</summary>
    /// <param name="contribuicaoRateada">Parcela rateada.</param>
    internal void DefinirContribuicaoRateada(ValorMonetario contribuicaoRateada)
    {
        ArgumentNullException.ThrowIfNull(contribuicaoRateada);
        ContribuicaoRateada = contribuicaoRateada;
    }
}
