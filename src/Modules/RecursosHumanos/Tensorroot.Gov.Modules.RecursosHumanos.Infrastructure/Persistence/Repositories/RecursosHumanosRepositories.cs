using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

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

/// <summary>Implementacao EF Core do repositorio do catalogo de <see cref="RubricaFolha"/>.</summary>
public sealed class RubricaFolhaRepository(RecursosHumanosDbContext context) : IRubricaFolhaRepository
{
    /// <inheritdoc />
    public void Adicionar(RubricaFolha rubrica)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        context.Rubricas.Add(rubrica);
    }

    /// <inheritdoc />
    public Task<RubricaFolha?> ObterPorIdAsync(RubricaFolhaId id, CancellationToken cancellationToken)
        => context.Rubricas.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<RubricaFolha?> ObterVigentePorCodigoAsync(Rubrica codigo, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        ArgumentNullException.ThrowIfNull(competencia);
        // Codigo unico por tenant: filtra por codigo (traduzivel) e valida a vigencia em memoria.
        var alvo = (competencia.Ano * 100) + competencia.Mes;
        var candidatas = await context.Rubricas
            .Where(r => r.Ativa && r.Codigo == codigo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return candidatas
            .Where(r => ((r.VigenciaInicio.Ano * 100) + r.VigenciaInicio.Mes) <= alvo)
            .OrderByDescending(r => (r.VigenciaInicio.Ano * 100) + r.VigenciaInicio.Mes)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<bool> CodigoExisteAsync(Rubrica codigo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        return context.Rubricas.AnyAsync(r => r.Ativa && r.Codigo == codigo, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RubricaFolha>> ListarVigentesAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = (competencia.Ano * 100) + competencia.Mes;
        var ativas = await context.Rubricas
            .Where(r => r.Ativa)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ativas
            .Where(r => ((r.VigenciaInicio.Ano * 100) + r.VigenciaInicio.Mes) <= alvo)
            .OrderBy(r => r.Codigo.Codigo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>Implementacao EF Core do repositorio de escrita das tabelas legais (INSS/IRRF/RPPS).</summary>
public sealed class TabelasLegaisRepository(RecursosHumanosDbContext context) : ITabelasLegaisRepository
{
    /// <inheritdoc />
    public void Adicionar(TabelaInss tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasInss.Add(tabela);
    }

    /// <inheritdoc />
    public void Adicionar(TabelaIrrf tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasIrrf.Add(tabela);
    }

    /// <inheritdoc />
    public void Adicionar(TabelaRpps tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasRpps.Add(tabela);
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="EventoESocial"/>.</summary>
public sealed class EventoESocialRepository(RecursosHumanosDbContext context) : IEventoESocialRepository
{
    /// <inheritdoc />
    public void Adicionar(EventoESocial evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        context.EventosESocial.Add(evento);
    }

    /// <inheritdoc />
    public Task<EventoESocial?> ObterPorIdAsync(EventoESocialId id, CancellationToken cancellationToken)
        => context.EventosESocial.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<EventoESocial?> ObterPorChaveAsync(ChaveIdempotenciaEvento chave, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chave);
        // ChaveIdempotencia e owned (3 colunas). Filtra pelas 3 partes — index unico cobre a busca.
        return await context.EventosESocial
            .FirstOrDefaultAsync(
                e => e.ChaveIdempotencia.TipoEvento == chave.TipoEvento
                    && e.ChaveIdempotencia.IdNegocio == chave.IdNegocio
                    && e.ChaveIdempotencia.Competencia == chave.Competencia,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExisteParaChaveAsync(ChaveIdempotenciaEvento chave, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chave);
        return await context.EventosESocial
            .AnyAsync(
                e => e.ChaveIdempotencia.TipoEvento == chave.TipoEvento
                    && e.ChaveIdempotencia.IdNegocio == chave.IdNegocio
                    && e.ChaveIdempotencia.Competencia == chave.Competencia,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventoESocial>> ListarPorEstadoAsync(EstadoEventoESocial estado, CancellationToken cancellationToken)
    {
        // SQLite (dev) nao ordena DateTimeOffset no SQL; materializa filtrado e ordena por GeradoEm em
        // memoria (lote pequeno por tenant/estado). Em SqlServer o filtro ja usa o indice (TenantId, Estado).
        var eventos = await context.EventosESocial
            .Where(e => e.Estado == estado)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return eventos.OrderBy(e => e.GeradoEm).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventoESocial>> ListarTodosAsync(CancellationToken cancellationToken)
    {
        var eventos = await context.EventosESocial
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return eventos.OrderBy(e => e.GeradoEm).ToList();
    }
}
