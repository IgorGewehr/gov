using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Redefine integralmente o conjunto de papeis (perfis RBAC) de um usuario.</summary>
/// <param name="UsuarioId">Usuario alvo.</param>
/// <param name="PapeisIds">Conjunto desejado de papeis (substitui o atual).</param>
public sealed record DefinirPapeisDoUsuarioCommand(Guid UsuarioId, IReadOnlyCollection<Guid> PapeisIds) : ICommand;

/// <summary>Regras de validacao da definicao de papeis do usuario.</summary>
public sealed class DefinirPapeisDoUsuarioValidator : AbstractValidator<DefinirPapeisDoUsuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirPapeisDoUsuarioValidator()
    {
        RuleFor(comando => comando.UsuarioId).NotEmpty();
        RuleFor(comando => comando.PapeisIds).NotNull();
    }
}

/// <summary>Handler da definicao de papeis do usuario.</summary>
public sealed class DefinirPapeisDoUsuarioHandler(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirPapeisDoUsuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirPapeisDoUsuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        var papeisIds = request.PapeisIds.Select(id => new PapelId(id)).ToArray();
        if (papeisIds.Length > 0)
        {
            var encontrados = await papeis.ObterPorIdsAsync(papeisIds, cancellationToken).ConfigureAwait(false);
            if (encontrados.Count != papeisIds.Distinct().Count())
            {
                throw new InvalidOperationException("Um ou mais papeis informados nao existem no tenant.");
            }
        }

        usuario.DefinirPapeis(papeisIds);

        // Reancora na UO raiz real do tenant (a ponte de compat cria na sentinela RaizPendente).
        if (papeisIds.Length > 0)
        {
            var unidadesDoTenant = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
            var raiz = unidadesDoTenant.FirstOrDefault(unidade => unidade.UnidadePaiId is null);
            if (raiz is not null)
            {
                usuario.ReancorarAtribuicoesPendentesNaRaiz(raiz.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
