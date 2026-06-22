using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;

namespace Tensorroot.Gov.Modules.Identidade.Application.Papeis;

/// <summary>Redefine integralmente o conjunto de permissoes de um papel.</summary>
/// <param name="PapelId">Papel alvo.</param>
/// <param name="Permissoes">Conjunto desejado de permissoes (do catalogo canonico).</param>
public sealed record DefinirPermissoesDoPapelCommand(Guid PapelId, IReadOnlyCollection<string> Permissoes) : ICommand;

/// <summary>Regras de validacao da definicao de permissoes do papel.</summary>
public sealed class DefinirPermissoesDoPapelValidator : AbstractValidator<DefinirPermissoesDoPapelCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirPermissoesDoPapelValidator()
    {
        RuleFor(comando => comando.PapelId).NotEmpty();
        RuleFor(comando => comando.Permissoes).NotNull();
    }
}

/// <summary>Handler da definicao de permissoes do papel.</summary>
public sealed class DefinirPermissoesDoPapelHandler(IPapelRepository papeis, IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirPermissoesDoPapelCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirPermissoesDoPapelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var papel = await papeis.ObterPorIdAsync(new PapelId(request.PapelId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Papel nao encontrado.");

        // O dominio rejeita escopos fora do catalogo canonico (negar por padrao).
        papel.DefinirPermissoes(request.Permissoes);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
