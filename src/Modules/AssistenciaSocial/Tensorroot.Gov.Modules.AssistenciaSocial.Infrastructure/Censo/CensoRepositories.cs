using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Censo;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Censo;

/// <summary>Implementacao EF Core do repositorio da unidade socioassistencial (3d.2).</summary>
public sealed class UnidadeSocioassistencialRepository(AssistenciaSocialDbContext context) : IUnidadeSocioassistencialRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(UnidadeSocioassistencial unidade, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unidade);
        await context.UnidadesSocioassistenciais.AddAsync(unidade, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UnidadeSocioassistencial?> ObterPorIdAsync(UnidadeSocioassistencialId id, CancellationToken cancellationToken)
        => await context.UnidadesSocioassistenciais
            .Include(u => u.Servicos)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnidadeSocioassistencial>> ListarAsync(CancellationToken cancellationToken)
        => await context.UnidadesSocioassistenciais
            .Include(u => u.Servicos)
            .OrderBy(u => u.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do formulario do Censo SUAS (3d.2).</summary>
public sealed class FormularioCensoSuasRepository(AssistenciaSocialDbContext context) : IFormularioCensoSuasRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(FormularioCensoSuas formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);
        await context.FormulariosCensoSuas.AddAsync(formulario, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FormularioCensoSuas?> ObterPorUnidadeExercicioAsync(UnidadeSocioassistencialId unidadeId, int exercicio, CancellationToken cancellationToken)
        => await context.FormulariosCensoSuas
            .FirstOrDefaultAsync(f => f.UnidadeId == unidadeId && f.Exercicio == exercicio, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<FormularioCensoSuas>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => await context.FormulariosCensoSuas
            .Where(f => f.Exercicio == exercicio)
            .OrderBy(f => f.UnidadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>
/// 3d.2 — implementacao EF Core de <see cref="IConsolidacaoCensoReadModel"/>. DERIVA o volume anual de
/// atendimentos somando os <c>RegistroMensalAtendimento</c> da unidade no exercicio e conta as familias
/// referenciadas (cadastro de familias). O Id da unidade do Censo e o mesmo UnidadeAtendimentoId
/// referenciado por RMA/Familia (Id reusado). O Global Query Filter por tenant garante o isolamento (I-9).
/// </summary>
public sealed class ConsolidacaoCensoReadModel(AssistenciaSocialDbContext context) : IConsolidacaoCensoReadModel
{
    /// <inheritdoc />
    public async Task<int> SomarAtendimentosDoExercicioAsync(Guid unidadeAtendimentoId, int exercicio, CancellationToken cancellationToken)
    {
        // Soma o volume de todos os RMAs (qualquer mes) da unidade no exercicio. A Competencia e mapeada
        // por value converter (Ano*100+Mes), entao o filtro por ano e feito em memoria sobre os RMAs da
        // unidade (volume tipicamente <= 12 por unidade/ano). As linhas vem por Include (sem N+1).
        var rmas = await context.RegistrosMensaisAtendimento
            .Include(rma => rma.Linhas)
            .Where(rma => rma.UnidadeAtendimentoId == unidadeAtendimentoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rmas
            .Where(rma => rma.Competencia.Ano == exercicio)
            .SelectMany(rma => rma.Linhas)
            .Sum(linha => linha.Quantidade);
    }

    /// <inheritdoc />
    public async Task<int> ContarFamiliasReferenciadasAsync(Guid unidadeAtendimentoId, CancellationToken cancellationToken)
        => await context.Familias
            .CountAsync(familia => familia.UnidadeAtendimentoId == unidadeAtendimentoId, cancellationToken)
            .ConfigureAwait(false);
}
