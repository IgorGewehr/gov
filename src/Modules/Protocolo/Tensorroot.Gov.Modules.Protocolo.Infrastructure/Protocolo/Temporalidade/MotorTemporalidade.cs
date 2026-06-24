using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Temporalidade;

/// <summary>
/// Motor de temporalidade/destinacao (e-ARQ v2 / CONARQ): calculo DETERMINISTICO e LOCAL (sem I/O) das
/// tres fases do ciclo de guarda. Soma anos sobre o evento base conforme a
/// <see cref="RegraTemporalidade"/> do tenant — zero hardcode (I-T6): TODO prazo vem da regra.
/// <para>
/// Fase corrente termina em <c>eventoBase + corrente</c>; intermediaria em
/// <c>eventoBase + corrente + intermediaria</c>; a aptidao a eliminacao = fim da intermediaria
/// (somente quando a destinacao for <see cref="Destinacao.Eliminacao"/>; em guarda permanente e nula).
/// </para>
/// </summary>
public sealed class MotorTemporalidade : IMotorTemporalidade
{
    /// <inheritdoc />
    public PlanoDeDestinacao Calcular(RegraTemporalidade regra, DateOnly eventoBase)
    {
        ArgumentNullException.ThrowIfNull(regra);

        var fimCorrente = eventoBase.AddYears(regra.PrazoGuardaCorrenteAnos);
        var fimIntermediaria = fimCorrente.AddYears(regra.PrazoGuardaIntermediariaAnos);

        var aptidao = regra.DestinacaoFinal == Destinacao.Eliminacao
            ? fimIntermediaria
            : (DateOnly?)null;

        return new PlanoDeDestinacao(fimCorrente, fimIntermediaria, regra.DestinacaoFinal, aptidao);
    }
}
