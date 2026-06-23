using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Requisicoes;

/// <summary>
/// Atende um pedido de requisição APROVADO (R-3/R-4): itera as linhas, calcula por linha a quantidade a
/// atender (mínimo entre o pendente e o saldo disponível do item — atendimento parcial permitido) e baixa
/// o estoque via <see cref="ItemEstoque.AtenderRequisicao(RequisicaoId, System.Guid, decimal, System.DateOnly)"/>
/// (saída + valoração + despesa no consumo, já implementado), na MESMA transação. Conclui o pedido em Atendido
/// e publica o evento de reposição (<see cref="PontoPedidoAtingidoIntegrationEvent"/>) para cada item que
/// atingir o ponto de pedido (I-6, herdado do estoque).
/// </summary>
/// <param name="PedidoRequisicaoId">Pedido a atender.</param>
/// <param name="Data">Data do atendimento.</param>
public sealed record AtenderPedidoCommand(Guid PedidoRequisicaoId, DateOnly Data) : ICommand;

/// <summary>Regras de validação do atendimento de pedido.</summary>
public sealed class AtenderPedidoValidator : AbstractValidator<AtenderPedidoCommand>
{
    /// <summary>Define as regras.</summary>
    public AtenderPedidoValidator() => RuleFor(comando => comando.PedidoRequisicaoId).NotEmpty();
}

/// <summary>Handler do atendimento de pedido de requisição (orquestra a baixa de estoque por item).</summary>
public sealed class AtenderPedidoHandler(
    IPedidoRequisicaoRepository pedidos,
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AtenderPedidoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtenderPedidoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pedido = await pedidos.ObterPorIdAsync(new PedidoRequisicaoId(request.PedidoRequisicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pedido de requisição não encontrado.");

        if (pedido.Situacao != SituacaoPedido.Aprovado)
        {
            throw new InvalidOperationException(
                $"O atendimento exige um pedido Aprovado. Situação atual: {pedido.Situacao}.");
        }

        // Itens que atingiram o ponto de pedido após a baixa — reposição publicada após o commit (I-6).
        var aRepor = new List<(Guid ItemEstoqueId, decimal Saldo)>();

        // R-3/R-4: itera as linhas; por item, atende o mínimo entre o pendente e o saldo disponível (parcial).
        foreach (var linha in pedido.Itens)
        {
            if (linha.TotalmenteAtendido)
            {
                continue;
            }

            var item = await itens.ObterPorIdAsync(linha.ItemEstoqueId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Item de estoque {linha.ItemEstoqueId} não encontrado.");

            if (!item.Movimentavel)
            {
                // Item inativado após a abertura: pula (não atende), demais linhas seguem.
                continue;
            }

            var aAtender = Math.Min(linha.Pendente, item.Saldo.Quantidade);
            if (aAtender <= 0m)
            {
                // Sem saldo para esta linha: atendimento parcial (esta linha fica pendente).
                continue;
            }

            // Reuso do motor de estoque: baixa de saldo + valoração + despesa no consumo (R-3).
            item.AtenderRequisicao(RequisicaoId.New(), pedido.SolicitanteId, aAtender, request.Data);
            pedido.RegistrarAtendimentoDeLinha(linha.Id, aAtender);

            if (item.PontoPedido.FoiAtingidoPor(item.Saldo))
            {
                aRepor.Add((item.Id.Value, item.Saldo.Quantidade));
            }
        }

        // R-4: conclui o pedido (Aprovado → Atendido); falha se nada pôde ser atendido (saldo zero em tudo).
        pedido.ConcluirAtendimento(request.Data);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // I-6: reposição na Administração para cada item que atingiu o ponto de pedido (após o commit).
        foreach (var (itemEstoqueId, saldo) in aRepor)
        {
            var evento = new PontoPedidoAtingidoIntegrationEvent(
                Guid.NewGuid(),
                timeProvider.GetUtcNow().UtcDateTime,
                tenant.TenantId,
                itemEstoqueId,
                saldo);

            await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
        }
    }
}
