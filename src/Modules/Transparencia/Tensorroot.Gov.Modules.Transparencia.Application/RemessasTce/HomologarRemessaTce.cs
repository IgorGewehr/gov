using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>Registra a homologação do TCE para uma remessa <c>Enviada</c> (passa a <c>Homologada</c>).</summary>
/// <param name="RemessaTceId">Identificador da remessa a homologar.</param>
public sealed record HomologarRemessaTceCommand(Guid RemessaTceId) : ICommand;

/// <summary>Regras de validação do comando de homologação de remessa.</summary>
public sealed class HomologarRemessaTceValidator : AbstractValidator<HomologarRemessaTceCommand>
{
    /// <summary>Define as regras.</summary>
    public HomologarRemessaTceValidator() => RuleFor(comando => comando.RemessaTceId).NotEmpty();
}

/// <summary>Handler da homologação da remessa pelo TCE-RS.</summary>
public sealed class HomologarRemessaTceHandler(
    IRemessaTceRepository remessas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<HomologarRemessaTceCommand>
{
    /// <inheritdoc />
    public async Task Handle(HomologarRemessaTceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas.ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa não encontrada.");

        remessa.Homologar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
