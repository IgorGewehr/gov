using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do read model de NFS-e.</summary>
public sealed class NotaFiscalServicoRepository(TributosDbContext context) : INotaFiscalServicoRepository
{
    /// <inheritdoc />
    public Task<bool> ExistePorChaveAsync(string chaveAcesso, CancellationToken cancellationToken)
    {
        // IS-6 — Defesa em profundidade no isolamento multi-tenant (CLAUDE.md S3/S5). O indice unico
        // de dedup e (TenantId, ChaveAcesso): a mesma chave pode existir legitimamente em tenants
        // distintos. Filtrar por TenantId EXPLICITO (alem do Global Query Filter) garante que a dedup
        // do Worker nao descarte uma nota de outro tenant nem dependa unicamente do GQF.
        var tenantId = context.CurrentTenantId;
        return context.NotasFiscaisServico
            .AnyAsync(nota => nota.TenantId == tenantId && nota.ChaveAcesso == chaveAcesso, cancellationToken);
    }

    /// <inheritdoc />
    public void Adicionar(NotaFiscalServico nota)
    {
        ArgumentNullException.ThrowIfNull(nota);
        context.NotasFiscaisServico.Add(nota);
    }
}
