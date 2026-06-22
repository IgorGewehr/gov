using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Cancela uma solicitacao de regulacao (antes de executada); se autorizada, devolve a cota e libera o SISREG.</summary>
/// <param name="SolicitacaoRegulacaoId">Solicitacao a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record CancelarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao do cancelamento de solicitacao de regulacao.</summary>
public sealed class CancelarSolicitacaoRegulacaoValidator : AbstractValidator<CancelarSolicitacaoRegulacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarSolicitacaoRegulacaoValidator()
    {
        RuleFor(comando => comando.SolicitacaoRegulacaoId).NotEmpty().WithMessage("Solicitacao e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(1000).WithMessage("Motivo do cancelamento e obrigatorio (max. 1000).");
    }
}

/// <summary>Handler do cancelamento de solicitacao de regulacao.</summary>
public sealed class CancelarSolicitacaoRegulacaoHandler(
    ISolicitacaoRegulacaoRepository solicitacoes,
    ISisregGateway sisreg,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarSolicitacaoRegulacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarSolicitacaoRegulacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var solicitacao = await solicitacoes
            .ObterPorIdAsync(new SolicitacaoRegulacaoId(request.SolicitacaoRegulacaoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Solicitacao nao encontrada.");

        // I-7: ao cancelar uma autorizada, libera a reserva no SISREG (idempotente) antes de devolver a cota.
        if (solicitacao.Situacao == SituacaoSolicitacaoRegulacao.Autorizada && solicitacao.ProtocoloSisreg is { } protocolo)
        {
            await sisreg
                .LiberarReservaAsync(solicitacao.Id, protocolo, cancellationToken)
                .ConfigureAwait(false);
        }

        solicitacao.Cancelar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
