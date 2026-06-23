using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Application.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;

/// <summary>Implementação EF Core do repositório de regras de classificação MDE (E-1).</summary>
public sealed class RegraClassificacaoMdeRepository(EducacaoDbContext context) : IRegraClassificacaoMdeRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(RegraClassificacaoMde regra, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(regra);
        await context.RegrasClassificacaoMde.AddAsync(regra, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> ExisteAlgumaAsync(CancellationToken cancellationToken)
        => context.RegrasClassificacaoMde.AnyAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegraClassificacaoMde>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken)
        => await context.RegrasClassificacaoMde
            .Where(regra => regra.VigenciaInicio <= referencia)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>
/// Implementação EF Core da porta de escrita da projeção de execução de Educação (E-1, Via A2).
/// Materializa a <see cref="LinhaExecucaoEducacao"/> a partir dos primitivos da porta — mantendo a entidade
/// de persistência confinada à Infraestrutura — e idempotente por <c>OrigemHash</c>.
/// </summary>
public sealed class LinhaExecucaoEducacaoRepository(EducacaoDbContext context, ITenantContext tenant) : ILinhaExecucaoEducacaoRepository
{
    /// <inheritdoc />
    public Task<bool> ExisteAsync(string origemHash, CancellationToken cancellationToken)
        => context.LinhasExecucaoEducacao.AnyAsync(linha => linha.OrigemHash == origemHash, cancellationToken);

    /// <inheritdoc />
    public async Task AdicionarReceitaBaseAsync(int exercicio, decimal valor, string origemHash, CancellationToken cancellationToken)
        => await context.LinhasExecucaoEducacao
            .AddAsync(LinhaExecucaoEducacao.ReceitaBase(tenant.TenantId, exercicio, valor, origemHash), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AdicionarDespesaAsync(int exercicio, string funcao, string? subfuncao, string? fonteRecurso, decimal valor, string origemHash, CancellationToken cancellationToken)
        => await context.LinhasExecucaoEducacao
            .AddAsync(LinhaExecucaoEducacao.Despesa(tenant.TenantId, exercicio, funcao, subfuncao, fonteRecurso, valor, origemHash), cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementação EF Core do repositório da distribuição do FUNDEB (E-3).</summary>
public sealed class DistribuicaoFundebRepository(EducacaoDbContext context) : IDistribuicaoFundebRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(DistribuicaoFundeb distribuicao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(distribuicao);
        await context.DistribuicoesFundeb.AddAsync(distribuicao, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DistribuicaoFundeb?> ObterPorIdAsync(DistribuicaoFundebId id, CancellationToken cancellationToken)
        => await context.DistribuicoesFundeb
            .FirstOrDefaultAsync(distribuicao => distribuicao.Id == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<DistribuicaoFundeb?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => await context.DistribuicoesFundeb
            .FirstOrDefaultAsync(distribuicao => distribuicao.Exercicio == exercicio, cancellationToken)
            .ConfigureAwait(false);
}
