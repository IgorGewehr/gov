using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Familia"/>.</summary>
public sealed class FamiliaRepository(AssistenciaSocialDbContext context) : IFamiliaRepository
{
    /// <inheritdoc />
    public void Adicionar(Familia familia)
    {
        ArgumentNullException.ThrowIfNull(familia);
        context.Familias.Add(familia);
    }

    /// <inheritdoc />
    public Task<Familia?> ObterPorIdAsync(FamiliaId id, CancellationToken cancellationToken)
        => context.Familias.FirstOrDefaultAsync(familia => familia.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Familia>> ListarPorTerritorioAsync(string territorio, CancellationToken cancellationToken)
        => await context.Familias
            .Where(familia => familia.Territorio == territorio)
            .OrderBy(familia => familia.DataReferenciamento)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Beneficio"/>.</summary>
public sealed class BeneficioRepository(AssistenciaSocialDbContext context) : IBeneficioRepository
{
    /// <inheritdoc />
    public void Adicionar(Beneficio beneficio)
    {
        ArgumentNullException.ThrowIfNull(beneficio);
        context.Beneficios.Add(beneficio);
    }

    /// <inheritdoc />
    public Task<Beneficio?> ObterPorIdAsync(BeneficioId id, CancellationToken cancellationToken)
        => context.Beneficios.FirstOrDefaultAsync(beneficio => beneficio.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Beneficio>> ListarPorFamiliaAsync(Guid familiaId, CancellationToken cancellationToken)
        => await context.Beneficios
            .Where(beneficio => beneficio.FamiliaId == familiaId)
            .OrderByDescending(beneficio => beneficio.Competencia)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Beneficio>> ListarConcedidosPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
        => await context.Beneficios
            .Where(beneficio => beneficio.Situacao == SituacaoBeneficio.Concedida && beneficio.Competencia == competencia)
            .OrderBy(beneficio => beneficio.FamiliaId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ProntuarioSuas"/>.</summary>
public sealed class ProntuarioSuasRepository(AssistenciaSocialDbContext context) : IProntuarioSuasRepository
{
    /// <inheritdoc />
    public void Adicionar(ProntuarioSuas prontuario)
    {
        ArgumentNullException.ThrowIfNull(prontuario);
        context.Prontuarios.Add(prontuario);
    }

    /// <inheritdoc />
    public Task<ProntuarioSuas?> ObterPorIdAsync(ProntuarioSuasId id, CancellationToken cancellationToken)
        => context.Prontuarios.FirstOrDefaultAsync(prontuario => prontuario.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<ProntuarioSuas?> ObterPorFamiliaAsync(Guid familiaId, CancellationToken cancellationToken)
        => context.Prontuarios.FirstOrDefaultAsync(prontuario => prontuario.FamiliaId == familiaId, cancellationToken);
}
