using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>Sobresta um processo (suspende temporariamente o andamento).</summary>
/// <param name="ProcessoId">Processo a sobrestar.</param>
/// <param name="Motivo">Motivo do sobrestamento.</param>
public sealed record SobrestarProcessoCommand(Guid ProcessoId, string Motivo) : ICommand;

/// <summary>Regras de validacao do sobrestamento de processo.</summary>
public sealed class SobrestarProcessoValidator : AbstractValidator<SobrestarProcessoCommand>
{
    /// <summary>Define as regras.</summary>
    public SobrestarProcessoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler do sobrestamento de processo.</summary>
public sealed class SobrestarProcessoHandler(
    IProcessoRepository processos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SobrestarProcessoCommand>
{
    /// <inheritdoc />
    public async Task Handle(SobrestarProcessoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processo = await processos.ObterPorIdAsync(new ProcessoId(request.ProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo nao encontrado.");

        processo.Sobrestar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
