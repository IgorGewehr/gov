using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do cadastro mestre de <see cref="Consignataria"/>.</summary>
public sealed class ConsignatariaRepository(RecursosHumanosDbContext context) : IConsignatariaRepository
{
    /// <inheritdoc />
    public void Adicionar(Consignataria consignataria)
    {
        ArgumentNullException.ThrowIfNull(consignataria);
        context.Consignatarias.Add(consignataria);
    }

    /// <inheritdoc />
    public Task<Consignataria?> ObterPorIdAsync(ConsignatariaId id, CancellationToken cancellationToken)
        => context.Consignatarias.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistePorCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        // Cnpj persiste via value converter (Digitos<->string), nao comparavel diretamente na query. As
        // consignatarias sao poucas por tenant (ja filtrado pelo Global Query Filter): compara em memoria.
        var digitos = cnpj.Digitos;
        var todas = await context.Consignatarias.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return todas.Exists(c => c.Cnpj.Digitos == digitos);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Consignataria>> ListarAsync(CancellationToken cancellationToken)
        => await context.Consignatarias
            .OrderBy(c => c.RazaoSocial)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do catalogo de <see cref="RubricaConsignavel"/>.</summary>
public sealed class RubricaConsignavelRepository(RecursosHumanosDbContext context) : IRubricaConsignavelRepository
{
    /// <inheritdoc />
    public void Adicionar(RubricaConsignavel rubrica)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        context.RubricasConsignaveis.Add(rubrica);
    }

    /// <inheritdoc />
    public Task<RubricaConsignavel?> ObterPorIdAsync(RubricaConsignavelId id, CancellationToken cancellationToken)
        => context.RubricasConsignaveis.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<RubricaConsignavel?> ObterAtivaPorCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var normalizado = codigo.Trim();
        return context.RubricasConsignaveis
            .FirstOrDefaultAsync(r => r.Ativa && r.Codigo == normalizado, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistePorCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var normalizado = codigo.Trim();
        return context.RubricasConsignaveis.AnyAsync(r => r.Codigo == normalizado, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListarCodigosQueContamParaMargemAsync(CancellationToken cancellationToken)
        => await context.RubricasConsignaveis
            .Where(r => r.Ativa && r.ContaParaMargem)
            .Select(r => r.Codigo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ContratoConsignacao"/>.</summary>
public sealed class ContratoConsignacaoRepository(RecursosHumanosDbContext context) : IContratoConsignacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(ContratoConsignacao contrato)
    {
        ArgumentNullException.ThrowIfNull(contrato);
        context.ContratosConsignacao.Add(contrato);
    }

    /// <inheritdoc />
    public Task<ContratoConsignacao?> ObterPorIdAsync(ContratoConsignacaoId id, CancellationToken cancellationToken)
        => context.ContratosConsignacao.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContratoConsignacao>> ListarAverbadasDoServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
        => await context.ContratosConsignacao
            .Where(c => c.ServidorId == servidorId && c.Situacao == SituacaoConsignacao.Averbada)
            .OrderBy(c => c.DataAverbacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContratoConsignacao>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
        => await context.ContratosConsignacao
            .Where(c => c.ServidorId == servidorId)
            .OrderByDescending(c => c.DataAverbacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
