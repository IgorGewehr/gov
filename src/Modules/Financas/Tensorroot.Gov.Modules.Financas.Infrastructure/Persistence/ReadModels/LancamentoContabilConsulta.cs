using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Queries;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Consultas analíticas sobre os lançamentos contábeis (razão lançamento-a-lançamento e diário
/// cronológico). Lê das tabelas LancamentosContabeis/PartidasContabeis (respeitando o filtro de
/// tenant) e computa o saldo acumulado em memória conforme a natureza do saldo da conta.
/// </summary>
public sealed class LancamentoContabilConsulta(FinancasDbContext context) : ILancamentoContabilConsulta
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LinhaRazaoAnaliticoDto>> RazaoAnaliticoAsync(
        Guid contaId,
        int exercicio,
        CancellationToken cancellationToken)
    {
        var natureza = await context.ContasContabeis
            .Where(c => c.Id == new ContaContabilId(contaId))
            .Select(c => (NaturezaSaldo?)c.NaturezaSaldo)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // Carrega os lançamentos do exercício que tocam a conta, em ordem cronológica (depois pela
        // chave do lançamento para estabilidade), com as partidas dessa conta.
        var lancamentos = await context.LancamentosContabeis
            .AsNoTracking()
            .Where(l => l.Exercicio == exercicio && l.Partidas.Any(p => p.ContaId == new ContaContabilId(contaId)))
            .OrderBy(l => l.Data)
            .ThenBy(l => l.Id)
            .Select(l => new
            {
                LancamentoId = l.Id.Value,
                l.Data,
                l.Historico,
                Origem = l.Origem.ToString(),
                Partidas = l.Partidas
                    .Where(p => p.ContaId == new ContaContabilId(contaId))
                    .Select(p => new { p.Lado, Valor = p.Valor.Valor })
                    .ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ehDevedora = natureza != NaturezaSaldo.Credora; // Devedora/Mista somam a débito.
        var linhas = new List<LinhaRazaoAnaliticoDto>();
        decimal saldo = 0m;

        foreach (var l in lancamentos)
        {
            foreach (var p in l.Partidas)
            {
                var debito = p.Lado == LadoPartida.Debito ? p.Valor : 0m;
                var credito = p.Lado == LadoPartida.Credito ? p.Valor : 0m;
                saldo += ehDevedora ? debito - credito : credito - debito;

                linhas.Add(new LinhaRazaoAnaliticoDto(
                    l.LancamentoId,
                    l.Data,
                    l.Historico,
                    l.Origem,
                    p.Lado.ToString(),
                    debito,
                    credito,
                    saldo));
            }
        }

        return linhas;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinhaDiarioDto>> DiarioAsync(
        int exercicio,
        DateOnly? de,
        DateOnly? ate,
        CancellationToken cancellationToken)
    {
        var lancamentos = await context.LancamentosContabeis
            .AsNoTracking()
            .Where(l => l.Exercicio == exercicio
                && (de == null || l.Data >= de)
                && (ate == null || l.Data <= ate))
            .OrderBy(l => l.Data)
            .ThenBy(l => l.Id)
            .Select(l => new
            {
                LancamentoId = l.Id.Value,
                l.Data,
                l.Historico,
                l.Origem,
                Partidas = l.Partidas
                    .OrderByDescending(p => p.Lado)
                    .Select(p => new { p.CodigoConta, p.Lado, Valor = p.Valor.Valor })
                    .ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return lancamentos
            .Select(l => new LinhaDiarioDto(
                l.LancamentoId,
                l.Data,
                l.Historico,
                l.Origem.ToString(),
                l.Partidas
                    .Select(p => new PartidaDiarioDto(p.CodigoConta, p.Lado.ToString(), p.Valor))
                    .ToList()))
            .ToList();
    }
}
