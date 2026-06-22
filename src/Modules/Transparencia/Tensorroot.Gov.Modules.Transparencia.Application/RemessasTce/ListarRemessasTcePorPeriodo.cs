using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>Resumo de uma remessa ao TCE-RS para listagem.</summary>
/// <param name="Id">Identificador da remessa.</param>
/// <param name="Periodo">Período (competência/exercício).</param>
/// <param name="LeiauteVersao">Versão do leiaute.</param>
/// <param name="Situacao">Situação atual.</param>
/// <param name="DataLimite">Prazo legal/parametrizado de envio.</param>
/// <param name="DataEnvio">Data de transmissão, se enviada.</param>
public sealed record RemessaTceResumo(
    Guid Id,
    string Periodo,
    string LeiauteVersao,
    string Situacao,
    DateOnly DataLimite,
    DateOnly? DataEnvio);

/// <summary>Lista as remessas do tenant por exercício, com filtros opcionais de tipo e situação (tenant-scoped).</summary>
/// <param name="Exercicio">Ano de exercício.</param>
/// <param name="Tipo">Tipo de período (opcional).</param>
/// <param name="Situacao">Situação (opcional).</param>
public sealed record ListarRemessasTcePorPeriodoQuery(
    int Exercicio,
    TipoPeriodo? Tipo,
    SituacaoRemessaTce? Situacao) : IQuery<IReadOnlyList<RemessaTceResumo>>;

/// <summary>Handler da listagem de remessas por período.</summary>
public sealed class ListarRemessasTcePorPeriodoHandler(IRemessaTceRepository remessas)
    : IQueryHandler<ListarRemessasTcePorPeriodoQuery, IReadOnlyList<RemessaTceResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemessaTceResumo>> Handle(
        ListarRemessasTcePorPeriodoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontradas = await remessas
            .ListarPorPeriodoAsync(request.Exercicio, request.Tipo, request.Situacao, cancellationToken)
            .ConfigureAwait(false);

        return encontradas
            .Select(remessa => new RemessaTceResumo(
                remessa.Id.Value,
                remessa.Periodo.ToString(),
                remessa.Leiaute.Versao,
                remessa.Situacao.ToString(),
                remessa.DataLimite,
                remessa.DataEnvio))
            .ToList();
    }
}
