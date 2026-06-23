using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Contracts;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Application.Merenda;

/// <summary>
/// Registra a distribuicao de merenda de um dia: calcula o consumo (per capita x comensais) a partir
/// do cardapio publicado e baixa os generos. A baixa efetiva do estoque ocorre em Patrimonio, acionada
/// pelo <see cref="MerendaDistribuidaIntegrationEvent"/> (cross-module via Contracts/Outbox).
/// </summary>
/// <param name="CardapioId">Cardapio publicado de origem.</param>
/// <param name="Data">Data efetiva da distribuicao.</param>
/// <param name="Dia">Dia da semana (para localizar os itens do cardapio).</param>
/// <param name="Refeicao">Tipo de refeicao servida.</param>
/// <param name="Comensais">Numero de comensais (&gt; 0).</param>
public sealed record RegistrarDistribuicaoMerendaCommand(
    Guid CardapioId,
    DateOnly Data,
    DiaSemanaCardapio Dia,
    TipoRefeicao Refeicao,
    int Comensais) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de distribuicao.</summary>
public sealed class RegistrarDistribuicaoMerendaValidator : AbstractValidator<RegistrarDistribuicaoMerendaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarDistribuicaoMerendaValidator()
    {
        RuleFor(comando => comando.CardapioId).NotEmpty().WithMessage("Cardapio obrigatorio.");
        RuleFor(comando => comando.Data).NotEmpty().WithMessage("Data obrigatoria.");
        RuleFor(comando => comando.Dia).IsInEnum().WithMessage("Dia da semana invalido.");
        RuleFor(comando => comando.Refeicao).IsInEnum().WithMessage("Tipo de refeicao invalido.");
        RuleFor(comando => comando.Comensais).GreaterThan(0).WithMessage("Comensais deve ser positivo.");
    }
}

/// <summary>Handler do registro de distribuicao de merenda.</summary>
public sealed class RegistrarDistribuicaoMerendaHandler(
    ICardapioRepository cardapios,
    IDistribuicaoMerendaRepository distribuicoes,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarDistribuicaoMerendaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarDistribuicaoMerendaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cardapio = await cardapios.ObterPorIdAsync(new CardapioId(request.CardapioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cardapio nao encontrado.");
        if (cardapio.Situacao != SituacaoCardapio.Publicado)
        {
            throw new InvalidOperationException("Somente cardapio Publicado admite distribuicao.");
        }

        // Consumo previsto = per capita x comensais, agregado por genero (do dia/refeicao do cardapio).
        var consumoPrevisto = cardapio.CalcularConsumo(request.Dia, request.Refeicao, request.Comensais);

        var distribuicao = DistribuicaoMerenda.Registrar(
            tenant.TenantId,
            cardapio.EscolaId,
            cardapio.Id,
            request.Data,
            request.Refeicao,
            request.Comensais,
            consumoPrevisto);

        distribuicoes.Adicionar(distribuicao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Integration Event: Patrimonio realiza a baixa efetiva do estoque (AtenderRequisicao por genero).
        var consumos = distribuicao.Consumos
            .Select(consumo => new ConsumoGeneroMerenda(consumo.GeneroEstoqueId, consumo.Quantidade, consumo.UnidadeMedida))
            .ToList();

        var evento = new MerendaDistribuidaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            distribuicao.Id.Value,
            distribuicao.EscolaId.Value,
            distribuicao.Data,
            consumos);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return distribuicao.Id.Value;
    }
}
