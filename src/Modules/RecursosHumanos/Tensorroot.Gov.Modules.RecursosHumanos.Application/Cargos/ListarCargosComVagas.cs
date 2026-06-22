using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>Resumo de um cargo com vagas disponiveis para leitura.</summary>
/// <param name="Id">Identificador do cargo.</param>
/// <param name="Denominacao">Denominacao legal.</param>
/// <param name="Tipo">Tipo do cargo.</param>
/// <param name="VagasDisponiveis">Vagas ainda disponiveis para provimento.</param>
public sealed record CargoResumo(Guid Id, string Denominacao, string Tipo, int VagasDisponiveis);

/// <summary>Lista os cargos com vagas disponiveis, opcionalmente por tipo.</summary>
/// <param name="Tipo">Filtro opcional por tipo de cargo.</param>
public sealed record ListarCargosComVagasQuery(TipoCargo? Tipo) : IQuery<IReadOnlyList<CargoResumo>>;

/// <summary>Handler da consulta de cargos com vagas.</summary>
public sealed class ListarCargosComVagasHandler(ICargoRepository cargos)
    : IQueryHandler<ListarCargosComVagasQuery, IReadOnlyList<CargoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CargoResumo>> Handle(
        ListarCargosComVagasQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await cargos
            .ListarComVagasDisponiveisAsync(request.Tipo, cancellationToken)
            .ConfigureAwait(false);

        return encontrados
            .Select(cargo => new CargoResumo(
                cargo.Id.Value,
                cargo.Denominacao,
                cargo.Tipo.ToString(),
                cargo.VagasDisponiveis))
            .ToList();
    }
}
