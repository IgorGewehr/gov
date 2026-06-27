using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>
/// Contabiliza o recolhimento da consignação (dispêndio extra-orçamentário): D Consignação a Recolher
/// (2.1.8.8.1.xx) / C Caixa (1.1.1) pelo valor da guia — baixando o passivo nascido na retenção.
/// Patrimonial, balanceado, idempotente por <c>GuiaRecolhimentoId</c>.
/// </summary>
public sealed class ContabilizarRecolhimentoHandler(
    IContaContabilRepository contas,
    ILancamentoContabilRepository lancamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : INotificationHandler<RecolhimentoEfetuado>
{
    /// <inheritdoc />
    public async Task Handle(RecolhimentoEfetuado notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var origem = notification.GuiaRecolhimentoId.Value;
        if (await lancamentos.ExisteParaOrigemAsync(origem, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var valor = ValorMonetario.De(notification.Valor);
        if (!valor.EhPositivo())
        {
            return;
        }

        var consignacao = await ResolverContaAsync(MapaContaConsignacao.CodigoConta(notification.Natureza), cancellationToken).ConfigureAwait(false);
        var caixa = await ResolverContaAsync(MapaContaConsignacao.CodigoCaixa, cancellationToken).ConfigureAwait(false);

        var linhas = new[]
        {
            LinhaDe(consignacao, LadoPartida.Debito, valor),
            LinhaDe(caixa, LadoPartida.Credito, valor),
        };

        var lancamento = LancamentoContabil.Registrar(
            tenant.TenantId,
            notification.Data,
            notification.Data.Year,
            $"Recolhimento de {notification.Natureza} (baixa da consignacao)",
            OrigemLancamento.EventoAutomatico,
            origem,
            eventoContabilId: null,
            linhas,
            periodoAberto: true);

        lancamentos.Adicionar(lancamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ContaContabil> ResolverContaAsync(string codigo, CancellationToken cancellationToken) =>
        await contas.ObterPorCodigoAsync(codigo, cancellationToken).ConfigureAwait(false)
            ?? throw new RoteiroContabilInvalidoException($"Conta {codigo} ausente no plano (rodar seed do plano de contas).");

    private static LinhaLancamento LinhaDe(ContaContabil conta, LadoPartida lado, ValorMonetario valor) =>
        new(conta.Id, conta.Codigo, conta.NaturezaInformacao, conta.Tipo, lado, valor);
}
