using System.Security.Cryptography;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>
/// Contabiliza, no momento do pagamento, a parcela retida (consignação) das liquidações quitadas pela
/// ordem. O roteiro de pagamento (EVT-PAG) já lançou D Fornecedores / C Caixa pelo bruto; este handler
/// posta o ajuste complementar — D Caixa / C Consignação a Recolher (2.1.8.8.1.xx) — pela soma retida,
/// de modo que o líquido efetivamente sai do Caixa e o retido permanece como passivo extra-orçamentário
/// a recolher (consignação — Lei 4.320/64). Patrimonial, balanceado, idempotente por origem derivada da
/// ordem (distinta da origem do EVT-PAG). Agrupa um lançamento por natureza de retenção.
/// </summary>
public sealed class ContabilizarRetencaoHandler(
    IOrdemDePagamentoRepository ordens,
    ILiquidacaoRepository liquidacoes,
    IContaContabilRepository contas,
    ILancamentoContabilRepository lancamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : INotificationHandler<PagamentoEfetuado>
{
    /// <inheritdoc />
    public async Task Handle(PagamentoEfetuado notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var ordem = await ordens.ObterPorIdAsync(notification.OrdemDePagamentoId, cancellationToken).ConfigureAwait(false);
        if (ordem is null)
        {
            return;
        }

        // Soma o retido por natureza, percorrendo as liquidacoes quitadas pela ordem.
        var retidoPorNatureza = new Dictionary<NaturezaRetencao, decimal>();
        foreach (var item in ordem.Itens)
        {
            var liquidacao = await liquidacoes.ObterPorIdAsync(item.LiquidacaoId, cancellationToken).ConfigureAwait(false);
            if (liquidacao is null)
            {
                continue;
            }

            foreach (var retencao in liquidacao.Retencoes)
            {
                retidoPorNatureza.TryGetValue(retencao.Natureza, out var atual);
                retidoPorNatureza[retencao.Natureza] = atual + retencao.Valor.Valor;
            }
        }

        if (retidoPorNatureza.Count == 0)
        {
            return;
        }

        var caixa = await ResolverContaAsync(MapaContaConsignacao.CodigoCaixa, cancellationToken).ConfigureAwait(false);

        var gerou = false;
        foreach (var (natureza, total) in retidoPorNatureza)
        {
            var valor = ValorMonetario.De(total);
            if (!valor.EhPositivo())
            {
                continue;
            }

            // Origem deterministica por (ordem, natureza): idempotente e disjunta do EVT-PAG.
            var origem = DerivarOrigem(ordem.Id.Value, natureza);
            if (await lancamentos.ExisteParaOrigemAsync(origem, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var consignacao = await ResolverContaAsync(MapaContaConsignacao.CodigoConta(natureza), cancellationToken).ConfigureAwait(false);

            var linhas = new[]
            {
                LinhaDe(caixa, LadoPartida.Debito, valor),
                LinhaDe(consignacao, LadoPartida.Credito, valor),
            };

            var lancamento = LancamentoContabil.Registrar(
                tenant.TenantId,
                ordem.DataPagamento,
                ordem.DataPagamento.Year,
                $"Consignacao {natureza} retida no pagamento {ordem.Numero} (a recolher)",
                OrigemLancamento.EventoAutomatico,
                origem,
                eventoContabilId: null,
                linhas,
                periodoAberto: true);

            lancamentos.Adicionar(lancamento);
            gerou = true;
        }

        if (gerou)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ContaContabil> ResolverContaAsync(string codigo, CancellationToken cancellationToken) =>
        await contas.ObterPorCodigoAsync(codigo, cancellationToken).ConfigureAwait(false)
            ?? throw new RoteiroContabilInvalidoException($"Conta {codigo} ausente no plano (rodar seed do plano de contas).");

    private static LinhaLancamento LinhaDe(ContaContabil conta, LadoPartida lado, ValorMonetario valor) =>
        new(conta.Id, conta.Codigo, conta.NaturezaInformacao, conta.Tipo, lado, valor);

    // GUID deterministico (SHA-256 truncado, mesmo algoritmo da cadeia de auditoria) a partir do id da
    // ordem + natureza, garantindo idempotencia do ajuste de consignacao por (ordem, natureza) sem colidir
    // com a origem do roteiro de pagamento. Nao e uso criptografico de seguranca — apenas derivacao estavel.
    private static Guid DerivarOrigem(Guid ordemId, NaturezaRetencao natureza)
    {
        Span<byte> buffer = stackalloc byte[16 + 1 + 4];
        ordemId.TryWriteBytes(buffer[..16]);
        buffer[16] = (byte)'#';
        BitConverter.TryWriteBytes(buffer[17..], (int)natureza);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(buffer, hash);
        return new Guid(hash[..16]);
    }
}
