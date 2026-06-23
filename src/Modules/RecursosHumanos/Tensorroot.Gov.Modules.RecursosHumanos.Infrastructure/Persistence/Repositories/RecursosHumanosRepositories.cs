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

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Servidor> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoServidor? situacao,
        RegimePrevidenciario? regime,
        CargoId? cargoId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Servidores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Nome (complex property → coluna "Nome") casa por trecho via LIKE (case-insensitive pela
            // collation do banco), com os curingas escapados. A Matricula tem value converter (VO↔string)
            // e nao e LIKE-avel diretamente: casa por igualdade exata quando o termo for uma matricula
            // valida (uso real: digitar a matricula completa). Matricula.De normaliza (uppercase/trim).
            var padrao = "%" + EscaparLike(termo.Trim()) + "%";
            var matriculaExata = TentarMatricula(termo);
            consulta = consulta.Where(servidor =>
                EF.Functions.Like(servidor.DadosPessoais.Nome, padrao, "\\")
                || (matriculaExata != null && servidor.Matricula == matriculaExata));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(servidor => servidor.Situacao == filtroSituacao);
        }

        if (regime is { } filtroRegime)
        {
            consulta = consulta.Where(servidor => servidor.Regime == filtroRegime);
        }

        if (cargoId is { } filtroCargo)
        {
            consulta = consulta.Where(servidor => servidor.CargoId == filtroCargo);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(servidor => servidor.DadosPessoais.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    // Escapa os curingas do LIKE (\, %, _) para tratar o termo do usuario como literal.
    private static string EscaparLike(string termo)
        => termo.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    // Constroi a Matricula a partir do termo, ou null se o termo nao for uma matricula valida
    // (vazio/excede o comprimento). Permite casar a matricula por igualdade exata sem quebrar a busca.
    private static Matricula? TentarMatricula(string termo)
    {
        try
        {
            return Matricula.De(termo);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
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
    public Task<FolhaDePagamento?> ObterPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken, TipoFolha tipo = TipoFolha.Mensal)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return context.FolhasDePagamento.FirstOrDefaultAsync(folha => folha.Competencia == competencia && folha.Tipo == tipo, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteParaCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken, TipoFolha tipo = TipoFolha.Mensal)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return context.FolhasDePagamento.AnyAsync(folha => folha.Competencia == competencia && folha.Tipo == tipo, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FolhaDePagamento>> ListarPorTipoEAnoAsync(int ano, TipoFolha tipo, CancellationToken cancellationToken)
    {
        // Competencia e persistida como inteiro (Ano*100 + Mes) via value converter; o intervalo do
        // ano e [ano*100+1, ano*100+12]. Filtra pelo tipo no SQL (indice (TenantId, Competencia, Tipo))
        // e materializa o intervalo do ano em memoria — lote pequeno por tenant/tipo/ano. Inclui os
        // Eventos (owned) para o handler do autosservico filtrar o dado-proprio.
        var folhasDoTipo = await context.FolhasDePagamento
            .Where(folha => folha.Tipo == tipo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return folhasDoTipo
            .Where(folha => folha.Competencia.Ano == ano)
            .OrderBy(folha => folha.Competencia.Mes)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FolhaDePagamento>> ListarPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        // Competencia e persistida como inteiro (value converter) — comparavel no SQL pela igualdade do VO.
        // Inclui os Eventos (owned) para a consolidacao fiscal mensal (P0-2) somar bases por servidor.
        return await context.FolhasDePagamento
            .Where(folha => folha.Competencia == competencia)
            .OrderBy(folha => folha.Tipo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FolhaDePagamento>> ListarPorServidorAsync(Guid servidorId, CancellationToken cancellationToken)
    {
        // Folhas em que o servidor tem ao menos um evento (owned EventoFolha.ServidorId). Filtra no SQL;
        // ordena por competencia (inteiro Ano*100+Mes) e tipo em memoria (lote pequeno por servidor).
        var doServidor = await context.FolhasDePagamento
            .Where(folha => folha.Eventos.Any(evento => evento.ServidorId == servidorId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return doServidor
            .OrderByDescending(folha => (folha.Competencia.Ano * 100) + folha.Competencia.Mes)
            .ThenBy(folha => folha.Tipo)
            .ToList();
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

/// <summary>
/// Implementacao EF Core do repositorio do vinculo usuario&#8596;servidor (ancora do autosservico).
/// Toda consulta respeita o Global Query Filter por tenant — o vinculo de outro tenant nao e visivel,
/// reforcando o isolamento dado-proprio por construcao.
/// </summary>
public sealed class VinculoServidorUsuarioRepository(RecursosHumanosDbContext context) : IVinculoServidorUsuarioRepository
{
    /// <inheritdoc />
    public void Adicionar(VinculoServidorUsuario vinculo)
    {
        ArgumentNullException.ThrowIfNull(vinculo);
        context.VinculosServidorUsuario.Add(vinculo);
    }

    /// <inheritdoc />
    public async Task<ServidorId?> ResolverServidorDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var vinculo = await context.VinculosServidorUsuario
            .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId, cancellationToken)
            .ConfigureAwait(false);
        return vinculo?.ServidorId;
    }

    /// <inheritdoc />
    public Task<bool> UsuarioJaVinculadoAsync(Guid usuarioId, CancellationToken cancellationToken)
        => context.VinculosServidorUsuario.AnyAsync(v => v.UsuarioId == usuarioId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ServidorJaVinculadoAsync(ServidorId servidorId, CancellationToken cancellationToken)
        => context.VinculosServidorUsuario.AnyAsync(v => v.ServidorId == servidorId, cancellationToken);
}
