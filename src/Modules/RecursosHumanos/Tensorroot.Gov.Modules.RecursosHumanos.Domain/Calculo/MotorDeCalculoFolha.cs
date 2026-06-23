using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;

/// <summary>
/// Servico de dominio (puro, sem I/O) que apura, para um servidor numa competencia, as bases e os
/// descontos legais da folha — INSS/RPPS progressivo e IRRF (sobre base apos previdencia, dependentes
/// e pensao, regra do mais vantajoso) — e o liquido, usando exclusivamente as TABELAS PARAMETRIZADAS
/// por tenant/competencia (nunca aliquotas hardcoded — CLAUDE.md S7/S16). Deterministico e auditavel:
/// dadas as mesmas entradas e tabelas, produz sempre o mesmo resultado.
/// </summary>
public static class MotorDeCalculoFolha
{
    /// <summary>
    /// Calcula proventos, descontos legais e liquido de um servidor.
    /// </summary>
    /// <param name="insumos">Verbas, regime, dependentes e pensao do servidor.</param>
    /// <param name="tabelaInss">Tabela INSS vigente (RGPS); obrigatoria para servidor RGPS.</param>
    /// <param name="tabelaIrrf">Tabela IRRF vigente; obrigatoria.</param>
    /// <param name="tabelaRpps">Tabela RPPS municipal vigente; obrigatoria para servidor efetivo (fail-closed).</param>
    /// <returns>Resultado deterministico do calculo do servidor.</returns>
    /// <exception cref="ArgumentNullException">Se insumos ou tabela IRRF forem nulos.</exception>
    /// <exception cref="CalculoFolhaException">Se faltar a tabela exigida pelo regime (INSS para RGPS, RPPS para efetivo).</exception>
    public static ResultadoCalculoServidor Calcular(
        InsumosCalculoServidor insumos,
        TabelaInss? tabelaInss,
        TabelaIrrf tabelaIrrf,
        TabelaRpps? tabelaRpps)
        => Apurar(insumos, tabelaInss, tabelaIrrf, tabelaRpps, OpcoesIrrf.Mensal);

    /// <summary>
    /// Calcula proventos e descontos legais de uma verba de BASE SEPARADA da folha mensal — o caso do
    /// 13o salario (gratificacao natalina): INSS/RPPS e IRRF do 13o sao apurados ISOLADAMENTE, sobre a
    /// propria base, com as MESMAS tabelas da competencia, mas sem somar ao mes (Lei 7.713/88 art. 12-A).
    /// As <paramref name="opcoes"/> permitem vedar o desconto simplificado do IRRF (regra do 13o). Reusa
    /// exatamente o mesmo laco de bases por incidencia e a previdencia fail-closed do calculo mensal —
    /// nao ha segundo motor (design §2.2). Deterministico.
    /// </summary>
    /// <param name="insumos">Verbas do 13o (com flags de incidencia), regime, dependentes e pensao.</param>
    /// <param name="tabelaInss">Tabela INSS vigente; obrigatoria para servidor RGPS.</param>
    /// <param name="tabelaIrrf">Tabela IRRF vigente; obrigatoria.</param>
    /// <param name="tabelaRpps">Tabela RPPS municipal vigente; obrigatoria para servidor efetivo (fail-closed).</param>
    /// <param name="opcoes">Opcoes do IRRF (ex.: <see cref="OpcoesIrrf.DecimoTerceiro"/> veda o simplificado).</param>
    /// <returns>Resultado deterministico do calculo da base separada.</returns>
    public static ResultadoCalculoServidor CalcularBaseSeparada(
        InsumosCalculoServidor insumos,
        TabelaInss? tabelaInss,
        TabelaIrrf tabelaIrrf,
        TabelaRpps? tabelaRpps,
        OpcoesIrrf opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        return Apurar(insumos, tabelaInss, tabelaIrrf, tabelaRpps, opcoes);
    }

