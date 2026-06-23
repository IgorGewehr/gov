using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="MarcacaoPonto"/> (AFD append-only).</summary>
public sealed class MarcacaoPontoRepository(RecursosHumanosDbContext context) : IMarcacaoPontoRepository
{
    /// <inheritdoc />
    public void Adicionar(MarcacaoPonto marcacao)
    {
        ArgumentNullException.ThrowIfNull(marcacao);
        context.PontoMarcacoes.Add(marcacao);
    }

    /// <inheritdoc />
    public async Task<long?> ObterUltimoNsrAsync(CancellationToken cancellationToken)
    {
        // Maior NSR ja persistido no tenant (filtro global aplica o escopo do REP). O NSR e VO com
        // conversor para long; ordenamos por ele (comparavel no provedor) e lemos o maior.
        var ultima = await context.PontoMarcacoes
            .OrderByDescending(m => m.Nsr)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return ultima?.Nsr.Valor;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<long>> NsrsEquipamentoExistentesAsync(
        Guid repId,
        IReadOnlyCollection<long> nsrsEquipamento,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nsrsEquipamento);
        if (nsrsEquipamento.Count == 0)
        {
            return new HashSet<long>();
        }

        // Dedup em lote por (RepId, NsrEquipamento) — o Global Query Filter ja aplica o escopo do tenant.
        // O IN e traduzido pelo provedor; lemos so a coluna do NSR de equipamento (projecao enxuta).
        var candidatos = nsrsEquipamento.ToHashSet();
        var existentes = await context.PontoMarcacoes
            .Where(m => m.RepId == repId
                && m.NsrEquipamento != null
                && candidatos.Contains(m.NsrEquipamento.Value))
            .Select(m => m.NsrEquipamento!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return existentes.ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarcacaoPonto>> ListarPorPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken)
    {
        var de = new DateTimeOffset(inicio.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var ate = new DateTimeOffset(fim.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        // DateTimeOffset e armazenado como texto no SQLite e o range nao traduz uniformemente entre
        // provedores; ordenamos por NSR (cronologia do REP) e aplicamos o intervalo em memoria.
        var todas = await context.PontoMarcacoes
            .OrderBy(m => m.Nsr)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return todas.Where(m => m.DataHora >= de && m.DataHora <= ate).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarcacaoPonto>> ListarPorServidorCompetenciaAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var doServidor = await context.PontoMarcacoes
            .Where(m => m.ServidorId == servidorId)
            .OrderBy(m => m.Nsr)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return doServidor
            .Where(m => m.DataHora.Year == competencia.Ano && m.DataHora.Month == competencia.Mes)
            .OrderBy(m => m.DataHora)
            .ToList();
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="JornadaTrabalho"/>.</summary>
public sealed class JornadaTrabalhoRepository(RecursosHumanosDbContext context) : IJornadaTrabalhoRepository
{
    /// <inheritdoc />
    public void Adicionar(JornadaTrabalho jornada)
    {
        ArgumentNullException.ThrowIfNull(jornada);
        context.PontoJornadas.Add(jornada);
    }

    /// <inheritdoc />
    public async Task<JornadaTrabalho?> ObterVigenteAsync(Guid servidorId, DateOnly data, CancellationToken cancellationToken)
    {
        // Jornada ativa mais recente com vigencia anterior ou igual a data de referencia.
        var candidatas = await context.PontoJornadas
            .Where(j => j.ServidorId == servidorId && j.Ativa && j.VigenciaInicio <= data)
            .OrderByDescending(j => j.VigenciaInicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return candidatas.FirstOrDefault();
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ApuracaoPonto"/>.</summary>
public sealed class ApuracaoPontoRepository(RecursosHumanosDbContext context) : IApuracaoPontoRepository
{
    /// <inheritdoc />
    public void Adicionar(ApuracaoPonto apuracao)
    {
        ArgumentNullException.ThrowIfNull(apuracao);
        context.PontoApuracoes.Add(apuracao);
    }

    /// <inheritdoc />
    public void Remover(ApuracaoPonto apuracao)
    {
        ArgumentNullException.ThrowIfNull(apuracao);
        context.PontoApuracoes.Remove(apuracao);
    }

    /// <inheritdoc />
    public Task<ApuracaoPonto?> ObterPorIdAsync(ApuracaoPontoId id, CancellationToken cancellationToken)
        => context.PontoApuracoes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<ApuracaoPonto?> ObterPorServidorCompetenciaAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = (competencia.Ano * 100) + competencia.Mes;
        // Competencia e VO com conversor para int; igualdade de VO e traduzivel, mas filtramos por
        // servidor e resolvemos a competencia em memoria para uniformidade com os demais metodos.
        var doServidor = await context.PontoApuracoes
            .Where(a => a.ServidorId == servidorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return doServidor.FirstOrDefault(a => (a.Competencia.Ano * 100) + a.Competencia.Mes == alvo);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApuracaoPonto>> ListarPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = (competencia.Ano * 100) + competencia.Mes;
        var todas = await context.PontoApuracoes
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return todas
            .Where(a => (a.Competencia.Ano * 100) + a.Competencia.Mes == alvo)
            .OrderBy(a => a.ServidorId)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<int> ObterSaldoBancoHorasAnteriorAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = (competencia.Ano * 100) + competencia.Mes;
        var doServidor = await context.PontoApuracoes
            .Where(a => a.ServidorId == servidorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var anterior = doServidor
            .Where(a => (a.Competencia.Ano * 100) + a.Competencia.Mes < alvo)
            .OrderByDescending(a => (a.Competencia.Ano * 100) + a.Competencia.Mes)
            .FirstOrDefault();
        return anterior?.SaldoBancoHorasAtualMinutos ?? 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApuracaoPonto>> ListarPorServidorAsync(Guid servidorId, CancellationToken cancellationToken)
    {
        // Filtra pelo servidor no SQL; ordena por competencia (inteiro Ano*100+Mes) em memoria —
        // Competencia e value-converter e o lote por servidor e pequeno (uma apuracao por competencia).
        var doServidor = await context.PontoApuracoes
            .Where(a => a.ServidorId == servidorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return doServidor
            .OrderByDescending(a => (a.Competencia.Ano * 100) + a.Competencia.Mes)
            .ToList();
    }
}

/// <summary>Implementacao EF Core do repositorio do parque de equipamentos REP (<see cref="RepConfigurado"/>).</summary>
public sealed class RepRepository(RecursosHumanosDbContext context) : IRepRepository
{
    /// <inheritdoc />
    public void Adicionar(RepConfigurado rep)
    {
        ArgumentNullException.ThrowIfNull(rep);
        context.PontoReps.Add(rep);
    }

    /// <inheritdoc />
    public Task<RepConfigurado?> ObterPorIdAsync(RepConfiguradoId id, CancellationToken cancellationToken)
        => context.PontoReps.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RepConfigurado>> ListarAtivosAsync(CancellationToken cancellationToken)
        => await context.PontoReps
            .Where(r => r.Ativo)
            .OrderBy(r => r.IdentificacaoEquipamento)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
