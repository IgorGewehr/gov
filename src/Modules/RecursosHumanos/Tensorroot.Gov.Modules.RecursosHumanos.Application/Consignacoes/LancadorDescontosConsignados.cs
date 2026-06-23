using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>
/// Desconto consignado a lancar na folha de um servidor, ja resolvido pelo corte por margem.
/// </summary>
/// <param name="ContratoId">Contrato de consignacao de origem.</param>
/// <param name="CodigoRubrica">Codigo da rubrica (S-1010) do desconto consignado.</param>
/// <param name="ValorLancado">Valor efetivamente lancado (apos eventual glosa por margem).</param>
/// <param name="ValorContratado">Valor cheio da parcela contratada.</param>
/// <param name="Grupo">Balde de margem do contrato.</param>
/// <param name="Glosado">Se o valor lancado foi reduzido/zerado por insuficiencia de margem.</param>
public sealed record DescontoConsignadoResolvido(
    Guid ContratoId,
    string CodigoRubrica,
    decimal ValorLancado,
    decimal ValorContratado,
    GrupoMargem Grupo,
    bool Glosado);

/// <summary>
/// GANCHO deterministico das consignacoes na folha (design RH §2.5; analogo a
/// <c>AjustadorProventoPorAfastamento</c>): no fechamento/calculo da folha, para cada servidor com
/// <c>ContratoConsignacao</c> Averbada vigente na competencia, RESOLVE o desconto a lancar
/// RESPEITANDO a margem disponivel apurada da MESMA competencia. Quando a soma dos averbados excede a
/// margem de um balde (ex.: a base caiu por afastamento — a folha lanca proventos ja ajustados), aplica
/// CORTE POR PRIORIDADE no balde (obrigatorias -&gt; facultativas -&gt; beneficio): mantem integralmente as de
/// maior prioridade e glosa as de menor (a ultima cabivel pode entrar parcial). O desconto resultante
/// jamais estoura a margem. O <c>MotorDeCalculoFolha</c> permanece PURO (recebe verbas ja resolvidas).
/// Deterministico e auditavel: a glosa fica sinalizada em <see cref="DescontoConsignadoResolvido.Glosado"/>.
/// </summary>
public sealed class LancadorDescontosConsignados(
    IContratoConsignacaoRepository contratos,
    IBaseConsignavelProvider baseConsignavel,
    IParametrosMargemProvider parametrosMargem)
{
    /// <summary>
    /// Resolve os descontos consignados de um servidor na competencia, com corte por prioridade dentro de
    /// cada balde quando a margem nao comporta a soma dos averbados.
    /// </summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="competencia">Competencia da folha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Descontos resolvidos (somente os com valor &gt; 0 sao lancaveis; glosados sinalizados).</returns>
    public async Task<IReadOnlyList<DescontoConsignadoResolvido>> ResolverAsync(
        Guid servidorId,
        Competencia competencia,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);

        var averbadas = await contratos
            .ListarAverbadasDoServidorAsync(new ServidorId(servidorId), cancellationToken)
            .ConfigureAwait(false);
        if (averbadas.Count == 0)
        {
            return [];
        }

        var baseCalculo = await baseConsignavel.ApurarBaseAsync(servidorId, competencia, cancellationToken).ConfigureAwait(false);
        var percentuais = await parametrosMargem.ObterVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        // Margem SEM comprometido: o limite por balde e o teto; o corte por prioridade consome o teto.
        var margem = MargemConsignavel.Calcular(baseCalculo, percentuais);

        var resolvidos = new List<DescontoConsignadoResolvido>();

        // Um balde de cada vez (reserva legal independente): o cartao nao invade o geral.
        foreach (var grupo in new[] { GrupoMargem.Geral, GrupoMargem.CartaoConsignado, GrupoMargem.CartaoBeneficio })
        {
            var disponivelNoBalde = margem.Limite(grupo);

            // Corte por prioridade: obrigatorias primeiro, depois facultativas, por fim beneficio. Dentro
            // da mesma categoria, ordena por averbacao mais antiga (criterio estavel e deterministico).
            var doBalde = averbadas
                .Where(c => c.GrupoMargem == grupo)
                .OrderBy(c => c.Categoria)
                .ThenBy(c => c.DataAverbacao)
                .ThenBy(c => c.Id.Value);

            foreach (var contrato in doBalde)
            {
                var contratado = contrato.ValorParcela;
                decimal lancar;
                bool glosado;
                if (contratado <= disponivelNoBalde)
                {
                    lancar = contratado;
                    glosado = false;
                }
                else
                {
                    // Cabe parcial (ate o que sobrou) ou nada — glosa registrada para auditoria.
                    lancar = disponivelNoBalde > 0m ? disponivelNoBalde : 0m;
                    glosado = true;
                }

                disponivelNoBalde = decimal.Round(disponivelNoBalde - lancar, 2, MidpointRounding.AwayFromZero);
                if (disponivelNoBalde < 0m)
                {
                    disponivelNoBalde = 0m;
                }

                resolvidos.Add(new DescontoConsignadoResolvido(
                    contrato.Id.Value,
                    contrato.CodigoRubrica,
                    decimal.Round(lancar, 2, MidpointRounding.AwayFromZero),
                    contratado,
                    grupo,
                    glosado));
            }
        }

        return resolvidos;
    }
}