    private static ResultadoCalculoServidor Apurar(
        InsumosCalculoServidor insumos,
        TabelaInss? tabelaInss,
        TabelaIrrf tabelaIrrf,
        TabelaRpps? tabelaRpps,
        OpcoesIrrf opcoes)
    {
        ArgumentNullException.ThrowIfNull(insumos);
        ArgumentNullException.ThrowIfNull(tabelaIrrf);

        var totalProventos = 0m;
        var baseInss = 0m;
        var baseRpps = 0m;
        var baseIrrf = 0m;
        var outrosDescontos = 0m;

        foreach (var verba in insumos.Verbas)
        {
            if (verba.EhProvento)
            {
                totalProventos += verba.Valor;
                if (verba.IncideInss)
                {
                    baseInss += verba.Valor;
                }

                if (verba.IncideRpps)
                {
                    baseRpps += verba.Valor;
                }

                if (verba.IncideIrrf)
                {
                    baseIrrf += verba.Valor;
                }
            }
            else
            {
                // Descontos manuais (consignados, pensao via rubrica, faltas) entram em "outros".
                // INSS/RPPS/IRRF sao apurados pelo motor — nao devem ser lancados manualmente.
                outrosDescontos += verba.Valor;
            }
        }

        // Previdencia: depende do regime, fail-closed se a tabela exigida nao estiver carregada.
        decimal descontoInss;
        decimal descontoRpps;
        if (insumos.Regime == RegimePrevidenciario.Rpps)
        {
            if (tabelaRpps is null)
            {
                throw new CalculoFolhaException(
                    "Calculo de servidor RPPS impedido: tabela RPPS municipal nao carregada para a competencia (fail-closed — EC 103/2019; lei municipal obrigatoria).");
            }

            descontoRpps = tabelaRpps.CalcularContribuicao(baseRpps);
            descontoInss = 0m;
        }
        else
        {
            if (tabelaInss is null)
            {
                throw new CalculoFolhaException(
                    "Calculo de servidor RGPS impedido: tabela INSS nao carregada para a competencia (fail-closed).");
            }

            descontoInss = tabelaInss.CalcularContribuicao(baseInss);
            descontoRpps = 0m;
        }

        var descontoPrevidenciario = descontoInss + descontoRpps;

        // IRRF: base apos previdencia, dependentes e pensao (regra do mais vantajoso na tabela). No 13o
        // (base separada), opcoes.AplicarSimplificado=false veda o desconto simplificado (art. 12-A).
        var descontoIrrf = tabelaIrrf.CalcularImposto(
            baseIrrf,
            descontoPrevidenciario,
            insumos.QuantidadeDependentes,
            insumos.PensaoAlimenticia,
            opcoes.AplicarSimplificado);

        var totalDescontos = descontoPrevidenciario + descontoIrrf + outrosDescontos;
        // P0-5: o liquido do RESULTADO do motor e nao-negativo (piso do VO LiquidoAPagar), mas a deteccao
        // de liquido INSUFICIENTE (descontos >= proventos) e responsabilidade do agregado FolhaDePagamento
        // (flag/evento de conferencia em Calcular) — onde estao TODOS os descontos do servidor, inclusive
        // os manuais (consignados) que nao passam pelo motor. Aqui nao se zera nada em silencio.
        var liquido = Math.Max(0m, totalProventos - totalDescontos);

        return new ResultadoCalculoServidor(
            insumos.ServidorId,
            decimal.Round(totalProventos, 2, MidpointRounding.AwayFromZero),
            decimal.Round(baseInss, 2, MidpointRounding.AwayFromZero),
            descontoInss,
            decimal.Round(baseRpps, 2, MidpointRounding.AwayFromZero),
            descontoRpps,
            decimal.Round(baseIrrf, 2, MidpointRounding.AwayFromZero),
            descontoIrrf,
            decimal.Round(outrosDescontos, 2, MidpointRounding.AwayFromZero),
            decimal.Round(totalDescontos, 2, MidpointRounding.AwayFromZero),
            decimal.Round(liquido, 2, MidpointRounding.AwayFromZero));
    }
}
