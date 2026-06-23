using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Efetua o pagamento do liquido (so a partir de Fechada — I-9).</summary>
/// <param name="FolhaDePagamentoId">Folha a pagar.</param>
/// <param name="DataPagamento">Data da liquidacao financeira.</param>
public sealed record EfetuarPagamentoCommand(Guid FolhaDePagamentoId, DateOnly DataPagamento) : ICommand;

/// <summary>Regras de validacao do pagamento de folha.</summary>
public sealed class EfetuarPagamentoValidator : AbstractValidator<EfetuarPagamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public EfetuarPagamentoValidator()
    {
        RuleFor(comando => comando.FolhaDePagamentoId)
            .NotEmpty()
            .WithMessage("Folha e obrigatoria.");
        RuleFor(comando => comando.DataPagamento)
            .NotEmpty()
            .WithMessage("Data de pagamento e obrigatoria.");
    }
}

/// <summary>
/// Handler do pagamento de folha. Publica o evento de integracao
/// <see cref="PagamentoEfetuadoIntegrationEvent"/> (Tesouraria — liquidacao financeira — I-9).
/// </summary>
public sealed class EfetuarPagamentoHandler(
    IFolhaDePagamentoRepository folhas,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    TimeProvider timeProvider)
    : ICommandHandler<EfetuarPagamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EfetuarPagamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        // I-9: o agregado garante que so paga a partir de Fechada.
        folha.EfetuarPagamento(request.DataPagamento);

        // Enfileirado no Outbox na MESMA transação — despacho isolado por módulo ao drenar evita resolver
        // o contexto de um módulo consumidor (ex.: Painel do Gestor) no escopo da requisição do RH (guarda H5).
        var evento = new PagamentoEfetuadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            folha.TenantId,
            folha.Id.Value,
            folha.Competencia.ToString(),
            folha.TotalLiquido.Valor,
            request.DataPagamento);
        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
