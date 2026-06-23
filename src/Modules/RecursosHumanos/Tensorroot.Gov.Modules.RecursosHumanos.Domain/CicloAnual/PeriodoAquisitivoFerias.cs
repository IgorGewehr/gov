namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;

/// <summary>
/// Periodo aquisitivo de ferias (VO de consulta, design §1.3): 12 meses de vinculo geram direito a
/// <see cref="DiasDireito"/> dias (default 30). Determina, em relacao a uma data de referencia (ENTRADA,
/// sem relogio), se o periodo esta vencido, em dobra (concessivo expirado — CLT art. 137) ou apenas
/// proporcional. A dobra para estatutario depende do estatuto — // TODO(validar-oficial: Estatuto 327/2008).
/// </summary>
/// <param name="Inicio">Inicio do periodo aquisitivo (data-base: exercicio do servidor).</param>
/// <param name="Fim">Fim do periodo aquisitivo (Inicio + 12 meses - 1 dia, em regra).</param>
/// <param name="DiasDireito">Dias de direito do periodo (default 30; reduzivel por faltas — parametrizavel).</param>
/// <param name="DiasJaGozados">Dias ja gozados deste periodo.</param>
/// <param name="DiasVendidosAbono">Dias convertidos em abono pecuniario deste periodo.</param>
public sealed record PeriodoAquisitivoFerias(
    DateOnly Inicio,
    DateOnly Fim,
    int DiasDireito,
    int DiasJaGozados,
    int DiasVendidosAbono)
{
    /// <summary>Dias de direito padrao de um periodo aquisitivo completo (CLT art. 130).</summary>
    public const int DiasDireitoPadrao = 30;

    /// <summary>Dias ainda nao gozados nem vendidos deste periodo (nao-negativo).</summary>
    public int DiasRemanescentes => Math.Max(0, DiasDireito - DiasJaGozados - DiasVendidosAbono);

    /// <summary>Indica se o periodo aquisitivo ja se completou ate a data de referencia (vencido).</summary>
    /// <param name="dataReferencia">Data de referencia (entrada).</param>
    /// <returns><c>true</c> se a data de referencia for igual/posterior ao <see cref="Fim"/>.</returns>
    public bool EstaVencido(DateOnly dataReferencia) => dataReferencia >= Fim;

    /// <summary>
    /// Indica se o periodo deve ser pago em DOBRA: concessivo (12 meses apos o <see cref="Fim"/>) expirado
    /// sem gozo das ferias (CLT art. 137). // TODO(validar-oficial): dobra para estatutario (Estatuto 327/2008).
    /// </summary>
    /// <param name="dataReferencia">Data de referencia (entrada).</param>
    /// <returns><c>true</c> se o concessivo expirou e ainda ha dias remanescentes.</returns>
    public bool EmDobra(DateOnly dataReferencia)
        => dataReferencia > Fim.AddMonths(12) && DiasRemanescentes > 0;

    /// <summary>
    /// Avos do periodo proporcional ate a data de referencia (regra dos 15 dias), considerando
    /// afastamentos que suspendem a contagem. Reusa <see cref="Avos.Apurar"/> (funcao pura).
    /// </summary>
    /// <param name="dataReferencia">Fim do recorte (ex.: data de desligamento).</param>
    /// <param name="afastamentos">Afastamentos do periodo (entrada).</param>
    /// <returns>Avos proporcionais (0..12).</returns>
    public Avos VencidoOuProporcional(DateOnly dataReferencia, IReadOnlyList<IntervaloAfastamento> afastamentos)
    {
        var fim = dataReferencia < Fim ? dataReferencia : Fim;
        return Avos.Apurar(Inicio, fim, afastamentos);
    }
}
