using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;

/// <summary>Persistência dos registros de geração da MSC sobre <c>financas.msc_gerada</c>.</summary>
public sealed class MscGeradaStore(FinancasDbContext context) : IMscGeradaStore
{
    /// <inheritdoc />
    public async Task<MscGeradaRegistro?> ObterAsync(
        int exercicio,
        int mes,
        int tipoMatriz,
        CancellationToken cancellationToken)
        => await context.MscsGeradas
            .FirstOrDefaultAsync(
                r => r.Exercicio == exercicio && r.Mes == mes && r.TipoMatriz == tipoMatriz,
                cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Adicionar(MscGeradaRegistro registro)
    {
        ArgumentNullException.ThrowIfNull(registro);
        context.MscsGeradas.Add(registro);
    }
}
