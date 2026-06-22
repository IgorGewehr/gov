using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Resumo de uma presenca (trilha imutavel) para transparencia (LAI).</summary>
/// <param name="VereadorId">Vereador presente.</param>
/// <param name="RegistradaEm">Momento do registro.</param>
public sealed record PresencaResumo(Guid VereadorId, DateTimeOffset RegistradaEm);

/// <summary>Obtem a trilha imutavel de presencas de uma sessao (tenant-scoped; LAI).</summary>
/// <param name="SessaoId">Sessao a consultar.</param>
public sealed record ObterPresencasDaSessaoQuery(Guid SessaoId) : IQuery<IReadOnlyList<PresencaResumo>>;

/// <summary>Handler da consulta de presencas da sessao.</summary>
public sealed class ObterPresencasDaSessaoHandler(ISessaoRepository sessoes)
    : IQueryHandler<ObterPresencasDaSessaoQuery, IReadOnlyList<PresencaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PresencaResumo>> Handle(
        ObterPresencasDaSessaoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        return sessao.Presencas
            .Select(presenca => new PresencaResumo(presenca.VereadorId.Value, presenca.RegistradaEm))
            .ToList();
    }
}
