using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using AgendamentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Agendamento.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio de agendas (tenant-scoped via Global Query Filter).</summary>
public sealed class AgendaProfissionalRepository(SaudeDbContext context) : IAgendaProfissionalRepository
{
    /// <inheritdoc />
    public void Adicionar(AgendaProfissional agenda)
    {
        ArgumentNullException.ThrowIfNull(agenda);
        context.AgendasProfissional.Add(agenda);
    }

    /// <inheritdoc />
    public Task<AgendaProfissional?> ObterPorIdAsync(AgendaProfissionalId id, CancellationToken cancellationToken)
        => context.AgendasProfissional.FirstOrDefaultAsync(agenda => agenda.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<AgendaProfissional?> ObterPorVagaAsync(VagaId vagaId, CancellationToken cancellationToken)
        => context.AgendasProfissional
            .FirstOrDefaultAsync(agenda => agenda.Vagas.Any(vaga => vaga.Id == vagaId), cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<VagaLivre>> BuscarVagasLivresAsync(
        ProfissionalId? profissionalId,
        EstabelecimentoId? estabelecimentoId,
        DateOnly? de,
        DateOnly? ate,
        TipoAtendimentoAgenda? tipo,
        CancellationToken cancellationToken)
    {
        var consulta = context.AgendasProfissional
            .Where(agenda => agenda.Situacao == SituacaoAgenda.Aberta);

        if (profissionalId is { } prof)
        {
            consulta = consulta.Where(agenda => agenda.ProfissionalId == prof);
        }

        if (estabelecimentoId is { } estab)
        {
            consulta = consulta.Where(agenda => agenda.EstabelecimentoId == estab);
        }

        if (tipo is { } filtroTipo)
        {
            consulta = consulta.Where(agenda => agenda.Tipo == filtroTipo);
        }

        if (de is { } dataDe)
        {
            consulta = consulta.Where(agenda => agenda.Data >= dataDe);
        }

        if (ate is { } dataAte)
        {
            consulta = consulta.Where(agenda => agenda.Data <= dataAte);
        }

        // Materializa as agendas (com suas vagas owned) e projeta em memoria: a expansao para vagas
        // livres + o enum convertido por value converter ficam fora do SQL (mesmo padrao das demais buscas).
        var agendas = await consulta
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return agendas
            .SelectMany(agenda => agenda.Vagas
                .Where(vaga => vaga.EstaLivre)
                .Select(vaga => new VagaLivre(
                    agenda.Id.Value,
                    vaga.Id.Value,
                    agenda.ProfissionalId.Value,
                    agenda.EstabelecimentoId.Value,
                    agenda.Tipo,
                    vaga.DataHora)))
            .OrderBy(vaga => vaga.DataHora)
            .ToList();
    }
}

/// <summary>Implementacao EF Core do repositorio de agendamentos (tenant-scoped via Global Query Filter).</summary>
public sealed class AgendamentoRepository(SaudeDbContext context) : IAgendamentoRepository
{
    /// <inheritdoc />
    public void Adicionar(AgendamentoRaiz agendamento)
    {
        ArgumentNullException.ThrowIfNull(agendamento);
        context.Agendamentos.Add(agendamento);
    }

    /// <inheritdoc />
    public Task<AgendamentoRaiz?> ObterPorIdAsync(AgendamentoId id, CancellationToken cancellationToken)
        => context.Agendamentos.FirstOrDefaultAsync(agendamento => agendamento.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> PacienteTemConflitoAsync(PacienteId pacienteId, DateTimeOffset dataHora, CancellationToken cancellationToken)
        => context.Agendamentos.AnyAsync(
            agendamento => agendamento.PacienteId == pacienteId
                && agendamento.DataHora == dataHora
                && (agendamento.Situacao == SituacaoAgendamento.Marcado
                    || agendamento.Situacao == SituacaoAgendamento.Confirmado),
            cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AgendamentoRaiz> Itens, int Total)> BuscarAsync(
        PacienteId? pacienteId,
        ProfissionalId? profissionalId,
        DateOnly? data,
        SituacaoAgendamento? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Agendamentos.AsQueryable();

        if (pacienteId is { } paciente)
        {
            consulta = consulta.Where(agendamento => agendamento.PacienteId == paciente);
        }

        if (profissionalId is { } profissional)
        {
            consulta = consulta.Where(agendamento => agendamento.ProfissionalId == profissional);
        }

        if (data is { } dataFiltro)
        {
            var inicio = new DateTimeOffset(dataFiltro.Year, dataFiltro.Month, dataFiltro.Day, 0, 0, 0, TimeSpan.Zero);
            var fim = inicio.AddDays(1);
            consulta = consulta.Where(agendamento => agendamento.DataHora >= inicio && agendamento.DataHora < fim);
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(agendamento => agendamento.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(agendamento => agendamento.DataHora)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

/// <summary>Implementacao EF Core do repositorio da fila de espera (tenant-scoped via Global Query Filter).</summary>
public sealed class FilaEsperaRepository(SaudeDbContext context) : IFilaEsperaRepository
{
    /// <inheritdoc />
    public void Adicionar(FilaEspera entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        context.FilasEspera.Add(entrada);
    }

    /// <inheritdoc />
    public Task<FilaEspera?> ObterPorIdAsync(FilaEsperaId id, CancellationToken cancellationToken)
        => context.FilasEspera.FirstOrDefaultAsync(fila => fila.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<FilaEspera?> ObterProximoAConvocarAsync(
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        TipoAtendimentoAgenda tipo,
        CancellationToken cancellationToken)
        => context.FilasEspera
            .Where(fila => fila.Situacao == SituacaoFilaEspera.Aguardando
                && fila.EstabelecimentoId == estabelecimentoId
                && fila.Tipo == tipo
                && (fila.ProfissionalId == profissionalId || fila.ProfissionalId == null))
            // Prioridade (Urgente=3 > Prioritaria=2 > Eletiva=1) e, dentro dela, ordem de chegada (FIFO).
            .OrderByDescending(fila => fila.Prioridade)
            .ThenBy(fila => fila.DataEntrada)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<FilaEspera> Itens, int Total)> BuscarAsync(
        EstabelecimentoId? estabelecimentoId,
        SituacaoFilaEspera? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.FilasEspera.AsQueryable();

        if (estabelecimentoId is { } estab)
        {
            consulta = consulta.Where(fila => fila.EstabelecimentoId == estab);
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(fila => fila.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderByDescending(fila => fila.Prioridade)
            .ThenBy(fila => fila.DataEntrada)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}
