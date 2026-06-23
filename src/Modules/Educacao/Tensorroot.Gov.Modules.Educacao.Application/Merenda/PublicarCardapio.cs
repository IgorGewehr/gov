using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Application.Merenda;

/// <summary>Publica o cardapio (Planejado -&gt; Publicado), habilitando as distribuicoes da semana.</summary>
/// <param name="CardapioId">Cardapio a publicar.</param>
public sealed record PublicarCardapioCommand(Guid CardapioId) : ICommand;

/// <summary>Handler da publicacao de cardapio.</summary>
public sealed class PublicarCardapioHandler(
    ICardapioRepository cardapios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<PublicarCardapioCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarCardapioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cardapio = await cardapios.ObterPorIdAsync(new CardapioId(request.CardapioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cardapio nao encontrado.");

        cardapio.Publicar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
