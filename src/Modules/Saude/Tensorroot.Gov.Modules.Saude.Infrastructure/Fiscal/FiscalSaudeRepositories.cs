using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Fiscal;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;

/// <summary>Implementação EF Core do repositório de regras de classificação ASPS (S-1).</summary>
public sealed class RegraClassificacaoAspsRepository(SaudeDbContext context) : IRegraClassificacaoAspsRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(RegraClassificacaoAsps regra, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(regra);
        await context.RegrasClassificacaoAsps.AddAsync(regra, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> ExisteAlgumaAsync(CancellationToken cancellationToken)
        => context.RegrasClassificacaoAsps.AnyAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegraClassificacaoAsps>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken)
        => await context.RegrasClassificacaoAsps
            .Where(regra => regra.VigenciaInicio <= referencia)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>
/// Implementação EF Core da porta de escrita da projeção de execução de Saúde (S-1, Via A2). Materializa
/// a <see cref="LinhaExecucaoSaude"/> a partir dos primitivos da porta — mantendo a entidade de persistência
/// confinada à Infraestrutura — e idempotente por <c>OrigemHash</c> (mesma origem não duplica a projeção).
/// </summary>
public sealed class LinhaExecucaoSaudeRepository(SaudeDbContext context, ITenantContext tenant) : ILinhaExecucaoSaudeRepository
{
    /// <inheritdoc />
    public Task<bool> ExisteAsync(string origemHash, CancellationToken cancellationToken)
        => context.LinhasExecucaoSaude.AnyAsync(linha => linha.OrigemHash == origemHash, cancellationToken);

    /// <inheritdoc />
    public async Task AdicionarReceitaBaseAsync(int exercicio, decimal valor, string origemHash, CancellationToken cancellationToken)
        => await context.LinhasExecucaoSaude
            .AddAsync(LinhaExecucaoSaude.ReceitaBase(tenant.TenantId, exercicio, valor, origemHash), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AdicionarDespesaAsync(int exercicio, string funcao, string? subfuncao, string? fonteRecurso, decimal valor, string origemHash, CancellationToken cancellationToken)
        => await context.LinhasExecucaoSaude
            .AddAsync(LinhaExecucaoSaude.Despesa(tenant.TenantId, exercicio, funcao, subfuncao, fonteRecurso, valor, origemHash), cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório do Fundo Municipal de Saúde (S-2).</summary>
public sealed class FundoMunicipalSaudeRepository(SaudeDbContext context) : IFundoMunicipalSaudeRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(FundoMunicipalSaude fundo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fundo);
        await context.FundosMunicipaisSaude.AddAsync(fundo, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FundoMunicipalSaude?> ObterPorIdAsync(FundoMunicipalSaudeId id, CancellationToken cancellationToken)
        => await context.FundosMunicipaisSaude
            .FirstOrDefaultAsync(fundo => fundo.Id == id, cancellationToken)
            .ConfigureAwait(false);
}
