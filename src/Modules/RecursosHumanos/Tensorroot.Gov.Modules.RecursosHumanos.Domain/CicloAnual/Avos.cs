namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;

/// <summary>
/// Intervalo de afastamento usado na apuracao de avos. <see cref="SuspendeContagem"/> indica se o
/// afastamento SUSPENDE a contagem do mes (depende da natureza legal do afastamento — para estatutario,
/// regra do Estatuto 327/2008). // TODO(validar-oficial): quais afastamentos suspendem avos no estatuto.
/// </summary>
/// <param name="Inicio">Inicio do afastamento (inclusivo).</param>
/// <param name="Fim">Fim do afastamento (inclusivo); <c>null</c> = ainda em curso (ate o fim do periodo apurado).</param>
/// <param name="SuspendeContagem">Verdadeiro se o afastamento descaracteriza o efetivo exercicio do(s) mes(es) atingido(s).</param>
public readonly record struct IntervaloAfastamento(DateOnly Inicio, DateOnly? Fim, bool SuspendeContagem);

/// <summary>
/// Avos (meses-de-direito) de um periodo, regra dos 15 dias da Lei 4.090/62: cada mes do periodo em que
/// houve ao menos <see cref="DiasMinimosNoMes"/> dias de vinculo/efetivo exercicio conta como UM avo
/// (0..12). Value Object PURO e deterministico: nao consulta relogio — admissao, fim do periodo
/// (31/12 no ciclo, ou data de desligamento na rescisao) e afastamentos sao ENTRADA (design §1.2).
/// </summary>
/// <param name="Quantidade">Quantidade de avos apurada (0 a 12).</param>
public sealed record Avos(int Quantidade)
{
    /// <summary>Dias minimos de exercicio no mes para contar o avo (Lei 4.090/62: fracao &gt;= 15 dias).</summary>
    public const int DiasMinimosNoMes = 15;

    /// <summary>Zero avos.</summary>
    public static Avos Zero { get; } = new(0);

    /// <summary>
    /// Apura os avos de um periodo (inclusivo em ambas as pontas) aplicando a regra dos 15 dias por mes,
    /// descontando os dias dos afastamentos que suspendem a contagem. Funcao pura de datas de entrada.
    /// </summary>
    /// <param name="inicioPeriodo">Inicio do periodo apurado (ex.: 01/01 do ano, ou admissao se posterior).</param>
    /// <param name="fimPeriodo">Fim do periodo apurado (ex.: 31/12, ou data de desligamento na rescisao).</param>
    /// <param name="afastamentosQueSuspendem">Afastamentos do periodo; so os com <c>SuspendeContagem=true</c> reduzem dias.</param>
    /// <returns>Avos (0..12) do periodo.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="fimPeriodo"/> for anterior a <paramref name="inicioPeriodo"/>.</exception>
    public static Avos Apurar(
        DateOnly inicioPeriodo,
        DateOnly fimPeriodo,
        IReadOnlyList<IntervaloAfastamento> afastamentosQueSuspendem)
    {
        ArgumentNullException.ThrowIfNull(afastamentosQueSuspendem);
        if (fimPeriodo < inicioPeriodo)
        {
            throw new ArgumentOutOfRangeException(nameof(fimPeriodo), "Fim do periodo nao pode ser anterior ao inicio.");
        }

        var avos = 0;
        var mes = new DateOnly(inicioPeriodo.Year, inicioPeriodo.Month, 1);
        var ultimoMes = new DateOnly(fimPeriodo.Year, fimPeriodo.Month, 1);

        while (mes <= ultimoMes)
        {
            var primeiroDiaMes = mes;
            var ultimoDiaMes = mes.AddMonths(1).AddDays(-1);

            // Recorte do mes dentro do periodo apurado.
            var inicioEfetivo = inicioPeriodo > primeiroDiaMes ? inicioPeriodo : primeiroDiaMes;
            var fimEfetivo = fimPeriodo < ultimoDiaMes ? fimPeriodo : ultimoDiaMes;

            var diasTrabalhados = fimEfetivo.DayNumber - inicioEfetivo.DayNumber + 1;

            // Subtrai os dias de afastamentos que suspendem a contagem, recortados ao mes efetivo.
            foreach (var afastamento in afastamentosQueSuspendem)
            {
                if (!afastamento.SuspendeContagem)
                {
                    continue;
                }

                var afInicio = afastamento.Inicio > inicioEfetivo ? afastamento.Inicio : inicioEfetivo;
                var afFimBruto = afastamento.Fim ?? fimEfetivo;
                var afFim = afFimBruto < fimEfetivo ? afFimBruto : fimEfetivo;
                if (afFim >= afInicio)
                {
                    diasTrabalhados -= afFim.DayNumber - afInicio.DayNumber + 1;
                }
            }

            if (diasTrabalhados >= DiasMinimosNoMes)
            {
                avos++;
            }

            mes = mes.AddMonths(1);
        }

        return new Avos(avos);
    }
}
