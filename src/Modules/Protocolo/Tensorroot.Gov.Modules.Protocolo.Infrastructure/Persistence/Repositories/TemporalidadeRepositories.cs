using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do <see cref="PlanoDeClassificacao"/>.</summary>
public sealed class PlanoDeClassificacaoRepository(ProtocoloDbContext context) : IPlanoDeClassificacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(PlanoDeClassificacao plano)
    {
        ArgumentNullException.ThrowIfNull(plano);
        context.PlanosClassificacao.Add(plano);
    }

    /// <inheritdoc />
    public Task<PlanoDeClassificacao?> ObterAtivoAsync(CancellationToken cancellationToken)
        => context.PlanosClassificacao
            .Include(plano => plano.Classes)
            .FirstOrDefaultAsync(plano => plano.Ativo, cancellationToken);
}

/// <summary>Implementacao EF Core do repositorio da <see cref="TabelaTemporalidade"/>.</summary>
public sealed class TabelaTemporalidadeRepository(ProtocoloDbContext context) : ITabelaTemporalidadeRepository
{
    /// <inheritdoc />
    public void Adicionar(TabelaTemporalidade tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasTemporalidade.Add(tabela);
    }

    /// <inheritdoc />
    public Task<TabelaTemporalidade?> ObterAtivaAsync(CancellationToken cancellationToken)
        => context.TabelasTemporalidade
            .Include(ttd => ttd.Regras)
            .FirstOrDefaultAsync(ttd => ttd.Ativa, cancellationToken);
}

/// <summary>Implementacao EF Core do repositorio da <see cref="DestinacaoProcesso"/>.</summary>
public sealed class DestinacaoProcessoRepository(ProtocoloDbContext context) : IDestinacaoProcessoRepository
{
    /// <inheritdoc />
    public void Adicionar(DestinacaoProcesso destinacao)
    {
        ArgumentNullException.ThrowIfNull(destinacao);
        context.DestinacoesProcesso.Add(destinacao);
    }

    /// <inheritdoc />
    public Task<DestinacaoProcesso?> ObterPorProcessoAsync(Guid processoId, CancellationToken cancellationToken)
        => context.DestinacoesProcesso.FirstOrDefaultAsync(ficha => ficha.ProcessoId == processoId, cancellationToken);

    /// <inheritdoc />
    public Task<DestinacaoProcesso?> ObterPorIdAsync(DestinacaoProcessoId id, CancellationToken cancellationToken)
        => context.DestinacoesProcesso.FirstOrDefaultAsync(ficha => ficha.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DestinacaoProcesso>> ListarAguardandoPrazoAsync(CancellationToken cancellationToken)
        => await context.DestinacoesProcesso
            .Where(ficha => ficha.Estado == EstadoDestinacao.AguardandoPrazo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
