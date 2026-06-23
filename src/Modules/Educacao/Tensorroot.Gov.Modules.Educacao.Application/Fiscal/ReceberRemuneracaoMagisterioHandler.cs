using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

namespace Tensorroot.Gov.Modules.Educacao.Application.Fiscal;

/// <summary>
/// <b>E-2 — Anti-Corruption Layer (entrada — RecursosHumanos).</b> Consome o
/// <see cref="RemuneracaoMagisterioApuradaIntegrationEvent"/> publicado pelo RH e MATERIALIZA o total da
/// remuneração dos profissionais da educação básica no read model
/// (<see cref="IRemuneracaoMagisterioReadModel"/>), de onde o <c>ApurarFundeb70Handler</c> lê o numerador
/// dos 70%. É o <b>cruzamento com a folha</b> exigido pelo breakdown (E-2): a remuneração do magistério
/// chega via Contracts — como a MSC (Finanças→Transparência) e a remessa de folha (RH→Transparência) — sem
/// que a Educação acesse o interno do RH. Idempotente por exercício (reprocessar atualiza o mesmo total).
/// <para>
/// Enquanto o RH não publicar o evento, o município informa o total como parâmetro
/// (<c>RegistrarRemuneracaoMagisterioCommand</c>), que chama a mesma porta — o domínio não muda.
/// </para>
/// </summary>
public sealed class ReceberRemuneracaoMagisterioHandler(
    IRemuneracaoMagisterioReadModel remuneracao,
    IUnitOfWork unitOfWork,
    ILogger<ReceberRemuneracaoMagisterioHandler> logger)
    : INotificationHandler<RemuneracaoMagisterioApuradaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(RemuneracaoMagisterioApuradaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        logger.LogInformation(
            "Remuneracao do magisterio recebida do RH para tenant {TenantId}, exercicio {Exercicio}: {Valor}.",
            notification.TenantId,
            notification.Exercicio,
            notification.RemuneracaoProfissionaisEducacao);

        await remuneracao
            .DefinirRemuneracaoProfissionaisAsync(notification.Exercicio, notification.RemuneracaoProfissionaisEducacao, cancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
