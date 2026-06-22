using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Transmite a declaracao consolidada ao SICONFI (secao 5.2 das regras).</summary>
/// <param name="DeclaracaoFiscalId">Declaracao a transmitir.</param>
public sealed record TransmitirDeclaracaoFiscalCommand(Guid DeclaracaoFiscalId) : ICommand;

/// <summary>Regras de validacao da transmissao da declaracao fiscal.</summary>
public sealed class TransmitirDeclaracaoFiscalValidator : AbstractValidator<TransmitirDeclaracaoFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public TransmitirDeclaracaoFiscalValidator()
    {
        RuleFor(comando => comando.DeclaracaoFiscalId).NotEmpty();
    }
}

/// <summary>Handler da transmissao da declaracao fiscal ao SICONFI.</summary>
public sealed class TransmitirDeclaracaoFiscalHandler(
    IDeclaracaoFiscalRepository declaracoes,
    ISiconfiGateway siconfiGateway,
    IUnitOfWork unitOfWork,
    IPublisher publisher)
    : ICommandHandler<TransmitirDeclaracaoFiscalCommand>
{
    /// <inheritdoc />
    public async Task Handle(TransmitirDeclaracaoFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var declaracao = await declaracoes
            .ObterPorIdAsync(new DeclaracaoFiscalId(request.DeclaracaoFiscalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Declaracao fiscal nao encontrada.");

        // ACL + Polly: transmissao idempotente por DeclaracaoFiscalId.
        var resultado = await siconfiGateway.TransmitirAsync(declaracao, cancellationToken).ConfigureAwait(false);

        declaracao.TransmitirSiconfi(resultado.DataTransmissao, resultado.Protocolo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // I-6/secao 7.2: somente MSC publica o evento de integracao (via Outbox).
        if (declaracao.TipoDeclaracao == TipoDeclaracaoFiscal.Msc)
        {
            var evento = new MscEnviadaSiconfiIntegrationEvent(
                Guid.NewGuid(),
                resultado.DataTransmissao.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                declaracao.TenantId,
                declaracao.Id.Value,
                declaracao.Competencia?.ToString() ?? string.Empty,
                resultado.Protocolo,
                resultado.DataTransmissao);

            await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
        }
    }
}
