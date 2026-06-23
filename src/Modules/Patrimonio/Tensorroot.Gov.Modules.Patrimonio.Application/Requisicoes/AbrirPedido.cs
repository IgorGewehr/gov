using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Requisicoes;

/// <summary>Linha de um pedido de requisição na abertura.</summary>
/// <param name="ItemEstoqueId">Item de almoxarifado requisitado (reuso de <see cref="ItemEstoque"/> por Id).</param>
/// <param name="Quantidade">Quantidade solicitada (estritamente positiva).</param>
public sealed record ItemPedidoEntrada(Guid ItemEstoqueId, decimal Quantidade);

/// <summary>
/// Abre um pedido de requisição de almoxarifado self-service (multi-item, por setor/UO). Nasce em
/// situação Solicitado, pendente de aprovação (R-1). Reusa o catálogo de <see cref="ItemEstoque"/>
/// existente — não cria itens, apenas referencia por Id.
/// </summary>
/// <param name="UnidadeId">UO consumidora (escopo organizacional, M1).</param>
/// <param name="SetorSolicitante">Setor solicitante (rótulo do consumo por setor).</param>
/// <param name="SolicitanteId">Servidor solicitante.</param>
/// <param name="Data">Data do pedido.</param>
/// <param name="Justificativa">Justificativa opcional.</param>
/// <param name="Itens">Linhas do pedido (item de estoque, quantidade).</param>
public sealed record AbrirPedidoCommand(
    Guid UnidadeId,
    string SetorSolicitante,
    Guid SolicitanteId,
    DateOnly Data,
    string? Justificativa,
    IReadOnlyList<ItemPedidoEntrada> Itens) : ICommand<Guid>;

/// <summary>Regras de validação da abertura de pedido de requisição.</summary>
public sealed class AbrirPedidoValidator : AbstractValidator<AbrirPedidoCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirPedidoValidator()
    {
        RuleFor(comando => comando.UnidadeId).NotEmpty();
        RuleFor(comando => comando.SetorSolicitante).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.SolicitanteId).NotEmpty();
        RuleFor(comando => comando.Itens).NotEmpty();
        RuleForEach(comando => comando.Itens).ChildRules(item =>
        {
            item.RuleFor(linha => linha.ItemEstoqueId).NotEmpty();
            item.RuleFor(linha => linha.Quantidade).GreaterThan(0m);
        });
    }
}

/// <summary>Handler da abertura de pedido de requisição.</summary>
public sealed class AbrirPedidoHandler(
    IPedidoRequisicaoRepository pedidos,
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirPedidoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirPedidoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Valida que cada item referenciado existe no almoxarifado do tenant e está movimentável (R-1).
        foreach (var linha in request.Itens)
        {
            var item = await itens.ObterPorIdAsync(new ItemEstoqueId(linha.ItemEstoqueId), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Item de estoque {linha.ItemEstoqueId} não encontrado.");

            if (!item.Movimentavel)
            {
                throw new InvalidOperationException($"Item de estoque '{item.Codigo}' está inativo e não pode ser requisitado.");
            }
        }

        var pedido = PedidoRequisicao.Abrir(
            tenant.TenantId,
            request.UnidadeId,
            request.SetorSolicitante,
            request.SolicitanteId,
            request.Data,
            request.Justificativa,
            request.Itens.Select(linha => (new ItemEstoqueId(linha.ItemEstoqueId), linha.Quantidade)));

        pedidos.Adicionar(pedido);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return pedido.Id.Value;
    }
}
