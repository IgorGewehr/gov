using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Lanca a producao do atendimento no SISAB (CDS/e-SUS APS) na competencia.</summary>
/// <param name="AtendimentoId">Atendimento cuja producao sera lancada.</param>
public sealed record LancarAtendimentoNoSISABCommand(Guid AtendimentoId) : ICommand;

/// <summary>Regras de validacao do lancamento no SISAB.</summary>
public sealed class LancarAtendimentoNoSISABValidator : AbstractValidator<LancarAtendimentoNoSISABCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarAtendimentoNoSISABValidator()
    {
        RuleFor(comando => comando.AtendimentoId).NotEmpty().WithMessage("Atendimento e obrigatorio.");
    }
}

/// <summary>Handler do lancamento no SISAB.</summary>
public sealed class LancarAtendimentoNoSISABHandler(
    IAtendimentoRepository atendimentos,
    ISisabGateway sisabGateway,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LancarAtendimentoNoSISABCommand>
{
    /// <inheritdoc />
    public async Task Handle(LancarAtendimentoNoSISABCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atendimento = await atendimentos
            .ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Atendimento nao encontrado.");

        // I-6: envia a producao da competencia ao SISAB (idempotente por (AtendimentoId, Competencia)).
        await sisabGateway
            .EnviarProducaoAsync(atendimento.Id, atendimento.Competencia, cancellationToken)
            .ConfigureAwait(false);

        atendimento.LancarNoSISAB(atendimento.Competencia);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
