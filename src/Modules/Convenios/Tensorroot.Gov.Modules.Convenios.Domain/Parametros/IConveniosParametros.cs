using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Parametros;

/// <summary>
/// Parametro de prazo do tenant: quantidade + unidade (uteis/corridos) + norma-fonte citavel. Resolve um
/// <see cref="PrazoLegal"/> via calendario do tenant (prazo CALCULADO, nunca digitado — CLAUDE.md S7/S16).
/// </summary>
/// <param name="Quantidade">Quantidade de dias.</param>
/// <param name="Unidade">Unidade de contagem (uteis/corridos).</param>
/// <param name="NormaFonte">Norma-fonte citavel (ex.: "Lei 13.019/2014 art. 71").</param>
public readonly record struct ParametroPrazo(int Quantidade, UnidadePrazo Unidade, string NormaFonte)
{
    /// <summary>Resolve o <see cref="PrazoLegal"/> a partir de <paramref name="inicio"/>.</summary>
    /// <param name="inicio">Data base do prazo.</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <returns>Prazo com o vencimento resolvido.</returns>
    public PrazoLegal Resolver(DateOnly inicio, ICalendarioDiasUteis calendario)
        => PrazoLegal.Criar(inicio, Quantidade, Unidade, NormaFonte, calendario);
}

/// <summary>
/// Parametro de percentual do tenant (ex.: contrapartida minima): percentual + norma-fonte citavel.
/// </summary>
/// <param name="Percentual">Percentual (ex.: 20m para 20%).</param>
/// <param name="NormaFonte">Norma-fonte citavel.</param>
public readonly record struct ParametroPercentual(decimal Percentual, string NormaFonte);

/// <summary>
/// Parametro do indice de devolucao (Selic) do tenant: dia de inicio da contagem + percentual acumulado +
/// norma-fonte. A Infra apura o percentual da tabela mensal por exercicio; o dominio so o recebe.
/// </summary>
/// <param name="DiaInicioContagem">Dia, a partir do vencimento, em que a atualizacao incide (ex.: 30).</param>
/// <param name="PercentualAcumulado">Percentual acumulado do indice no periodo.</param>
/// <param name="NormaFonte">Norma-fonte citavel.</param>
public readonly record struct ParametroIndiceDevolucao(int DiaInicioContagem, decimal PercentualAcumulado, string NormaFonte)
{
    /// <summary>Constroi o VO <see cref="IndiceDevolucao"/> a partir do parametro.</summary>
    /// <returns>Indice de devolucao do tenant.</returns>
    public IndiceDevolucao Resolver() => IndiceDevolucao.Criar(DiaInicioContagem, PercentualAcumulado, NormaFonte);
}

/// <summary>
/// Porta de DOMINIO dos parametros de Convenios por tenant: prazos de analise/saneamento/entrega (fluxos A e
/// B), percentual minimo de contrapartida e indice de devolucao (Selic). A implementacao (Infra) le de
/// config/tabela por tenant. NENHUM literal numerico (20%, 60/180, 90+30, 150, 45, Selic/30o dia) vive no
/// dominio: todos chegam por esta porta, com norma-fonte. CLAUDE.md S7/S16.
/// </summary>
public interface IConveniosParametros
{
    // ===== Fluxo A — Convenios federais RECEBIDOS =====

    /// <summary>Prazo de analise da PC PARCIAL (default 60 dias — Dec. 11.531/2023).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Parametro de prazo.</returns>
    ParametroPrazo PrazoAnalisePcParcial(Guid tenantId);

    /// <summary>Prazo de analise da PC FINAL (default 180 dias — Dec. 11.531/2023).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Parametro de prazo.</returns>
    ParametroPrazo PrazoAnalisePcFinal(Guid tenantId);

    /// <summary>Percentual minimo de contrapartida (default 20% — Portaria Conjunta 33/2023).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Parametro de percentual.</returns>
    ParametroPercentual PercentualContrapartidaMinimo(Guid tenantId);

    // ===== Fluxo B — Parcerias-saida OSC (MROSC) =====

    /// <summary>Prazo de entrega da PC pela OSC (default 90 dias — Lei 13.019 art. 69).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Parametro de prazo.</returns>
    ParametroPrazo PrazoEntregaPcOsc(Guid tenantId);

    /// <summary>Prorrogacao do prazo de entrega da PC pela OSC (default 30 dias, uma vez).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Parametro de prazo.</returns>
    ParametroPrazo PrazoProrrogacaoPcOsc(Guid tenantId);

    /// <summary>Prazo de analise da PC da OSC (default 150 dias — Lei 13.019 art. 71).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Parametro de prazo.</returns>
    ParametroPrazo PrazoAnalisePcOsc(Guid tenantId);

    // ===== Transversal aos dois fluxos =====

    /// <summary>Prazo de saneamento de pendencias (default 45 dias — ambos os fluxos).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Parametro de prazo.</returns>
    ParametroPrazo PrazoSaneamento(Guid tenantId);

    /// <summary>Indice de devolucao (Selic) vigente para o exercicio (default: Selic a partir do 30o dia).</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="exercicio">Ano de referencia da atualizacao.</param>
    /// <returns>Parametro do indice de devolucao.</returns>
    ParametroIndiceDevolucao IndiceDevolucao(Guid tenantId, int exercicio);
}
