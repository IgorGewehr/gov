using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

/// <summary>
/// Plano de destinacao calculado para um processo: as datas das tres fases (corrente, intermediaria)
/// e a destinacao final, com a data de aptidao a eliminacao (nula em guarda permanente).
/// </summary>
/// <param name="FimGuardaCorrente">Fim da fase corrente (= eventoBase + corrente).</param>
/// <param name="FimGuardaIntermediaria">Fim da fase intermediaria (= corrente + intermediaria).</param>
/// <param name="Destinacao">Destinacao final (eliminacao/guarda permanente).</param>
/// <param name="DataAptidaoEliminacao">Data a partir da qual a eliminacao e permitida (nula em guarda permanente).</param>
public sealed record PlanoDeDestinacao(
    DateOnly FimGuardaCorrente,
    DateOnly FimGuardaIntermediaria,
    Destinacao Destinacao,
    DateOnly? DataAptidaoEliminacao);

/// <summary>
/// Motor de temporalidade/destinacao (e-ARQ v2 / CONARQ): calculo DETERMINISTICO e LOCAL (sem I/O)
/// das tres fases do ciclo de guarda a partir da <see cref="RegraTemporalidade"/> do tenant. I-T6:
/// prazos/destinacao vem SEMPRE da regra (zero hardcode).
/// </summary>
public interface IMotorTemporalidade
{
    /// <summary>Calcula o plano de destinacao para uma regra e um evento base.</summary>
    /// <param name="regra">Regra de temporalidade da TTD do tenant (prazos + destinacao + evento).</param>
    /// <param name="eventoBase">Data base da contagem (resolvida do <see cref="EventoContagem"/>).</param>
    /// <returns>Plano de destinacao com as datas calculadas.</returns>
    PlanoDeDestinacao Calcular(RegraTemporalidade regra, DateOnly eventoBase);
}
