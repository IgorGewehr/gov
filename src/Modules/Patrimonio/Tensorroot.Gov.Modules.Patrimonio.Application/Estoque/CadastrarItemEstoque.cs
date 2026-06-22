using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Cadastra um item de consumo no almoxarifado (nasce ativo, saldo zero, I-10).</summary>
/// <param name="Codigo">Código do item no catálogo.</param>
/// <param name="Descricao">Descrição do item.</param>
/// <param name="UnidadeMedida">Unidade de medida (un, kg, cx, L).</param>
/// <param name="MetodoCusteio">Método de custeio (PEPS/médio).</param>
/// <param name="PontoPedido">Ponto de pedido (saldo-gatilho de reposição).</param>
/// <param name="ClassificacaoAbc">Classe ABC.</param>
public sealed record CadastrarItemEstoqueCommand(
    string Codigo,
    string Descricao,
    string UnidadeMedida,
    int MetodoCusteio,
    decimal PontoPedido,
    int ClassificacaoAbc) : ICommand<Guid>;

/// <summary>Regras de validação do cadastro de item de estoque.</summary>
public sealed class CadastrarItemEstoqueValidator : AbstractValidator<CadastrarItemEstoqueCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarItemEstoqueValidator()
    {
        RuleFor(comando => comando.Codigo)
            .NotEmpty()
            .MaximumLength(40);

        RuleFor(comando => comando.Descricao)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(comando => comando.UnidadeMedida)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(comando => (MetodoCusteio)comando.MetodoCusteio).IsInEnum();
        RuleFor(comando => comando.PontoPedido).GreaterThanOrEqualTo(0m);
        RuleFor(comando => (CurvaABC)comando.ClassificacaoAbc).IsInEnum();
    }
}

/// <summary>Handler do cadastro de item de estoque.</summary>
public sealed class CadastrarItemEstoqueHandler(
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarItemEstoqueCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarItemEstoqueCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await itens.CodigoExisteAsync(request.Codigo, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Já existe um item com o código '{request.Codigo}' neste tenant.");
        }

        var item = ItemEstoque.Cadastrar(
            tenant.TenantId,
            request.Codigo,
            request.Descricao,
            request.UnidadeMedida,
            (MetodoCusteio)request.MetodoCusteio,
            PontoPedido.De(request.PontoPedido),
            (CurvaABC)request.ClassificacaoAbc);

        itens.Adicionar(item);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item.Id.Value;
    }
}
