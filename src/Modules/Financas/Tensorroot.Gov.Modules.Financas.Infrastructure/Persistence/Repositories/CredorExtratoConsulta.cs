using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consulta de leitura do extrato do credor: projeta os empenhos do credor (por documento normalizado),
/// com os valores acumulados de execução (empenhado/anulado/liquidado/pago) já mantidos no agregado
/// <c>Empenho</c>. Respeita o Global Query Filter de tenant do DbContext.
/// </summary>
public sealed class CredorExtratoConsulta(FinancasDbContext context) : ICredorExtratoConsulta
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CredorExtratoItem>> ListarEmpenhosDoCredorAsync(
        string documento,
        int? exercicio,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);

        var consulta = context.Empenhos
            .Where(empenho => empenho.Credor.Documento == documento);

        if (exercicio is not null)
        {
            consulta = consulta.Where(empenho => empenho.Exercicio == exercicio);
        }

        var empenhos = await consulta
            .OrderByDescending(empenho => empenho.Exercicio)
            .ThenBy(empenho => empenho.Numero)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return empenhos
            .Select(empenho => new CredorExtratoItem(
                empenho.Id.Value,
                empenho.Numero,
                empenho.Exercicio,
                empenho.DataEmpenho,
                empenho.Situacao.ToString(),
                empenho.ValorEmpenhado.Valor,
                empenho.ValorAnulado.Valor,
                empenho.ValorLiquidado.Valor,
                empenho.ValorPago.Valor))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CredorRetencaoResumo>> ListarRetencoesDoCredorAsync(
        string documento,
        int? exercicio,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);

        // Retencao e entidade-filha (owned) de Liquidacao; Liquidacao referencia o Empenho por EmpenhoId;
        // o credor esta no Empenho. Junta-se Liquidacoes x Empenhos pelo EmpenhoId para chegar ao credor.
        // Ambos os DbSets respeitam o Global Query Filter de tenant (CLAUDE.md §5).
        var empenhosDoCredor = context.Empenhos
            .Where(empenho => empenho.Credor.Documento == documento);

        if (exercicio is not null)
        {
            empenhosDoCredor = empenhosDoCredor.Where(empenho => empenho.Exercicio == exercicio);
        }

        var idsEmpenho = empenhosDoCredor.Select(empenho => empenho.Id);

        // Projeta as retenções num shape PLANO (escalares) e materializa antes de agrupar: o GroupBy com
        // agregação sobre coleção owned (Retencao é child de Liquidacao) NÃO traduz para SQL no provider
        // relacional (lança InvalidOperationException em runtime). O conjunto é limitado a UM credor, então
        // o group-by é feito em memória (LINQ-to-Objects), sem custo relevante. O filtro de tenant continua
        // aplicado no SQL pelos Global Query Filters de Liquidacoes/Empenhos.
        var retencoes = await context.Liquidacoes
            .Where(liquidacao => idsEmpenho.Contains(liquidacao.EmpenhoId))
            .SelectMany(liquidacao => liquidacao.Retencoes)
            .Select(retencao => new { retencao.Natureza, Valor = retencao.Valor.Valor, retencao.Recolhida })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return retencoes
            .GroupBy(retencao => retencao.Natureza)
            .Select(grupo =>
            {
                var valorRetido = grupo.Sum(retencao => retencao.Valor);
                var valorRecolhido = grupo.Where(retencao => retencao.Recolhida).Sum(retencao => retencao.Valor);
                return new CredorRetencaoResumo(
                    grupo.Key.ToString(),
                    valorRetido,
                    valorRecolhido,
                    ValorAReter: valorRetido - valorRecolhido);
            })
            .OrderBy(resumo => resumo.Natureza)
            .ToList();
    }
}
