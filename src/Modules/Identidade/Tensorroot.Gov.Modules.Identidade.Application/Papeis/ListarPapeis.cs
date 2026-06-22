using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Identidade.Application.Papeis;

/// <summary>Detalhe de um papel (perfil RBAC) com suas permissoes.</summary>
/// <param name="Id">Identificador do papel.</param>
/// <param name="Nome">Nome do papel.</param>
/// <param name="Permissoes">Permissoes concedidas.</param>
public sealed record PapelDetalhe(Guid Id, string Nome, IReadOnlyCollection<string> Permissoes);

/// <summary>Lista os papeis do tenant atual com suas permissoes.</summary>
public sealed record ListarPapeisQuery : IQuery<IReadOnlyList<PapelDetalhe>>;

/// <summary>Handler da listagem de papeis.</summary>
public sealed class ListarPapeisHandler(IPapelRepository papeis)
    : IQueryHandler<ListarPapeisQuery, IReadOnlyList<PapelDetalhe>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PapelDetalhe>> Handle(ListarPapeisQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lista = await papeis.ListarAsync(cancellationToken).ConfigureAwait(false);

        return lista
            .Select(papel => new PapelDetalhe(
                papel.Id.Value,
                papel.Nome,
                papel.Permissoes.ToArray()))
            .ToList();
    }
}
