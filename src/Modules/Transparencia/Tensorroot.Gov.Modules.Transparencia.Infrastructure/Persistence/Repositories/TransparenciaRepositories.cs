using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="RemessaTce"/>.</summary>
public sealed class RemessaTceRepository(TransparenciaDbContext context) : IRemessaTceRepository
{
    /// <inheritdoc />
    public void Adicionar(RemessaTce remessa)
    {
        ArgumentNullException.ThrowIfNull(remessa);
        context.RemessasTce.Add(remessa);
    }

    /// <inheritdoc />
    public Task<RemessaTce?> ObterPorIdAsync(RemessaTceId id, CancellationToken cancellationToken)
        => context.RemessasTce.FirstOrDefaultAsync(remessa => remessa.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RemessaTce>> ListarPorPeriodoAsync(
        int exercicio,
        TipoPeriodo? tipo,
        SituacaoRemessaTce? situacao,
        CancellationToken cancellationToken)
        => await context.RemessasTce
            .Where(remessa => remessa.Periodo.Exercicio == exercicio)
            .Where(remessa => tipo == null || remessa.Periodo.Tipo == tipo)
            .Where(remessa => situacao == null || remessa.Situacao == situacao)
            .OrderBy(remessa => remessa.DataGeracao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="DeclaracaoFiscal"/>.</summary>
public sealed class DeclaracaoFiscalRepository(TransparenciaDbContext context) : IDeclaracaoFiscalRepository
{
    /// <inheritdoc />
    public void Adicionar(DeclaracaoFiscal declaracaoFiscal)
    {
        ArgumentNullException.ThrowIfNull(declaracaoFiscal);
        context.DeclaracoesFiscais.Add(declaracaoFiscal);
    }

    /// <inheritdoc />
    public Task<DeclaracaoFiscal?> ObterPorIdAsync(DeclaracaoFiscalId id, CancellationToken cancellationToken)
        => context.DeclaracoesFiscais.FirstOrDefaultAsync(declaracao => declaracao.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteVigenteAsync(
        TipoDeclaracaoFiscal tipo,
        int exercicio,
        int? mes,
        int? numeroBimestre,
        int? numeroQuadrimestre,
        CancellationToken cancellationToken)
        => context.DeclaracoesFiscais.AnyAsync(
            declaracao => declaracao.TipoDeclaracao == tipo
                && declaracao.Exercicio == exercicio
                && declaracao.Situacao != SituacaoDeclaracaoFiscal.Rejeitada
                && (mes == null || (declaracao.Competencia != null && declaracao.Competencia.Mes == mes))
                && (numeroBimestre == null || (declaracao.Bimestre != null && declaracao.Bimestre.Numero == numeroBimestre))
                && (numeroQuadrimestre == null || (declaracao.Quadrimestre != null && declaracao.Quadrimestre.Numero == numeroQuadrimestre)),
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeclaracaoFiscal>> ListarPorExercicioAsync(
        int exercicio,
        TipoDeclaracaoFiscal? tipo,
        SituacaoDeclaracaoFiscal? situacao,
        CancellationToken cancellationToken)
        => await context.DeclaracoesFiscais
            .Where(declaracao => declaracao.Exercicio == exercicio)
            .Where(declaracao => tipo == null || declaracao.TipoDeclaracao == tipo)
            .Where(declaracao => situacao == null || declaracao.Situacao == situacao)
            .OrderBy(declaracao => declaracao.DataConsolidacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
