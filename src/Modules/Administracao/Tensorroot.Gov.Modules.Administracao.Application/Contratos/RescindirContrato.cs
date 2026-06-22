using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Rescinde o contrato (extincao antecipada com motivacao) — I-15.</summary>
/// <param name="ContratoId">Contrato a rescindir.</param>
/// <param name="Motivo">Motivacao do ato administrativo de rescisao.</param>
public sealed record RescindirContratoCommand(Guid ContratoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da rescisao.</summary>
public sealed class RescindirContratoValidator : AbstractValidator<RescindirContratoCommand>
{
    /// <summary>Define as regras.</summary>
    public RescindirContratoValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty();
    }
}

/// <summary>Handler da rescisao do contrato.</summary>
public sealed class RescindirContratoHandler(IContratoRepository contratos, IUnitOfWork unitOfWork)
    : ICommandHandler<RescindirContratoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RescindirContratoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        contrato.Rescindir(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
