using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>
/// Servico de aplicacao que MONTA a <see cref="MargemConsignavel"/> de um servidor numa competencia,
/// reunindo a base consignavel apurada da folha (<see cref="IBaseConsignavelProvider"/>), os percentuais
/// vigentes por balde (<see cref="IParametrosMargemProvider"/>) e o COMPROMETIDO por balde (soma das
/// parcelas das consignacoes Averbadas vigentes do servidor). Fonte unica de verdade da margem: usado pela
/// consulta de margem, pela averbacao/reativacao e pelo gancho da folha — garantindo o mesmo calculo
/// deterministico em todos os pontos. Opcionalmente IGNORA um contrato (ao reativar/recalcular o proprio).
/// </summary>
public sealed class CalculadoraMargemConsignavel(
    IBaseConsignavelProvider baseConsignavel,
    IParametrosMargemProvider parametrosMargem,
    IContratoConsignacaoRepository contratos)
{
    /// <summary>Calcula a margem do servidor na competencia.</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <param name="ignorarContratoId">Contrato a desconsiderar do comprometido (ex.: ao reativar o proprio).</param>
    /// <returns>Margem consignavel do servidor na competencia.</returns>
    public async Task<MargemConsignavel> CalcularAsync(
        Guid servidorId,
        Competencia competencia,
        CancellationToken cancellationToken,
        ContratoConsignacaoId? ignorarContratoId = null)
    {
        ArgumentNullException.ThrowIfNull(competencia);

        var baseCalculo = await baseConsignavel
            .ApurarBaseAsync(servidorId, competencia, cancellationToken)
            .ConfigureAwait(false);
        var percentuais = await parametrosMargem
            .ObterVigenteAsync(competencia, cancellationToken)
            .ConfigureAwait(false);

        var averbadas = await contratos
            .ListarAverbadasDoServidorAsync(new ServidorId(servidorId), cancellationToken)
            .ConfigureAwait(false);

        var comprometido = new Dictionary<GrupoMargem, decimal>();
        foreach (var contrato in averbadas)
        {
            if (ignorarContratoId is { } ignorar && contrato.Id == ignorar)
            {
                continue;
            }

            comprometido[contrato.GrupoMargem] = comprometido.TryGetValue(contrato.GrupoMargem, out var atual)
                ? atual + contrato.ValorParcela
                : contrato.ValorParcela;
        }

        return MargemConsignavel.Calcular(baseCalculo, percentuais, comprometido);
    }
}
