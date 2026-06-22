using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório do read model de NFS-e.</summary>
public sealed class NotaFiscalServicoRepository(TributosDbContext context) : INotaFiscalServicoRepository
{
    /// <inheritdoc />
    public Task<bool> ExistePorChaveAsync(string chaveAcesso, CancellationToken cancellationToken)
        => context.NotasFiscaisServico.AnyAsync(nota => nota.ChaveAcesso == chaveAcesso, cancellationToken);

    /// <inheritdoc />
    public void Adicionar(NotaFiscalServico nota)
    {
        ArgumentNullException.ThrowIfNull(nota);
        context.NotasFiscaisServico.Add(nota);
    }
}
