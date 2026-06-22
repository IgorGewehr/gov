using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Servidor"/>.</summary>
public sealed class ServidorRepository(RecursosHumanosDbContext context) : IServidorRepository
{
    /// <inheritdoc />
    public void Adicionar(Servidor servidor)
    {
        ArgumentNullException.ThrowIfNull(servidor);
        context.Servidores.Add(servidor);
    }

    /// <inheritdoc />
    public Task<Servidor?> ObterPorIdAsync(ServidorId id, CancellationToken cancellationToken)
        => context.Servidores.FirstOrDefaultAsync(servidor => servidor.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Servidor?> ObterPorMatriculaAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(matricula);
        return context.Servidores.FirstOrDefaultAsync(servidor => servidor.Matricula == matricula, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> MatriculaExisteAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(matricula);
        return context.Servidores.AnyAsync(servidor => servidor.Matricula == matricula, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Servidor>> ListarAtivosAsync(CancellationToken cancellationToken)
        => await context.Servidores
            .Where(servidor => servidor.Situacao != SituacaoServidor.Desligado)
            .OrderBy(servidor => servidor.Matricula)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Cargo"/>.</summary>
public sealed class CargoRepository(RecursosHumanosDbContext context) : ICargoRepository
{
    /// <inheritdoc />
    public void Adicionar(Cargo cargo)
    {
        ArgumentNullException.ThrowIfNull(cargo);
        context.Cargos.Add(cargo);
    }

    /// <inheritdoc />
    public Task<Cargo?> ObterPorIdAsync(CargoId id, CancellationToken cancellationToken)
        => context.Cargos.FirstOrDefaultAsync(cargo => cargo.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Cargo>> ListarComVagasDisponiveisAsync(TipoCargo? tipo, CancellationToken cancellationToken)
        => await context.Cargos
            .Where(cargo => cargo.Situacao != SituacaoCargo.Extinto
                && cargo.VagasOcupadas < cargo.QuantidadeVagas
                && (tipo == null || cargo.Tipo == tipo))
            .OrderBy(cargo => cargo.Denominacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="FolhaDePagamento"/>.</summary>
public sealed class FolhaDePagamentoRepository(RecursosHumanosDbContext context) : IFolhaDePagamentoRepository
{
    /// <inheritdoc />
    public void Adicionar(FolhaDePagamento folha)
    {
        ArgumentNullException.ThrowIfNull(folha);
        context.FolhasDePagamento.Add(folha);
    }

    /// <inheritdoc />
    public Task<FolhaDePagamento?> ObterPorIdAsync(FolhaDePagamentoId id, CancellationToken cancellationToken)
        => context.FolhasDePagamento.FirstOrDefaultAsync(folha => folha.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<FolhaDePagamento?> ObterPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return context.FolhasDePagamento.FirstOrDefaultAsync(folha => folha.Competencia == competencia, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteParaCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return context.FolhasDePagamento.AnyAsync(folha => folha.Competencia == competencia, cancellationToken);
    }
}
