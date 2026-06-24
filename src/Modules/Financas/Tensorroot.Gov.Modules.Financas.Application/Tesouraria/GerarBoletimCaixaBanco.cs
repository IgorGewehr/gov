using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;

namespace Tensorroot.Gov.Modules.Financas.Application.Tesouraria;

/// <summary>Linha do Boletim de Caixa/Banco por conta (fechamento diário).</summary>
/// <param name="ContaId">Conta.</param>
/// <param name="Nome">Nome da conta.</param>
/// <param name="Tipo">Espécie.</param>
/// <param name="SaldoAnterior">Saldo antes do dia.</param>
/// <param name="Recebimentos">Total de entradas (recebimentos + transferências recebidas) no dia.</param>
/// <param name="Pagamentos">Total de saídas (pagamentos + transferências enviadas) no dia.</param>
/// <param name="SaldoDia">Saldo ao fim do dia.</param>
public sealed record LinhaBoletimContaDto(
    Guid ContaId,
    string Nome,
    string Tipo,
    decimal SaldoAnterior,
    decimal Recebimentos,
    decimal Pagamentos,
    decimal SaldoDia);

/// <summary>Boletim de Caixa/Banco consolidado de um dia (Lei 4.320/64 — controle da tesouraria).</summary>
/// <param name="Data">Dia de referência.</param>
/// <param name="Contas">Linhas por conta.</param>
/// <param name="TotalSaldoAnterior">Soma dos saldos anteriores.</param>
/// <param name="TotalRecebimentos">Soma das entradas do dia.</param>
/// <param name="TotalPagamentos">Soma das saídas do dia.</param>
/// <param name="TotalSaldoDia">Soma dos saldos ao fim do dia.</param>
public sealed record BoletimCaixaBancoDto(
    DateOnly Data,
    IReadOnlyList<LinhaBoletimContaDto> Contas,
    decimal TotalSaldoAnterior,
    decimal TotalRecebimentos,
    decimal TotalPagamentos,
    decimal TotalSaldoDia);

/// <summary>Gera o Boletim de Caixa/Banco de um dia (boletim de receita/despesa/fechamento).</summary>
/// <param name="Data">Dia de referência.</param>
public sealed record GerarBoletimCaixaBancoQuery(DateOnly Data) : IQuery<BoletimCaixaBancoDto>;

/// <summary>
/// Handler do boletim: para cada conta, deriva saldo anterior, entradas e saídas do dia a partir do
/// extrato. O saldo do dia = saldo do último movimento do dia, ou o anterior se não houve movimento.
/// </summary>
public sealed class GerarBoletimCaixaBancoHandler(IContaFinanceiraRepository contas)
    : IQueryHandler<GerarBoletimCaixaBancoQuery, BoletimCaixaBancoDto>
{
    /// <inheritdoc />
    public async Task<BoletimCaixaBancoDto> Handle(GerarBoletimCaixaBancoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lista = await contas.ListarAsync(cancellationToken).ConfigureAwait(false);

        var linhas = new List<LinhaBoletimContaDto>();
        foreach (var resumo in lista)
        {
            var conta = await contas.ObterPorIdAsync(resumo.Id, cancellationToken).ConfigureAwait(false);
            if (conta is null)
            {
                continue;
            }

            var movimentosAteOntem = conta.Movimentos.Where(m => m.Data < request.Data).OrderBy(m => m.Data).ToList();
            var movimentosDoDia = conta.Movimentos.Where(m => m.Data == request.Data).OrderBy(m => m.Data).ToList();

            // Saldo anterior = saldo após o último movimento antes do dia; se nenhum, o saldo inicial.
            var saldoAnterior = movimentosAteOntem.Count > 0 ? movimentosAteOntem[^1].SaldoApos.Valor : conta.SaldoInicial.Valor;

            var recebimentos = movimentosDoDia
                .Where(m => m.Tipo is TipoMovimentoFinanceiro.Recebimento or TipoMovimentoFinanceiro.TransferenciaEntrada)
                .Sum(m => m.Valor.Valor);
            var pagamentos = movimentosDoDia
                .Where(m => m.Tipo is TipoMovimentoFinanceiro.Pagamento or TipoMovimentoFinanceiro.TransferenciaSaida)
                .Sum(m => m.Valor.Valor);

            var saldoDia = movimentosDoDia.Count > 0 ? movimentosDoDia[^1].SaldoApos.Valor : saldoAnterior;

            linhas.Add(new LinhaBoletimContaDto(
                conta.Id.Value,
                conta.Nome,
                conta.Tipo.ToString(),
                saldoAnterior,
                recebimentos,
                pagamentos,
                saldoDia));
        }

        return new BoletimCaixaBancoDto(
            request.Data,
            linhas,
            linhas.Sum(l => l.SaldoAnterior),
            linhas.Sum(l => l.Recebimentos),
            linhas.Sum(l => l.Pagamentos),
            linhas.Sum(l => l.SaldoDia));
    }
}
