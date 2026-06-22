using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Educacao.Application.Escolas;

/// <summary>Lista as escolas da rede (escopo do tenant atual), projetadas em <see cref="EscolaResumo"/>.</summary>
public sealed record ListarEscolasDaRedeQuery : IQuery<IReadOnlyList<EscolaResumo>>;

/// <summary>Handler da listagem de escolas da rede.</summary>
public sealed class ListarEscolasDaRedeHandler(IEscolaRepository escolas)
    : IQueryHandler<ListarEscolasDaRedeQuery, IReadOnlyList<EscolaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EscolaResumo>> Handle(ListarEscolasDaRedeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lista = await escolas.ListarAsync(cancellationToken).ConfigureAwait(false);

        return lista
            .Select(escola => new EscolaResumo(
                escola.Id.Value,
                escola.CodigoInep.Valor,
                escola.Nome,
                escola.DependenciaAdministrativa.ToString(),
                escola.Situacao.ToString()))
            .ToList();
    }
}
