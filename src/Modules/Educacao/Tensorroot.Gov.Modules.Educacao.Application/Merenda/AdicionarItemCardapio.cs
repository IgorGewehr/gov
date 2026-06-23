using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Application.Merenda;

/// <summary>
/// Adiciona um genero (item) ao cardapio em um dia/refeicao, com a quantidade per capita. O genero e
/// um item do almoxarifado de Patrimonio referenciado por Id (FK logica cross-module).
/// </summary>
/// <param name="CardapioId">Cardapio alvo.</param>
/// <param name="Dia">Dia da semana.</param>
/// <param name="Refeicao">Tipo de refeicao.</param>
/// <param name="GeneroEstoqueId">Genero (ItemEstoque) por Id.</param>
/// <param name="QuantidadePerCapita">Per capita (&gt; 0).</param>
/// <param name="UnidadeMedida">Unidade de medida do genero.</param>
public sealed record AdicionarItemCardapioCommand(
    Guid CardapioId,
    DiaSemanaCardapio Dia,
    TipoRefeicao Refeicao,
    Guid GeneroEstoqueId,
    decimal QuantidadePerCapita,
    string UnidadeMedida) : ICommand<Guid>;

/// <summary>Regras de validacao da adicao de item ao cardapio.</summary>
public sealed class AdicionarItemCardapioValidator : AbstractValidator<AdicionarItemCardapioCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarItemCardapioValidator()
    {
        RuleFor(comando => comando.CardapioId).NotEmpty().WithMessage("Cardapio obrigatorio.");
        RuleFor(comando => comando.Dia).IsInEnum().WithMessage("Dia da semana invalido.");
        RuleFor(comando => comando.Refeicao).IsInEnum().WithMessage("Tipo de refeicao invalido.");
        RuleFor(comando => comando.GeneroEstoqueId).NotEmpty().WithMessage("Genero obrigatorio.");
        RuleFor(comando => comando.QuantidadePerCapita).GreaterThan(0m).WithMessage("Per capita deve ser positivo.");
        RuleFor(comando => comando.UnidadeMedida).NotEmpty().WithMessage("Unidade de medida obrigatoria.");
    }
}

/// <summary>Handler da adicao de item ao cardapio (somente enquanto Planejado — I-M1).</summary>
public sealed class AdicionarItemCardapioHandler(
    ICardapioRepository cardapios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarItemCardapioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarItemCardapioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cardapio = await cardapios.ObterPorIdAsync(new CardapioId(request.CardapioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cardapio nao encontrado.");

        var itemId = cardapio.AdicionarItem(
            request.Dia, request.Refeicao, request.GeneroEstoqueId, request.QuantidadePerCapita, request.UnidadeMedida);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return itemId.Value;
    }
}
