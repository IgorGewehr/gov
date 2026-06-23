using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;
using AtendimentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.Atendimento;
using AtendimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.AtendimentoId;
using AtendimentoPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio de pacientes (sempre tenant-scoped via Global Query Filter).</summary>
public sealed class PacienteRepository(SaudeDbContext context) : IPacienteRepository
{
    /// <inheritdoc />
    public void Adicionar(Paciente paciente)
    {
        ArgumentNullException.ThrowIfNull(paciente);
        context.Pacientes.Add(paciente);
    }

    /// <inheritdoc />
    public Task<Paciente?> ObterPorIdAsync(PacienteId id, CancellationToken cancellationToken)
        => context.Pacientes.FirstOrDefaultAsync(paciente => paciente.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Paciente?> ObterPorCnsAsync(Cns cns, CancellationToken cancellationToken)
        => context.Pacientes.FirstOrDefaultAsync(paciente => paciente.Cns == cns, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistePorCnsAsync(Cns cns, CancellationToken cancellationToken)
        => context.Pacientes.AnyAsync(paciente => paciente.Cns == cns, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Paciente> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoPaciente? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Pacientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // Nome casa por trecho na coluna-sombra normalizada (lowercase + sem diacriticos).
            var padraoNome = BuscaTexto.MontarPadraoContains(BuscaTexto.Normalizar(termo));
            // Digitos casam por CPF (coluna-sombra CpfBusca, ate 11 digitos). O CNS, quando o termo for um
            // CNS valido completo (15 digitos + DV), casa por igualdade exata (chave de negocio do paciente).
            var digitos = BuscaTexto.SomenteDigitos(termo);
            var temDigitos = digitos.Length > 0;
            var padraoDigitos = temDigitos ? "%" + digitos + "%" : null;
            var cnsExato = Cns.EhValido(digitos) ? new Cns(digitos) : (Cns?)null;

            consulta = consulta.Where(paciente =>
                EF.Functions.Like(EF.Property<string>(paciente, "NomeBusca"), padraoNome, "\\")
                || (cnsExato != null && paciente.Cns == cnsExato.Value)
                || (temDigitos && EF.Property<string?>(paciente, "CpfBusca") != null
                    && EF.Functions.Like(EF.Property<string>(paciente, "CpfBusca"), padraoDigitos!, "\\")));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(paciente => paciente.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(paciente => EF.Property<string>(paciente, "NomeBusca"))
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

/// <summary>Implementacao EF Core do repositorio de atendimentos.</summary>
public sealed class AtendimentoRepository(SaudeDbContext context) : IAtendimentoRepository
{
    /// <inheritdoc />
    public void Adicionar(AtendimentoRaiz atendimento)
    {
        ArgumentNullException.ThrowIfNull(atendimento);
        context.Atendimentos.Add(atendimento);
    }

    /// <inheritdoc />
    public Task<AtendimentoRaiz?> ObterPorIdAsync(AtendimentoId id, CancellationToken cancellationToken)
        => context.Atendimentos.FirstOrDefaultAsync(atendimento => atendimento.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AtendimentoRaiz>> ListarPorPacienteAsync(
        AtendimentoPacienteId pacienteId,
        DateOnly? de,
        DateOnly? ate,
        CancellationToken cancellationToken)
    {
        var consulta = context.Atendimentos
            .Where(atendimento => atendimento.PacienteId == pacienteId);

        if (de is { } dataInicial)
        {
            var inicio = new DateTimeOffset(dataInicial.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            consulta = consulta.Where(atendimento => atendimento.DataHora >= inicio);
        }

        if (ate is { } dataFinal)
        {
            var fim = new DateTimeOffset(dataFinal.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            consulta = consulta.Where(atendimento => atendimento.DataHora <= fim);
        }

        return await consulta
            .OrderByDescending(atendimento => atendimento.DataHora)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementacao EF Core do repositorio de solicitacoes de regulacao.</summary>
public sealed class SolicitacaoRegulacaoRepository(SaudeDbContext context) : ISolicitacaoRegulacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(SolicitacaoRegulacao solicitacao)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);
        context.SolicitacoesRegulacao.Add(solicitacao);
    }

    /// <inheritdoc />
    public Task<SolicitacaoRegulacao?> ObterPorIdAsync(SolicitacaoRegulacaoId id, CancellationToken cancellationToken)
        => context.SolicitacoesRegulacao.FirstOrDefaultAsync(solicitacao => solicitacao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SolicitacaoRegulacao>> ListarPendentesAsync(
        string? codigoSigtap,
        Prioridade? prioridade,
        CancellationToken cancellationToken)
    {
        var consulta = context.SolicitacoesRegulacao
            .Where(solicitacao =>
                solicitacao.Situacao == SituacaoSolicitacaoRegulacao.Solicitada
                || solicitacao.Situacao == SituacaoSolicitacaoRegulacao.Devolvida);

        if (!string.IsNullOrWhiteSpace(codigoSigtap))
        {
            var normalizado = codigoSigtap.Trim();
            consulta = consulta.Where(solicitacao => solicitacao.Procedimento.CodigoSigtap == normalizado);
        }

        if (prioridade is { } filtroPrioridade)
        {
            consulta = consulta.Where(solicitacao => solicitacao.Prioridade == filtroPrioridade);
        }

        return await consulta
            .OrderByDescending(solicitacao => solicitacao.Prioridade)
            .ThenBy(solicitacao => solicitacao.DataSolicitacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
