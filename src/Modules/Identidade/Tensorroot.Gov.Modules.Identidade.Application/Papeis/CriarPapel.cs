using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;

namespace Tensorroot.Gov.Modules.Identidade.Application.Papeis;

/// <summary>Cria um papel (perfil RBAC) no tenant atual.</summary>
/// <param name="Nome">Nome do papel (unico por tenant).</param>
/// <param name="Permissoes">Permissoes iniciais (do catalogo canonico).</param>
public sealed record CriarPapelCommand(string Nome, IReadOnlyCollection<string>? Permissoes = null) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de papel.</summary>
public sealed class CriarPapelValidator : AbstractValidator<CriarPapelCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarPapelValidator()
        => RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(Papel.ComprimentoMaximoNome);
}

/// <summary>Handler da criacao de papel.</summary>
public sealed class CriarPapelHandler(
    IPapelRepository papeis,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CriarPapelCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarPapelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await papeis.NomeEmUsoAsync(request.Nome, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe um papel com este nome no tenant.");
        }

        // A validacao de permissoes conhecidas e garantida pelo dominio (Papel.DefinirPermissoes).
        var papel = Papel.Criar(tenant.TenantId, request.Nome, request.Permissoes);

        papeis.Adicionar(papel);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return papel.Id.Value;
    }
}
