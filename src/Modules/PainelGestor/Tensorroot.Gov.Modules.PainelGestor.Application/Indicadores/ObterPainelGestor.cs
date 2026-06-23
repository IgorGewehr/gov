using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Indicadores;

/// <summary>
/// <b>M8 — Painel do Gestor.</b> Consulta read-only que monta os 5 KPIs do gestor de um exercício a
/// partir do read model consolidado (alimentado por Integration Events). Reprodutível: dados os mesmos
/// valores materializados e os mesmos limites vigentes, o resultado é idêntico (sem relógio — o exercício
/// é a âncora). Tenant-scoped pelo Global Query Filter do DbContext.
/// </summary>
/// <param name="Exercicio">Exercício (ano) a consultar.</param>
public sealed record ObterPainelGestorQuery(int Exercicio) : IQuery<PainelGestorDto>;

/// <summary>
/// Handler do painel: lê o snapshot do exercício e os limites de pessoal vigentes, deriva os percentuais
/// e semáforos via funções de domínio puras e projeta o DTO. Toda a regra (LRF) é do domínio; o handler
/// só orquestra as portas. Quando não há snapshot, devolve um painel zerado/Indeterminado (degradação
/// graciosa) — nunca lança nem inventa número.
/// </summary>
public sealed class ObterPainelGestorHandler(
    IIndicadorMunicipioRepository repositorio,
    ILimitesPessoalProvider limitesProvider)
    : IQueryHandler<ObterPainelGestorQuery, PainelGestorDto>
{
    /// <inheritdoc />
    public async Task<PainelGestorDto> Handle(ObterPainelGestorQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await repositorio.ObterPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        var limites = await limitesProvider.ObterAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        if (snapshot is null)
        {
            return PainelVazio(request.Exercicio, limites);
        }

        return new PainelGestorDto(
            snapshot.Exercicio,
            ProjetarExecucao(snapshot),
            ProjetarMinimos(snapshot),
            ProjetarArrecadacao(snapshot),
            ProjetarPessoal(snapshot, limites),
            ProjetarPrestacaoContas(snapshot));
    }

    private static ExecucaoOrcamentariaDto ProjetarExecucao(IndicadorMunicipioSnapshot s)
    {
        var dotacao = s.DotacaoAtualizada;
        // Sem divisão por zero: dotação ausente → percentuais 0 (degradação graciosa).
        decimal Pct(decimal valor) => dotacao > 0m ? valor / dotacao : 0m;
        return new ExecucaoOrcamentariaDto(
            dotacao, s.Empenhado, s.Liquidado, s.Pago,
            Pct(s.Empenhado), Pct(s.Liquidado), Pct(s.Pago));
    }

    private static List<MinimoConstitucionalDto> ProjetarMinimos(IndicadorMunicipioSnapshot s)
        => s.Minimos
            .Select(m => new MinimoConstitucionalDto(
                m.Setor, m.ReceitaBase, m.Aplicado, m.PercentualAplicado, m.PercentualMinimo, m.Situacao))
            .ToList();

    private static ArrecadacaoDto ProjetarArrecadacao(IndicadorMunicipioSnapshot s)
        => new(s.ArrecadacaoTributaria, s.DividaAtivaSaldoInscrito, s.DividaAtivaSaldoAjuizado, s.DividaAtivaRecuperada);

    private static DespesaPessoalLrfDto ProjetarPessoal(IndicadorMunicipioSnapshot s, LimitesPessoalLrf limites)
    {
        var apuracao = ApuradorPessoalLrf.Apurar(s.DespesaPessoal, s.ReceitaCorrenteLiquida, limites);
        return new DespesaPessoalLrfDto(
            apuracao.DespesaPessoal,
            apuracao.Rcl,
            apuracao.PercentualDaRcl,
            limites.LimiteLegal,
            limites.LimitePrudencial,
            limites.LimiteAlerta,
            apuracao.Situacao);
    }

    private static PrestacaoContasDto ProjetarPrestacaoContas(IndicadorMunicipioSnapshot s)
    {
        var emDia = s.RemessasComPrazoVencido == 0;
        SituacaoLimite situacao;
        if (s.RemessasEnviadas == 0 && s.RemessasComPrazoVencido == 0)
        {
            // Nenhuma remessa do período registrada ainda — indeterminado (não afirma "em dia" no vazio).
            situacao = SituacaoLimite.Indeterminado;
        }
        else
        {
            situacao = emDia ? SituacaoLimite.Adequado : SituacaoLimite.Excedido;
        }

        return new PrestacaoContasDto(s.RemessasEnviadas, s.RemessasComPrazoVencido, emDia, situacao);
    }

    private static PainelGestorDto PainelVazio(int exercicio, LimitesPessoalLrf limites)
        => new(
            exercicio,
            new ExecucaoOrcamentariaDto(0m, 0m, 0m, 0m, 0m, 0m, 0m),
            [],
            new ArrecadacaoDto(0m, 0m, 0m, 0m),
            new DespesaPessoalLrfDto(0m, 0m, 0m, limites.LimiteLegal, limites.LimitePrudencial, limites.LimiteAlerta, SituacaoLimite.Indeterminado),
            new PrestacaoContasDto(0, 0, true, SituacaoLimite.Indeterminado));
}
