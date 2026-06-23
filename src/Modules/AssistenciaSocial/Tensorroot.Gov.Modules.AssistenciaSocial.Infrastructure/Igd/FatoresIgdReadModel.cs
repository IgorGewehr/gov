using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Igd;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Igd;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Igd;

/// <summary>
/// 3d.3 — implementacao EF Core de <see cref="IFatoresIgdReadModel"/>. DERIVA os tres fatores LOCAIS [0,1]
/// da estimativa do IGD a partir das entidades JA existentes (Familia, AcompanhamentoCondicionalidade,
/// Beneficio), tenant-scoped. Sao indicadores locais — NAO o extrato oficial do MDS (rotulo no resultado).
/// </summary>
public sealed class FatoresIgdReadModel(AssistenciaSocialDbContext context) : IFatoresIgdReadModel
{
    /// <inheritdoc />
    public async Task<FatoresIgd> ObterFatoresAsync(int exercicio, CancellationToken cancellationToken)
    {
        var fatorAtualizacao = await CalcularFatorAtualizacaoCadastralAsync(cancellationToken).ConfigureAwait(false);
        var fatorCondicionalidades = await CalcularFatorCondicionalidadesAsync(exercicio, cancellationToken).ConfigureAwait(false);
        var fatorGestao = await CalcularFatorGestaoBeneficiosAsync(exercicio, cancellationToken).ConfigureAwait(false);

        return new FatoresIgd(fatorAtualizacao, fatorCondicionalidades, fatorGestao);
    }

    /// <summary>Taxa de atualizacao cadastral local: proporcao de familias com cadastro dentro da vigencia.</summary>
    private async Task<decimal> CalcularFatorAtualizacaoCadastralAsync(CancellationToken cancellationToken)
    {
        var total = await context.Familias.CountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return 0m;
        }

        // Cadastro em dia = situacao diferente de AtualizacaoVencida (Referenciada/Regularizada).
        var emDia = await context.Familias
            .CountAsync(familia => familia.Situacao != SituacaoFamilia.AtualizacaoVencida, cancellationToken)
            .ConfigureAwait(false);

        return Proporcao(emDia, total);
    }

    /// <summary>Cumprimento de condicionalidades: proporcao de acompanhamentos sem efeito (sem descumprimento efetivo) no exercicio.</summary>
    private async Task<decimal> CalcularFatorCondicionalidadesAsync(int exercicio, CancellationToken cancellationToken)
    {
        // Filtro por exercicio em memoria (Competencia mapeada por value converter — projecao da parte
        // Ano nao e traduzivel; materializa-se a Competencia inteira, que round-trips pelo converter).
        var acompanhamentos = await context.AcompanhamentosCondicionalidade
            .Select(a => new { a.Competencia, a.Efeito })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var doExercicio = acompanhamentos.Where(a => a.Competencia.Ano == exercicio).ToList();
        if (doExercicio.Count == 0)
        {
            // Sem acompanhamentos no exercicio: fator neutro maximo (nenhum descumprimento registrado).
            return 1m;
        }

        var semEfeito = doExercicio.Count(a => a.Efeito == EfeitoDescumprimento.Nenhum);
        return Proporcao(semEfeito, doExercicio.Count);
    }

    /// <summary>Gestao de beneficios: proporcao de beneficios decididos (concedidos/indeferidos) sobre o total no exercicio.</summary>
    private async Task<decimal> CalcularFatorGestaoBeneficiosAsync(int exercicio, CancellationToken cancellationToken)
    {
        var beneficios = await context.Beneficios
            .Select(b => new { b.Competencia, b.Situacao })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var doExercicio = beneficios.Where(b => b.Competencia.Ano == exercicio).ToList();
        if (doExercicio.Count == 0)
        {
            return 1m; // nenhuma pendencia de gestao no exercicio.
        }

        // Gestao saudavel = beneficios ja DECIDIDOS (sem pendencia em avaliacao).
        var decididos = doExercicio.Count(b => b.Situacao != SituacaoBeneficio.EmAvaliacao);
        return Proporcao(decididos, doExercicio.Count);
    }

    private static decimal Proporcao(int parte, int total)
        => decimal.Round((decimal)parte / total, 4, MidpointRounding.AwayFromZero);
}
