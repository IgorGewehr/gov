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
}
