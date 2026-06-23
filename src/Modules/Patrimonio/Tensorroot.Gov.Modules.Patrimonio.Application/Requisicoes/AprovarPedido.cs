using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Requisicoes;

/// <summary>
/// Aprova um pedido de requisição (Solicitado → Aprovado), habilitando o atendimento (R-2). A autorização
/// (RBAC/escopo do setor) é garantida pela permissão do endpoint; a transição é invariante do agregado.
/// </summary>
/// <param name="PedidoRequisicaoId">Pedido a aprovar.</param>
/// <param name="AprovadorId">Autoridade que aprova.</param>
/// <param name="Data">Data da aprovação.</param>
public sealed record AprovarPedidoCommand(Guid PedidoRequisicaoId, Guid AprovadorId, DateOnly Data) : ICommand;

/// <summary>Regras de validação da aprovação de pedido.</summary>
public sealed class AprovarPedidoValidator : AbstractValidator<AprovarPedidoCommand>
{
    /// <summary>Define as regras.</summary>
    public AprovarPedidoValidator()
    {
        RuleFor(comando => comando.PedidoRequisicaoId).NotEmpty();
        RuleFor(comando => comando.AprovadorId).NotEmpty();
    }
}

/// <summary>Handler da aprovação de pedido de requisição.</summary>
public sealed class AprovarPedidoHandler(
    IPedidoRequisicaoRepository pedidos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AprovarPedidoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AprovarPedidoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pedido = await pedidos.ObterPorIdAsync(new PedidoRequisicaoId(request.PedidoRequisicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pedido de requisição não encontrado.");

        pedido.Aprovar(request.AprovadorId, request.Data);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
