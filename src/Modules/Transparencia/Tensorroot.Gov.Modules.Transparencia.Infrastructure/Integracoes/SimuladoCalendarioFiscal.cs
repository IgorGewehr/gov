using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Calendario fiscal simulado (dev/demonstracao): deriva a data-limite por tipo de declaracao a partir
/// do periodo informado (MSC: fim do mes subsequente; RREO: 30 dias apos o bimestre; RGF: 30 dias apos
/// o quadrimestre; DCA: 30/04 do exercicio seguinte). A implementacao real le o calendario STN
/// parametrizado por tenant (I-11; LRF art. 23 paragrafo 3).
/// </summary>
public sealed class SimuladoCalendarioFiscal : ICalendarioFiscal
{
    /// <inheritdoc />
    public Task<DateOnly> DerivarDataLimiteAsync(
        TipoDeclaracaoFiscal tipo,
        int exercicio,
        int? mes,
        int? numeroBimestre,
        int? numeroQuadrimestre,
        CancellationToken cancellationToken)
    {
        var dataLimite = tipo switch
        {
            TipoDeclaracaoFiscal.Msc => UltimoDiaDoMesSubsequente(exercicio, mes ?? 12),
            TipoDeclaracaoFiscal.Rreo => FimDoPeriodo(exercicio, (numeroBimestre ?? 1) * 2).AddDays(30),
            TipoDeclaracaoFiscal.Rgf => FimDoPeriodo(exercicio, (numeroQuadrimestre ?? 1) * 4).AddDays(30),
            _ => new DateOnly(exercicio + 1, 4, 30),
        };

        return Task.FromResult(dataLimite);
    }

    private static DateOnly UltimoDiaDoMesSubsequente(int exercicio, int mes)
        => new DateOnly(exercicio, Math.Clamp(mes, 1, 12), 1).AddMonths(2).AddDays(-1);

    private static DateOnly FimDoPeriodo(int exercicio, int mesFinal)
    {
        var mes = Math.Clamp(mesFinal, 1, 12);
        return new DateOnly(exercicio, mes, 1).AddMonths(1).AddDays(-1);
    }
}
