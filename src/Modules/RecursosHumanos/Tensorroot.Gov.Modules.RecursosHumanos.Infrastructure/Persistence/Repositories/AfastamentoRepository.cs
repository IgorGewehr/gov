using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementacao EF Core do repositorio do agregado <see cref="Afastamento"/>. Toda consulta respeita o
/// Global Query Filter por tenant. A vigencia/sobreposicao por competencia e avaliada em memoria (lote
/// pequeno por servidor/tenant), reusando a regra do dominio (<see cref="Afastamento.VigenteEm"/>).
/// </summary>
public sealed class AfastamentoRepository(RecursosHumanosDbContext context) : IAfastamentoRepository
{
    /// <inheritdoc />
    public void Adicionar(Afastamento afastamento)
    {
        ArgumentNullException.ThrowIfNull(afastamento);
        context.Afastamentos.Add(afastamento);
    }

    /// <inheritdoc />
    public Task<Afastamento?> ObterPorIdAsync(AfastamentoId id, CancellationToken cancellationToken)
        => context.Afastamentos.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Afastamento?> ObterVigenteDoServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
        => context.Afastamentos
            .FirstOrDefaultAsync(
                a => a.ServidorId == servidorId && a.Situacao == SituacaoAfastamento.Vigente,
                cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Afastamento>> ListarVigentesNaCompetenciaAsync(
        Guid servidorId,
        Competencia competencia,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var id = new ServidorId(servidorId);
        var primeiroDia = new DateOnly(competencia.Ano, competencia.Mes, 1);
        var diasNoMes = DateTime.DaysInMonth(competencia.Ano, competencia.Mes);
        var ultimoDia = new DateOnly(competencia.Ano, competencia.Mes, diasNoMes);

        // Afastamentos do servidor que NAO estao cancelados; a sobreposicao exata e validada no dominio.
        var candidatos = await context.Afastamentos
            .Where(a => a.ServidorId == id && a.Situacao != SituacaoAfastamento.Cancelado)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return candidatos
            .Where(a => a.VigenteEm(primeiroDia, ultimoDia))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Afastamento>> ListarPorServidorAsync(Guid servidorId, CancellationToken cancellationToken)
    {
        var id = new ServidorId(servidorId);
        var itens = await context.Afastamentos
            .Where(a => a.ServidorId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return itens
            .OrderByDescending(a => a.Inicio)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Afastamento> Itens, int Total)> BuscarAsync(
        TipoAfastamento? tipo,
        SituacaoAfastamento? situacao,
        Competencia? competencia,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Afastamentos.AsQueryable();

        if (tipo is { } filtroTipo)
        {
            consulta = consulta.Where(a => a.Tipo == filtroTipo);
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(a => a.Situacao == filtroSituacao);
        }

        // Filtro por competencia: sobreposicao avaliada em memoria (regra do dominio). Sem competencia,
        // a paginacao ocorre no SQL.
        if (competencia is not null)
        {
            var primeiroDia = new DateOnly(competencia.Ano, competencia.Mes, 1);
            var diasNoMes = DateTime.DaysInMonth(competencia.Ano, competencia.Mes);
            var ultimoDia = new DateOnly(competencia.Ano, competencia.Mes, diasNoMes);

            var todos = await consulta.ToListAsync(cancellationToken).ConfigureAwait(false);
            var incidentes = todos
                .Where(a => a.VigenteEm(primeiroDia, ultimoDia))
                .OrderByDescending(a => a.Inicio)
                .ToList();

            var paginaCompetencia = incidentes
                .Skip((pagina - 1) * tamanho)
                .Take(tamanho)
                .ToList();
            return (paginaCompetencia, incidentes.Count);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);
        var itens = await consulta
            .OrderByDescending(a => a.Inicio)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (itens, total);
    }
}
