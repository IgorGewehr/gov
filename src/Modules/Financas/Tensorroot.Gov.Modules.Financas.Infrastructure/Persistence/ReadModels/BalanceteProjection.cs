using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Projeção do balancete sobre <c>financas.balancete_conta</c>. Cria a linha do período herdando o
/// saldo anterior do mês precedente (contas que encerram zeram na virada de exercício).
/// </summary>
public sealed class BalanceteProjection(FinancasDbContext context) : IBalanceteProjection
{
    /// <inheritdoc />
    public async Task<LinhaBalancete> ObterOuCriarLinhaAsync(
        ContaContabil conta,
        int exercicio,
        int periodoMes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conta);

        var contaId = conta.Id.Value;

        // Consulta primeiro o change tracker local: dentro de um mesmo lançamento (ou de um mesmo
        // lote de drenagem do Outbox processado no MESMO contexto), duas partidas podem tocar a MESMA
        // conta/período. Uma linha recém-adicionada e AINDA não persistida não é vista por uma query
        // ao banco — provocaria DUAS inserções com a mesma chave única (conta+exercício+mês) e violação
        // de UNIQUE. Reutilizar a linha já rastreada (Added ou Unchanged) evita o conflito.
        var local = context.BalancetesConta.Local
            .FirstOrDefault(linha => linha.ContaId == contaId && linha.Exercicio == exercicio && linha.PeriodoMes == periodoMes);
        if (local is not null)
        {
            return local;
        }

        var existente = await context.BalancetesConta
            .AsTracking()
            .FirstOrDefaultAsync(
                linha => linha.ContaId == contaId && linha.Exercicio == exercicio && linha.PeriodoMes == periodoMes,
                cancellationToken)
            .ConfigureAwait(false);

        if (existente is not null)
        {
            return existente;
        }

        var saldoAnterior = await SaldoAnteriorAsync(conta, exercicio, periodoMes, cancellationToken).ConfigureAwait(false);

        var nova = new LinhaBalancete
        {
            TenantId = conta.TenantId,
            ContaId = contaId,
            CodigoConta = conta.Codigo.Codigo,
            Titulo = conta.Titulo,
            NaturezaSaldo = conta.NaturezaSaldo,
            NaturezaInformacao = conta.NaturezaInformacao,
            Nivel = conta.Nivel,
            Exercicio = exercicio,
            PeriodoMes = periodoMes,
            SaldoAnterior = saldoAnterior,
            SaldoAtual = saldoAnterior,
        };

        context.BalancetesConta.Add(nova);
        return nova;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinhaBalancete>> ListarPorPeriodoAsync(
        int exercicio,
        int periodoMes,
        CancellationToken cancellationToken)
        => await context.BalancetesConta
            .Where(linha => linha.Exercicio == exercicio && linha.PeriodoMes == periodoMes)
            .OrderBy(linha => linha.CodigoConta)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinhaBalancete>> ListarRazaoContaAsync(
        Guid contaId,
        int exercicio,
        CancellationToken cancellationToken)
        => await context.BalancetesConta
            .Where(linha => linha.ContaId == contaId && linha.Exercicio == exercicio)
            .OrderBy(linha => linha.PeriodoMes)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<decimal> SaldoAnteriorAsync(
        ContaContabil conta,
        int exercicio,
        int periodoMes,
        CancellationToken cancellationToken)
    {
        var contaId = conta.Id.Value;

        // Período 0 (Abertura do exercício seguinte, 01/01) — DESIGN encerramento §2/§4.5.
        // O saldo inicial vem do MENOR período do MESMO exercício que já exista (em geral o mês 1,
        // ainda sem movimento). Se nenhum existir, transpõe o saldo FINAL do exercício anterior
        // (último período: mês 13 de apuração, ou mês 12). Contas que encerram (3/4/5/6) já estão
        // zeradas após a apuração; nada a transpor.
        if (periodoMes == 0)
        {
            if (conta.Encerramento)
            {
                return 0m;
            }

            var fechamentoAnterior = await context.BalancetesConta
                .Where(linha => linha.ContaId == contaId && linha.Exercicio == exercicio - 1)
                .OrderByDescending(linha => linha.PeriodoMes)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return fechamentoAnterior?.SaldoAtual ?? 0m;
        }

        // Períodos 2..13: herda do maior período anterior do mesmo exercício (mês 13 lê do mês 12).
        if (periodoMes > 1)
        {
            var mesAnterior = await context.BalancetesConta
                .Where(linha => linha.ContaId == contaId && linha.Exercicio == exercicio && linha.PeriodoMes < periodoMes)
                .OrderByDescending(linha => linha.PeriodoMes)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return mesAnterior?.SaldoAtual ?? 0m;
        }

        // Período 1 (virada de exercício): contas que encerram (5/6/3/4) zeram; permanentes (1/2)
        // carregam o saldo final do exercício anterior — inclusive o período 0 (abertura), se houver.
        if (conta.Encerramento)
        {
            return 0m;
        }

        var exercicioAnterior = await context.BalancetesConta
            .Where(linha => linha.ContaId == contaId && linha.Exercicio == exercicio - 1)
            .OrderByDescending(linha => linha.PeriodoMes)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var aberturaEsteExercicio = await context.BalancetesConta
            .Where(linha => linha.ContaId == contaId && linha.Exercicio == exercicio && linha.PeriodoMes == 0)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // A abertura (mês 0) deste exercício, quando existe, já consolida transposição + resultado.
        return aberturaEsteExercicio?.SaldoAtual ?? exercicioAnterior?.SaldoAtual ?? 0m;
    }
}
