using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de tabelas de alíquota do ISS.</summary>
public sealed class TabelaAliquotaIssRepository(TributosDbContext context) : ITabelaAliquotaIssRepository
{
    /// <inheritdoc />
    public void Adicionar(TabelaAliquotaIss tabela)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        context.TabelasAliquotaIss.Add(tabela);
    }

    /// <inheritdoc />
    public async Task<TabelaAliquotaIss?> ObterVigentePorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var aaaaMm = (competencia.Ano * 100) + competencia.Mes;

        // A tabela vigente é a de maior início de vigência que não ultrapassa a competência apurada.
        return await context.TabelasAliquotaIss
            .Include(tabela => tabela.Itens)
            .Where(tabela => tabela.Vigente && tabela.VigenciaInicioAaaaMm <= aaaaMm)
            .OrderByDescending(tabela => tabela.VigenciaInicioAaaaMm)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementação EF Core do repositório de apurações mensais do ISS (livro eletrônico).</summary>
public sealed class ApuracaoIssRepository(TributosDbContext context) : IApuracaoIssRepository
{
    /// <inheritdoc />
    public void Adicionar(ApuracaoIss apuracao)
    {
        ArgumentNullException.ThrowIfNull(apuracao);
        context.ApuracoesIss.Add(apuracao);
    }

    /// <inheritdoc />
    public Task<ApuracaoIss?> ObterPorContribuinteCompetenciaAsync(ContribuinteId contribuinteId, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return context.ApuracoesIss
            .Include(apuracao => apuracao.Itens)
            .FirstOrDefaultAsync(apuracao => apuracao.ContribuinteId == contribuinteId && apuracao.Competencia == competencia, cancellationToken);
    }
}

/// <summary>Implementação EF Core da consulta às NFS-e ingeridas (base da apuração do ISS).</summary>
public sealed class NotaFiscalServicoConsulta(TributosDbContext context) : INotaFiscalServicoConsulta
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<NotaFiscalServico>> ListarVigentesPorPrestadorCompetenciaAsync(
        string prestadorCnpj,
        Competencia competencia,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prestadorCnpj);
        ArgumentNullException.ThrowIfNull(competencia);

        return await context.NotasFiscaisServico
            .Where(nota => nota.PrestadorCnpj == prestadorCnpj
                && nota.Competencia == competencia
                && nota.Situacao == SituacaoNfse.Normal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementação EF Core do repositório de alíquotas do ITBI.</summary>
public sealed class AliquotaItbiRepository(TributosDbContext context) : IAliquotaItbiRepository
{
    /// <inheritdoc />
    public void Adicionar(AliquotaItbi aliquota)
    {
        ArgumentNullException.ThrowIfNull(aliquota);
        context.AliquotasItbi.Add(aliquota);
    }

    /// <inheritdoc />
    public Task<AliquotaItbi?> ObterVigenteAsync(int exercicio, CancellationToken cancellationToken)
        => context.AliquotasItbi
            .FirstOrDefaultAsync(aliquota => aliquota.Exercicio == exercicio && aliquota.Vigente, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de transmissões imobiliárias (ITBI).</summary>
public sealed class TransmissaoImobiliariaRepository(TributosDbContext context) : ITransmissaoImobiliariaRepository
{
    /// <inheritdoc />
    public void Adicionar(TransmissaoImobiliaria transmissao)
    {
        ArgumentNullException.ThrowIfNull(transmissao);
        context.TransmissoesImobiliarias.Add(transmissao);
    }

    /// <inheritdoc />
    public Task<TransmissaoImobiliaria?> ObterPorIdAsync(TransmissaoImobiliariaId id, CancellationToken cancellationToken)
        => context.TransmissoesImobiliarias.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}

/// <summary>Implementação EF Core do repositório de processos de arbitramento do ITBI (CTN art. 148).</summary>
public sealed class ProcessoArbitramentoItbiRepository(TributosDbContext context) : IProcessoArbitramentoItbiRepository
{
    /// <inheritdoc />
    public void Adicionar(ProcessoArbitramentoItbi processo)
    {
        ArgumentNullException.ThrowIfNull(processo);
        context.ProcessosArbitramentoItbi.Add(processo);
    }

    /// <inheritdoc />
    public Task<ProcessoArbitramentoItbi?> ObterPorIdAsync(ProcessoArbitramentoItbiId id, CancellationToken cancellationToken)
        => context.ProcessosArbitramentoItbi.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
