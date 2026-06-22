using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>Inscreve um lançamento vencido em Dívida Ativa, gerando o título.</summary>
/// <param name="LancamentoId">Lançamento de origem.</param>
public sealed record InscreverEmDividaAtivaCommand(Guid LancamentoId) : ICommand<Guid>;

/// <summary>Handler da inscrição em Dívida Ativa.</summary>
public sealed class InscreverEmDividaAtivaHandler(
    ILancamentoRepository lancamentos,
    IDividaAtivaRepository dividas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<InscreverEmDividaAtivaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(InscreverEmDividaAtivaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lancamento = await lancamentos.ObterPorIdAsync(new LancamentoId(request.LancamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Lançamento não encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Regra de domínio: só inscreve se em aberto e vencido.
        lancamento.InscreverEmDividaAtiva(hoje);

        var divida = DividaAtiva.Inscrever(
            tenant.TenantId,
            lancamento.ContribuinteId,
            lancamento.Id,
            lancamento.ValorPrincipal,
            hoje);

        dividas.Adicionar(divida);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return divida.Id.Value;
    }
}
