using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Integracoes;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Integracoes;

/// <summary>
/// Gateway/read model CadUnico simulado (dev/demonstracao): projeta uma folha resumo
/// deterministica por NIS, permitindo exercitar o referenciamento e a projecao do resumo sem o
/// ambiente real do MDS. A base federal nunca e sobrescrita (I-9) — somente leitura.
/// </summary>
public sealed class SimuladoCadUnicoGateway : ICadUnicoGateway, ICadUnicoReadModel
{
    /// <inheritdoc />
    public Task<ResultadoCadUnico> ConsultarPorNisAsync(string nis, CancellationToken cancellationToken)
        => Task.FromResult(Projetar(nis));

    /// <inheritdoc />
    public Task<ResultadoCadUnico> ProjetarResumoAsync(string nis, CancellationToken cancellationToken)
        => Task.FromResult(Projetar(nis));

    private static ResultadoCadUnico Projetar(string nis)
    {
        if (!Nis.IsValid(nis))
        {
            return new ResultadoCadUnico(false, 0m, 0, default);
        }

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        return new ResultadoCadUnico(
            NisLocalizado: true,
            RendaFamiliarDeclarada: 1200.00m,
            QuantidadeMembros: 3,
            DataUltimaAtualizacao: hoje.AddMonths(-6));
    }
}
