using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

namespace Tensorroot.Gov.Modules.Financas.Application.Integracoes;

/// <summary>
/// O VÍNCULO planejamento→execução (DESIGN §6): consome o domain event
/// <see cref="ItemLoaEntrouEmExecucao"/> e faz a <see cref="DotacaoOrcamentaria"/> NASCER da LOA
/// (dotação inicial = despesa fixada), com origem rastreável. Idempotente por item: se a dotação
/// já foi gerada, não recria. A contabilidade (roteiro EVT-DOT) é disparada pelo evento da dotação.
/// </summary>
public sealed class GerarDotacaoDeItemLoaHandler(
    ILoaRepository loas,
    IDotacaoOrcamentariaRepository dotacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : INotificationHandler<ItemLoaEntrouEmExecucao>
{
    /// <inheritdoc />
    public async Task Handle(ItemLoaEntrouEmExecucao notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var loa = await loas.ObterPorIdAsync(notification.LoaId, cancellationToken).ConfigureAwait(false);
        if (loa is null)
        {
            return;
        }

        // Idempotencia: item ja vinculado a uma dotacao nao recria (re-execucao do evento e segura).
        var item = loa.Itens.FirstOrDefault(i => i.Id == notification.ItemId);
        if (item is null || item.DotacaoGerada)
        {
            return;
        }

        var dotacao = DotacaoOrcamentaria.CriarDeLoa(
            tenant.TenantId,
            notification.Exercicio,
            notification.Classificacao,
            notification.ValorFixado,
            notification.LoaId.Value,
            notification.ItemId.Value,
            notification.AcaoPpaId.Value);

        dotacoes.Adicionar(dotacao);
        loa.RegistrarDotacaoGerada(notification.ItemId, dotacao.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
