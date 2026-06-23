using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Requisicoes;

/// <summary>
/// Cancela um pedido de requisição (Solicitado/Aprovado → Cancelado), terminal sem efeito de estoque (R-5).
/// Pedido já atendido não pode ser cancelado (a saída de estoque é irreversível por aqui).
/// </summary>
/// <param name="PedidoRequisicaoId">Pedido a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record CancelarPedidoCommand(Guid PedidoRequisicaoId, string Motivo) : ICommand;

/// <summary>Regras de validação do cancelamento de pedido.</summary>
public sealed class CancelarPedidoValidator : AbstractValidator<CancelarPedidoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarPedidoValidator()
    {
        RuleFor(comando => comando.PedidoRequisicaoId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler do cancelamento de pedido de requisição.</summary>
public sealed class CancelarPedidoHandler(
    IPedidoRequisicaoRepository pedidos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarPedidoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarPedidoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pedido = await pedidos.ObterPorIdAsync(new PedidoRequisicaoId(request.PedidoRequisicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pedido de requisição não encontrado.");

        pedido.Cancelar(request.Motivo);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
