using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Cancela o atendimento antes da assinatura.</summary>
/// <param name="AtendimentoId">Atendimento a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record CancelarAtendimentoCommand(Guid AtendimentoId, string Motivo) : ICommand;

/// <summary>Regras de validacao do cancelamento de atendimento.</summary>
public sealed class CancelarAtendimentoValidator : AbstractValidator<CancelarAtendimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarAtendimentoValidator()
    {
        RuleFor(comando => comando.AtendimentoId).NotEmpty().WithMessage("Atendimento e obrigatorio.");
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500).WithMessage("Motivo e obrigatorio (max. 500).");
    }
}

/// <summary>Handler do cancelamento de atendimento.</summary>
public sealed class CancelarAtendimentoHandler(
    IAtendimentoRepository atendimentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarAtendimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarAtendimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atendimento = await atendimentos
            .ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Atendimento nao encontrado.");

        atendimento.Cancelar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
