using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

/// <summary>Identificador forte do agregado <see cref="ParametrosMargemVigente"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ParametrosMargemVigenteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ParametrosMargemVigenteId"/>.</returns>
    public static ParametrosMargemVigenteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Percentuais de margem consignavel CADASTRADOS pelo tenant, versionados por vigencia (mesma filosofia de
/// <c>RegraAfastamento</c>/<c>TabelaInss</c>): a Lei 14.131/2021 fixa 35%+5%+5%, mas o ente pode ter lei
/// municipal propria e mudancas ao longo do tempo. O provider seleciona a vigencia mais recente &lt;=
/// competencia; na ausencia de qualquer cadastro, cai para os defaults legais parametrizados. Raiz de agregado.
/// </summary>
public sealed class ParametrosMargemVigente : AggregateRoot<ParametrosMargemVigenteId>, IMustHaveTenant
{
    private ParametrosMargemVigente()
    {
    }

    private ParametrosMargemVigente(
        ParametrosMargemVigenteId id,
        Guid tenantId,
        Competencia vigenciaInicio,
        decimal percentualGeral,
        decimal percentualCartaoConsignado,
        decimal percentualCartaoBeneficio)
        : base(id)
    {
        TenantId = tenantId;
        VigenciaInicio = vigenciaInicio;
        PercentualGeral = percentualGeral;
        PercentualCartaoConsignado = percentualCartaoConsignado;
        PercentualCartaoBeneficio = percentualCartaoBeneficio;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Competencia a partir da qual os percentuais vigoram (versionamento por vigencia).</summary>
    public Competencia VigenciaInicio { get; private set; } = default!;

    /// <summary>Fracao (0..1) da margem GERAL.</summary>
    public decimal PercentualGeral { get; private set; }

    /// <summary>Fracao (0..1) da reserva de CARTAO DE CREDITO consignado.</summary>
    public decimal PercentualCartaoConsignado { get; private set; }

    /// <summary>Fracao (0..1) da reserva de CARTAO BENEFICIO.</summary>
    public decimal PercentualCartaoBeneficio { get; private set; }

    /// <summary>Define os percentuais de margem vigentes a partir de uma competencia para o tenant.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="vigenciaInicio">Competencia inicial de vigencia.</param>
    /// <param name="percentualGeral">Fracao (0..1) da margem geral.</param>
    /// <param name="percentualCartaoConsignado">Fracao (0..1) do cartao consignado.</param>
    /// <param name="percentualCartaoBeneficio">Fracao (0..1) do cartao beneficio.</param>
    /// <returns>Novo <see cref="ParametrosMargemVigente"/> valido.</returns>
    /// <exception cref="ArgumentNullException">Se a competencia for nula.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se algum percentual estiver fora de 0..1.</exception>
    public static ParametrosMargemVigente Definir(
        Guid tenantId,
        Competencia vigenciaInicio,
        decimal percentualGeral,
        decimal percentualCartaoConsignado,
        decimal percentualCartaoBeneficio)
    {
        ArgumentNullException.ThrowIfNull(vigenciaInicio);
        GarantirFracao(percentualGeral, nameof(percentualGeral));
        GarantirFracao(percentualCartaoConsignado, nameof(percentualCartaoConsignado));
        GarantirFracao(percentualCartaoBeneficio, nameof(percentualCartaoBeneficio));

        return new ParametrosMargemVigente(
            ParametrosMargemVigenteId.New(),
            tenantId,
            vigenciaInicio,
            percentualGeral,
            percentualCartaoConsignado,
            percentualCartaoBeneficio);
    }

    private static void GarantirFracao(decimal valor, string nome)
    {
        if (valor is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nome, "Percentual de margem deve ser uma fracao entre 0 e 1.");
        }
    }
}
