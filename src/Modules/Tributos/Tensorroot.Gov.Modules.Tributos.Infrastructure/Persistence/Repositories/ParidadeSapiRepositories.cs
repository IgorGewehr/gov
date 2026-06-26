using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Desif;
using Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;
using Tensorroot.Gov.Modules.Tributos.Domain.Sim;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de declarações DES-IF (Apuração Mensal do ISSQN).</summary>
public sealed class DeclaracaoDesifRepository(TributosDbContext context) : IDeclaracaoDesifRepository
{
    /// <inheritdoc />
    public void Adicionar(DeclaracaoDesif declaracao)
    {
        ArgumentNullException.ThrowIfNull(declaracao);
        context.DeclaracoesDesif.Add(declaracao);
    }

    /// <inheritdoc />
    public Task<DeclaracaoDesif?> ObterPorIdAsync(DeclaracaoDesifId id, CancellationToken cancellationToken)
        => context.DeclaracoesDesif
            .Include(declaracao => declaracao.Subtitulos)
            .FirstOrDefaultAsync(declaracao => declaracao.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<DeclaracaoDesif?> ObterVigentePorContribuinteCompetenciaAsync(ContribuinteId contribuinteId, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);

        // TenantId explícito (defesa em profundidade além do Global Query Filter — CLAUDE.md §3/§5).
        // "Vigente" = não substituída (uma DES-IF substituída deixou de ser exigível).
        var tenantId = context.CurrentTenantId;
        return context.DeclaracoesDesif
            .Include(declaracao => declaracao.Subtitulos)
            .FirstOrDefaultAsync(
                declaracao => declaracao.TenantId == tenantId
                    && declaracao.ContribuinteId == contribuinteId
                    && declaracao.Competencia == competencia
                    && declaracao.Situacao != SituacaoDesif.Substituida,
                cancellationToken);
    }
}

/// <summary>Implementação EF Core do repositório de títulos de registro do S.I.M.</summary>
public sealed class TituloRegistroSimRepository(TributosDbContext context) : ITituloRegistroSimRepository
{
    /// <inheritdoc />
    public void Adicionar(TituloRegistroSim titulo)
    {
        ArgumentNullException.ThrowIfNull(titulo);
        context.TitulosRegistroSim.Add(titulo);
    }

    /// <inheritdoc />
    public Task<TituloRegistroSim?> ObterPorIdAsync(TituloRegistroSimId id, CancellationToken cancellationToken)
        => context.TitulosRegistroSim
            .Include(titulo => titulo.Produtos)
            .FirstOrDefaultAsync(titulo => titulo.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<long> ObterProximoSequencialAsync(int exercicio, CancellationToken cancellationToken)
    {
        // Conta os títulos do tenant concedidos (com data de registro) no exercício e devolve o próximo
        // sequencial. TenantId explícito como defesa em profundidade (CLAUDE.md §3/§5).
        var tenantId = context.CurrentTenantId;
        var total = await context.TitulosRegistroSim
            .LongCountAsync(
                titulo => titulo.TenantId == tenantId
                    && titulo.DataRegistro != null
                    && titulo.DataRegistro!.Value.Year == exercicio,
                cancellationToken)
            .ConfigureAwait(false);
        return total + 1;
    }
}

/// <summary>Implementação EF Core do repositório de Domicílios Eletrônicos do Contribuinte (DEC).</summary>
public sealed class DomicilioEletronicoContribuinteRepository(TributosDbContext context) : IDomicilioEletronicoContribuinteRepository
{
    /// <inheritdoc />
    public void Adicionar(DomicilioEletronicoContribuinte domicilio)
    {
        ArgumentNullException.ThrowIfNull(domicilio);
        context.DomiciliosEletronicos.Add(domicilio);
    }

    /// <inheritdoc />
    public Task<DomicilioEletronicoContribuinte?> ObterAtivoPorContribuinteAsync(ContribuinteId contribuinteId, CancellationToken cancellationToken)
    {
        var tenantId = context.CurrentTenantId;
        return context.DomiciliosEletronicos
            .Include(domicilio => domicilio.Mensagens)
            .FirstOrDefaultAsync(
                domicilio => domicilio.TenantId == tenantId
                    && domicilio.ContribuinteId == contribuinteId
                    && domicilio.Ativo,
                cancellationToken);
    }
}
