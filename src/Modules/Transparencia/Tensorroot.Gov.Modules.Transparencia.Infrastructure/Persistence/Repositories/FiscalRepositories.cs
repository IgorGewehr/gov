using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de regras de classificação setorial.</summary>
public sealed class FonteRecursoVinculadoRepository(TransparenciaDbContext context) : IFonteRecursoVinculadoRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(FonteRecursoVinculado regra, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(regra);
        await context.FontesRecursoVinculado.AddAsync(regra, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FonteRecursoVinculado>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken)
        => await context.FontesRecursoVinculado
            .Where(regra => regra.VigenciaInicio <= referencia)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório do calendário federal.</summary>
public sealed class CalendarioFederalRepository(TransparenciaDbContext context) : ICalendarioFederalRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(CalendarioFederal prazo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prazo);
        await context.CalendariosFederais.AddAsync(prazo, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarioFederal>> ListarPorExercicioAsync(int exercicio, string? chave, CancellationToken cancellationToken)
        => await context.CalendariosFederais
            .Where(prazo => prazo.Exercicio == exercicio)
            .Where(prazo => chave == null || prazo.Chave == chave)
            .OrderBy(prazo => prazo.DataLimite)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório dos pareceres de conselhos.</summary>
public sealed class ParecerConselhoRepository(TransparenciaDbContext context) : IParecerConselhoRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(ParecerConselho parecer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parecer);
        await context.ParecesConselho.AddAsync(parecer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParecerConselho>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => await context.ParecesConselho
            .Where(parecer => parecer.Exercicio == exercicio)
            .OrderBy(parecer => parecer.DataParecer)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório/projeção das linhas de execução fiscal.</summary>
public sealed class LinhaExecucaoFiscalRepository(TransparenciaDbContext context) : ILinhaExecucaoFiscalRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(LinhaExecucaoFiscal linha, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(linha);
        await context.LinhasExecucaoFiscal.AddAsync(linha, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(string origemHash, CancellationToken cancellationToken)
        => context.LinhasExecucaoFiscal.AnyAsync(linha => linha.OrigemHash == origemHash, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinhaExecucaoFiscal>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => await context.LinhasExecucaoFiscal
            .Where(linha => linha.Exercicio == exercicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
