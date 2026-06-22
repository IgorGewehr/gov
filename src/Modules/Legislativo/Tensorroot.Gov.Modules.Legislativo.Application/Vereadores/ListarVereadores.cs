using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Vereadores;

/// <summary>Projecao de leitura de um vereador (cadastro).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="NomeCivil">Nome civil.</param>
/// <param name="NomeParlamentar">Nome parlamentar.</param>
/// <param name="Partido">Sigla partidaria.</param>
/// <param name="LegislaturaInicio">Ano de inicio da legislatura.</param>
/// <param name="LegislaturaFim">Ano de fim da legislatura.</param>
/// <param name="CargoMesa">Cargo na Mesa Diretora.</param>
/// <param name="Situacao">Situacao do mandato.</param>
public sealed record VereadorResumo(
    Guid Id,
    string NomeCivil,
    string NomeParlamentar,
    string Partido,
    int LegislaturaInicio,
    int LegislaturaFim,
    string CargoMesa,
    string Situacao);

/// <summary>Lista os vereadores do tenant (cadastro, ordenado por nome parlamentar).</summary>
public sealed record ListarVereadoresQuery : IQuery<IReadOnlyList<VereadorResumo>>;

/// <summary>Handler da listagem de vereadores.</summary>
public sealed class ListarVereadoresHandler(IVereadorRepository vereadores)
    : IQueryHandler<ListarVereadoresQuery, IReadOnlyList<VereadorResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VereadorResumo>> Handle(ListarVereadoresQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lista = await vereadores.ListarAsync(cancellationToken).ConfigureAwait(false);
        return lista.Select(Mapear).ToList();
    }

    internal static VereadorResumo Mapear(Vereador vereador) => new(
        vereador.Id.Value,
        vereador.NomeCivil,
        vereador.NomeParlamentar,
        vereador.Partido,
        vereador.LegislaturaInicio,
        vereador.LegislaturaFim,
        vereador.CargoMesa.ToString(),
        vereador.Situacao.ToString());
}
