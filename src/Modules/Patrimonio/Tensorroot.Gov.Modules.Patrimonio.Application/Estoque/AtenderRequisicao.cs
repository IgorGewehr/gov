using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>
/// Atende uma requisição de material: valora pelo custeio (I-3/I-8), gera saída, reduz o saldo,
/// reconhece a despesa no consumo (I-4/I-7) e, ao atingir o ponto de pedido, publica o evento de
/// integração de reposição à Administração (I-6).
/// </summary>
/// <param name="ItemEstoqueId">Item de estoque.</param>
/// <param name="RequisicaoId">Identificador da requisição.</param>
/// <param name="SolicitanteId">Solicitante.</param>
/// <param name="Quantidade">Quantidade requisitada (estritamente positiva).</param>
/// <param name="Data">Data do atendimento.</param>
public sealed record AtenderRequisicaoCommand(
    Guid ItemEstoqueId,
    Guid RequisicaoId,
    Guid SolicitanteId,
    decimal Quantidade,
    DateOnly Data) : ICommand;

/// <summary>Regras de validação do atendimento de requisição.</summary>
public sealed class AtenderRequisicaoValidator : AbstractValidator<AtenderRequisicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AtenderRequisicaoValidator()
    {
        RuleFor(comando => comando.ItemEstoqueId).NotEmpty();
        RuleFor(comando => comando.RequisicaoId).NotEmpty();
        RuleFor(comando => comando.SolicitanteId).NotEmpty();
        RuleFor(comando => comando.Quantidade).GreaterThan(0m);
    }
}

/// <summary>Handler do atendimento de requisição (publica Integration Event de reposição quando aplicável).</summary>
public sealed class AtenderRequisicaoHandler(
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AtenderRequisicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtenderRequisicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await itens.ObterPorIdAsync(new ItemEstoqueId(request.ItemEstoqueId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de estoque não encontrado.");

        item.AtenderRequisicao(
            new RequisicaoId(request.RequisicaoId),
            request.SolicitanteId,
            request.Quantidade,
            request.Data);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // I-6: ao atingir o ponto de pedido após a baixa, dispara a reposição na Administração.
        if (item.PontoPedido.FoiAtingidoPor(item.Saldo))
        {
            var evento = new PontoPedidoAtingidoIntegrationEvent(
                Guid.NewGuid(),
                timeProvider.GetUtcNow().UtcDateTime,
                tenant.TenantId,
                request.ItemEstoqueId,
                item.Saldo.Quantidade);

            await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
        }
    }
}
