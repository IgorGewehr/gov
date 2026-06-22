using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Application.Unidades;

/// <summary>Ativa uma UO (idempotente). Nunca deleta — preserva o historico.</summary>
/// <param name="UnidadeId">UO alvo.</param>
public sealed record AtivarUnidadeCommand(Guid UnidadeId) : ICommand;

/// <summary>Desativa uma UO (idempotente), preservando o historico de auditoria.</summary>
/// <param name="UnidadeId">UO alvo.</param>
public sealed record DesativarUnidadeCommand(Guid UnidadeId) : ICommand;

/// <summary>Validacao da ativacao de UO.</summary>
public sealed class AtivarUnidadeValidator : AbstractValidator<AtivarUnidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public AtivarUnidadeValidator() => RuleFor(comando => comando.UnidadeId).NotEmpty();
}

/// <summary>Validacao da desativacao de UO.</summary>
public sealed class DesativarUnidadeValidator : AbstractValidator<DesativarUnidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public DesativarUnidadeValidator() => RuleFor(comando => comando.UnidadeId).NotEmpty();
}

/// <summary>Handler da ativacao de UO.</summary>
public sealed class AtivarUnidadeHandler(IUnidadeRepository unidades, IUnitOfWork unitOfWork)
    : ICommandHandler<AtivarUnidadeCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtivarUnidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var unidade = await unidades.ObterPorIdAsync(new UnidadeOrganizacionalId(request.UnidadeId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade organizacional nao encontrada.");

        unidade.Ativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da desativacao de UO.</summary>
public sealed class DesativarUnidadeHandler(IUnidadeRepository unidades, IUnitOfWork unitOfWork)
    : ICommandHandler<DesativarUnidadeCommand>
{
    /// <inheritdoc />
    public async Task Handle(DesativarUnidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var unidade = await unidades.ObterPorIdAsync(new UnidadeOrganizacionalId(request.UnidadeId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade organizacional nao encontrada.");

        unidade.Desativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
