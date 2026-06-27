using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Certidoes;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de certidões de regularidade fiscal (CND/CPEN).</summary>
public sealed class CertidaoRegularidadeFiscalRepository(TributosDbContext context) : ICertidaoRegularidadeFiscalRepository
{
    /// <inheritdoc />
    public void Adicionar(CertidaoRegularidadeFiscal certidao)
    {
        ArgumentNullException.ThrowIfNull(certidao);
        context.CertidoesRegularidadeFiscal.Add(certidao);
    }

    /// <inheritdoc />
    public async Task<long> ObterProximoSequencialAsync(int exercicio, CancellationToken cancellationToken)
    {
        // Conta as certidões do tenant emitidas no exercício (Global Query Filter isola o tenant) e
        // devolve o próximo sequencial. TenantId explícito como defesa em profundidade (CLAUDE.md §3/§5).
        var tenantId = context.CurrentTenantId;
        var total = await context.CertidoesRegularidadeFiscal
            .LongCountAsync(c => c.TenantId == tenantId && c.DataEmissao.Year == exercicio, cancellationToken)
            .ConfigureAwait(false);
        return total + 1;
    }

    /// <inheritdoc />
    public Task<CertidaoRegularidadeFiscal?> ObterPorNumeroAsync(string numero, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        return context.CertidoesRegularidadeFiscal.FirstOrDefaultAsync(c => c.Numero == numero, cancellationToken);
    }
}

/// <summary>Implementação EF Core do repositório de declarações mensais de ISS (GIA).</summary>
public sealed class DeclaracaoGiaIssRepository(TributosDbContext context) : IDeclaracaoGiaIssRepository
{
    /// <inheritdoc />
    public void Adicionar(DeclaracaoGiaIss declaracao)
    {
        ArgumentNullException.ThrowIfNull(declaracao);
        context.DeclaracoesGiaIss.Add(declaracao);
    }

    /// <inheritdoc />
    public Task<DeclaracaoGiaIss?> ObterPorIdAsync(DeclaracaoGiaIssId id, CancellationToken cancellationToken)
        => context.DeclaracoesGiaIss
            .Include(declaracao => declaracao.Itens)
            .FirstOrDefaultAsync(declaracao => declaracao.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<DeclaracaoGiaIss?> ObterVigentePorContribuinteCompetenciaAsync(ContribuinteId contribuinteId, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);

        // TenantId explícito (defesa em profundidade além do Global Query Filter): a GIA de um tenant
        // jamais colide com a de outro por contribuinte/competência (CLAUDE.md §3/§5). "Vigente" = não
        // substituída (uma declaração substituída deixou de ser exigível).
        var tenantId = context.CurrentTenantId;
        return context.DeclaracoesGiaIss
            .Include(declaracao => declaracao.Itens)
            .FirstOrDefaultAsync(
                declaracao => declaracao.TenantId == tenantId
                    && declaracao.ContribuinteId == contribuinteId
                    && declaracao.Competencia == competencia
                    && declaracao.Situacao != SituacaoGiaIss.Substituida,
                cancellationToken);
    }
}

/// <summary>
/// Implementação EF Core da apuração da situação fiscal do contribuinte (insumo da CND/CPEN). Conta os
/// débitos próprios vencidos em aberto e a dívida ativa, separando exigíveis de suspensos. O volume por
/// contribuinte é modesto: a dívida ativa é avaliada em memória para reusar o invariante de prescrição
/// do domínio (<see cref="DividaAtiva.EstaPrescrita"/>), não traduzível em SQL.
/// </summary>
public sealed class SituacaoFiscalConsulta(TributosDbContext context) : ISituacaoFiscalConsulta
{
    /// <inheritdoc />
    public async Task<SituacaoFiscalContribuinte> ApurarAsync(ContribuinteId contribuinteId, DateOnly dataBase, CancellationToken cancellationToken)
    {
        var tenantId = context.CurrentTenantId;

        // Débitos próprios: lançamentos do contribuinte AINDA EM ABERTO e VENCIDOS na data-base.
        var lancamentosVencidos = await context.Lancamentos
            .CountAsync(
                lancamento => lancamento.TenantId == tenantId
                    && lancamento.ContribuinteId == contribuinteId
                    && lancamento.Situacao == SituacaoLancamento.Aberto
                    && lancamento.Vencimento < dataBase,
                cancellationToken)
            .ConfigureAwait(false);

        // Dívida ativa: separa exigíveis (cobráveis) de suspensos (parcelados) usando o invariante de
        // exigibilidade/prescrição do domínio. Carregada em memória (volume por contribuinte é pequeno).
        var dividas = await context.DividasAtivas
            .Where(divida => divida.TenantId == tenantId && divida.ContribuinteId == contribuinteId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var exigiveis = 0;
        var suspensas = 0;
        foreach (var divida in dividas)
        {
            switch (divida.Situacao)
            {
                case SituacaoDividaAtiva.Quitada:
                case SituacaoDividaAtiva.Cancelada:
                    // Extintas/canceladas não pesam na regularidade.
                    break;
                case SituacaoDividaAtiva.Parcelada:
                    // Exigibilidade SUSPENSA (CTN art. 151, VI) → habilita CPEN, não CND.
                    suspensas++;
                    break;
                case SituacaoDividaAtiva.EmExecucaoFiscal when divida.Garantida && !divida.EstaPrescrita(dataBase):
                    // Execução fiscal GARANTIDA por penhora/depósito suficiente (CTN art. 206; Súmula
                    // 451-STJ): exigibilidade afastada para fins de certidão → habilita CPEN, não Positiva (P2-7).
                    suspensas++;
                    break;
                default:
                    // Inscrita/CdaEmitida/Protestada/EmExecucaoFiscal (sem garantia): exigível se NÃO prescrita.
                    if (!divida.EstaPrescrita(dataBase))
                    {
                        exigiveis++;
                    }

                    break;
            }
        }

        return new SituacaoFiscalContribuinte(lancamentosVencidos, exigiveis, suspensas);
    }

    /// <inheritdoc />
    public Task<Contribuinte?> ResolverContribuintePorDocumentoAsync(string documento, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return Task.FromResult<Contribuinte?>(null);
        }

        return context.Contribuintes.FirstOrDefaultAsync(c => c.Documento == documento, cancellationToken);
    }
}
