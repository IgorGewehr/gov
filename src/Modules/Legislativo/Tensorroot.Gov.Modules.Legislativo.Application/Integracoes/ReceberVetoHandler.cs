using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Executivo): consome o evento de integracao
/// <see cref="VetoIntegrationEvent"/> publicado pelo modulo do Executivo quando o autografo e vetado.
/// Atualiza a trilha de <c>Tramitacao</c> da proposicao apenas se ela estiver em
/// <see cref="SituacaoProposicao.AutografoEnviado"/> (I-13), habilitando a apreciacao de derrubada de
/// veto por maioria absoluta; caso contrario, o evento e ignorado/auditado (B-12). Idempotente por
/// <c>EventId</c> (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberVetoHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ReceberVetoHandler> logger)
    : INotificationHandler<VetoIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(VetoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var proposicao = await proposicoes
            .ObterPorIdAsync(new ProposicaoId(notification.ProposicaoId), cancellationToken)
            .ConfigureAwait(false);

        if (proposicao is null)
        {
            logger.LogWarning(
                "Veto recebido do Executivo para proposicao inexistente {ProposicaoId} no tenant {TenantId}.",
                notification.ProposicaoId,
                notification.TenantId);
            return;
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (!proposicao.RegistrarVeto(hoje))
        {
            logger.LogInformation(
                "Veto ignorado (proposicao {ProposicaoId} fora de AutografoEnviado; situacao {Situacao}).",
                notification.ProposicaoId,
                proposicao.Situacao);
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
