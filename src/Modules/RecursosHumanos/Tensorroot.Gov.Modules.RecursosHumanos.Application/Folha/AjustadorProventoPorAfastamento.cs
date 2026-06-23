using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>
/// GANCHO determinista do afastamento na folha (design RH §3.3): antes de lancar um PROVENTO de um
/// servidor numa competencia, consulta os afastamentos VIGENTES e aplica o pior/aplicavel
/// <see cref="EfeitoFolhaAfastamento"/> ao valor do provento-base (suspende, reduz por percentual ou
/// proporcionaliza por dias). O <c>MotorDeCalculoFolha</c> permanece PURO — recebe a verba ja ajustada.
/// Quando ha mais de um afastamento incidente, combina o EFEITO MAIS RESTRITIVO (menor provento
/// resultante), garantindo que a folha jamais pague acima do devido. Deterministico e auditavel.
/// </summary>
public sealed class AjustadorProventoPorAfastamento(IAfastamentoRepository afastamentos)
{
    /// <summary>
    /// Ajusta o valor de um provento-base pelo efeito dos afastamentos vigentes do servidor na competencia.
    /// Descontos NAO sao ajustados (retorna o valor cru). Sem afastamento incidente, retorna o valor original.
    /// </summary>
    /// <param name="servidorId">Servidor do lancamento.</param>
    /// <param name="competencia">Competencia da folha.</param>
    /// <param name="ehProvento"><c>true</c> se a verba e provento (so proventos sao ajustados).</param>
    /// <param name="valorBase">Valor cheio do provento-base.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Valor do provento ajustado pelo(s) afastamento(s) vigente(s), nunca negativo.</returns>
    public async Task<decimal> AjustarProventoAsync(
        Guid servidorId,
        Competencia competencia,
        bool ehProvento,
        decimal valorBase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);

        if (!ehProvento || valorBase <= 0m)
        {
            return valorBase;
        }

        var incidentes = await afastamentos
            .ListarVigentesNaCompetenciaAsync(servidorId, competencia, cancellationToken)
            .ConfigureAwait(false);
        if (incidentes.Count == 0)
        {
            return valorBase;
        }

        // Efeito MAIS RESTRITIVO: aplica cada efeito ao valor-base e fica com o MENOR resultado
        // (o que mais reduz o provento). Cobre o caso de afastamentos sobrepostos no mes.
        var ajustado = valorBase;
        foreach (var afastamento in incidentes)
        {
            var efeito = afastamento.EfeitoNa(competencia);
            if (!efeito.AfetaFolha)
            {
                continue;
            }

            var candidato = efeito.AplicarAoProvento(valorBase);
            if (candidato < ajustado)
            {
                ajustado = candidato;
            }
        }

        return ajustado;
    }
}
