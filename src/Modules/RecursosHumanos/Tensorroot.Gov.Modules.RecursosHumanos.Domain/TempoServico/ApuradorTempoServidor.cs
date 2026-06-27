namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

/// <summary>
/// Intervalo nao-computavel (licenca sem contagem de tempo) que se ABATE do tempo de efetivo exercicio: par
/// fechado <c>[Inicio, Fim]</c>. Espelha os afastamentos com <c>ContaTempo=false</c> do servidor (a borda os
/// fornece a partir do agregado <c>Afastamento</c>). Imutavel.
/// </summary>
/// <param name="Inicio">Inicio do periodo nao-computavel (inclusivo).</param>
/// <param name="Fim">Fim do periodo nao-computavel (inclusivo).</param>
public readonly record struct IntervaloNaoComputavel(DateOnly Inicio, DateOnly Fim);

/// <summary>
/// Servico de DOMINIO (puro, sem relogio/IO) que apura o periodo de EFETIVO EXERCICIO do servidor para a
/// certidao de tempo: a partir do inicio de exercicio ate a data-base, abatendo os dias nao-computaveis
/// (licencas sem contagem de tempo) que se sobrepoem ao intervalo. A "data de hoje" e os afastamentos sao
/// resolvidos na borda (Application) e PASSADOS para ca — o dominio nao le relogio nem banco (CLAUDE.md S7).
/// </summary>
public static class ApuradorTempoServidor
{
    /// <summary>
    /// Apura o periodo de efetivo exercicio <c>[inicioExercicio .. dataBase]</c> do servidor, ja descontando
    /// os dias nao-computaveis sobrepostos. O resultado e' um <see cref="PeriodoTempo"/> de natureza
    /// <see cref="NaturezaPeriodo.EfetivoExercicio"/> com fator comum (a conversao especial->comum, quando
    /// aplicavel, e' modelada por periodos averbados/especiais informados a parte).
    /// </summary>
    /// <param name="inicioExercicio">Data de inicio do efetivo exercicio do servidor.</param>
    /// <param name="dataBase">Data-base da apuracao (inclusiva; tipicamente o "hoje" do tenant ou a data de desligamento).</param>
    /// <param name="naoComputaveis">Periodos nao-computaveis a abater (licencas sem contagem de tempo).</param>
    /// <param name="observacao">Observacao do periodo (opcional).</param>
    /// <returns>Periodo de efetivo exercicio apurado.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="naoComputaveis"/> for nulo.</exception>
    /// <exception cref="CertidaoTempoServicoException">Se a data-base for anterior ao inicio do exercicio.</exception>
    public static PeriodoTempo ApurarEfetivoExercicio(
        DateOnly inicioExercicio,
        DateOnly dataBase,
        IEnumerable<IntervaloNaoComputavel> naoComputaveis,
        string? observacao = null)
    {
        ArgumentNullException.ThrowIfNull(naoComputaveis);
        if (dataBase < inicioExercicio)
        {
            throw new CertidaoTempoServicoException("Data-base da apuracao nao pode ser anterior ao inicio do exercicio.");
        }

        var diasNaoComputaveis = ContarDiasNaoComputaveis(inicioExercicio, dataBase, naoComputaveis);
        return PeriodoTempo.EfetivoExercicio(inicioExercicio, dataBase, diasNaoComputaveis, observacao: observacao);
    }

    /// <summary>
    /// Conta os dias UNICOS nao-computaveis dentro do intervalo <c>[inicio, fim]</c> (uniao dos periodos, sem
    /// dupla contagem de sobreposicoes entre eles), recortados pelos limites do intervalo de apuracao.
    /// </summary>
    /// <param name="inicio">Inicio do intervalo de apuracao (inclusivo).</param>
    /// <param name="fim">Fim do intervalo de apuracao (inclusivo).</param>
    /// <param name="naoComputaveis">Periodos nao-computaveis (podem se sobrepor entre si).</param>
    /// <returns>Quantidade de dias unicos nao-computaveis dentro do intervalo.</returns>
    public static int ContarDiasNaoComputaveis(
        DateOnly inicio,
        DateOnly fim,
        IEnumerable<IntervaloNaoComputavel> naoComputaveis)
    {
        ArgumentNullException.ThrowIfNull(naoComputaveis);

        // Recorta cada periodo aos limites do intervalo e descarta os vazios; ordena por inicio.
        var recortados = naoComputaveis
            .Select(periodo => new IntervaloNaoComputavel(
                periodo.Inicio < inicio ? inicio : periodo.Inicio,
                periodo.Fim > fim ? fim : periodo.Fim))
            .Where(periodo => periodo.Fim >= periodo.Inicio)
            .OrderBy(periodo => periodo.Inicio)
            .ThenBy(periodo => periodo.Fim)
            .ToList();

        if (recortados.Count == 0)
        {
            return 0;
        }

        // Funde os intervalos sobrepostos/adjacentes e soma os dias da UNIAO (evita dupla contagem — um
        // mesmo dia coberto por duas licencas conta uma vez).
        var total = 0;
        var atualInicio = recortados[0].Inicio;
        var atualFim = recortados[0].Fim;

        for (var indice = 1; indice < recortados.Count; indice++)
        {
            var proximo = recortados[indice];

            // Sobreposicao ou adjacencia (proximo comeca ate 1 dia apos o fim atual) -> estende o bloco.
            if (proximo.Inicio.DayNumber <= atualFim.DayNumber + 1)
            {
                if (proximo.Fim > atualFim)
                {
                    atualFim = proximo.Fim;
                }
            }
            else
            {
                total += atualFim.DayNumber - atualInicio.DayNumber + 1;
                atualInicio = proximo.Inicio;
                atualFim = proximo.Fim;
            }
        }

        total += atualFim.DayNumber - atualInicio.DayNumber + 1;
        return total;
    }
}
