using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

namespace Tensorroot.Gov.Modules.Legislativo.Application.LimiteCamara;

/// <summary>
/// Consolida (fecha) o demonstrativo do art. 29-A de uma apuracao: congela os numeros para a prestacao
/// ao TCE-RS (transmissao real = M10) e emite o evento de consolidacao com os semaforos. Retorna o
/// demonstrativo consolidado.
/// </summary>
/// <param name="ApuracaoId">Identificador da apuracao a consolidar.</param>
public sealed record ConsolidarApuracaoArt29ACommand(Guid ApuracaoId) : ICommand<DemonstrativoArt29ADto>;

/// <summary>Handler da consolidacao da apuracao do art. 29-A.</summary>
public sealed class ConsolidarApuracaoArt29AHandler(
    IApuracaoArt29ARepository apuracoes,
    IUnitOfWork unitOfWork) : ICommandHandler<ConsolidarApuracaoArt29ACommand, DemonstrativoArt29ADto>
{
    /// <inheritdoc />
    public async Task<DemonstrativoArt29ADto> Handle(ConsolidarApuracaoArt29ACommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apuracao = await apuracoes.ObterPorIdAsync(new ApuracaoArt29AId(request.ApuracaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Apuracao do art. 29-A nao encontrada.");

        apuracao.Consolidar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ObterDemonstrativoArt29AHandler.Montar(apuracao);
    }
}
