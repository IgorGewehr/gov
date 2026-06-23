namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;

/// <summary>
/// Verba rescisoria componente: identifica QUAL verba (por simbolo logico) deve ser lancada na folha de
/// rescisao. O codigo de rubrica concreto e o calculo do valor sao resolvidos pelo handler a partir do
/// catalogo parametrizavel (design §4.2). Imutavel.
/// </summary>
public enum VerbaRescisoria
{
    /// <summary>Saldo de salario (dias trabalhados no mes do desligamento).</summary>
    SaldoSalario = 1,

    /// <summary>13o proporcional (avos do ano ate o desligamento), base separada.</summary>
    DecimoTerceiroProporcional = 2,

    /// <summary>Ferias vencidas + 1/3 (periodos completos nao gozados), indenizadas.</summary>
    FeriasVencidas = 3,

    /// <summary>Ferias proporcionais + 1/3 (avos do periodo em curso), indenizadas.</summary>
    FeriasProporcionais = 4,

    /// <summary>Aviso previo (Lei 12.506/2011); so celetista.</summary>
    AvisoPrevio = 5,

    /// <summary>Multa de 40% do FGTS; so celetista, dispensa sem justa causa.</summary>
    MultaFgts = 6,
}

/// <summary>
/// Regra da matriz parametrizavel (tipo de desligamento x regime -> verba devida). Carregada como DADO
/// (design §4.2): nenhum <c>if</c> magico — a decisao "verba devida?" vem da matriz, nao de codigo.
/// </summary>
/// <param name="Tipo">Tipo de desligamento.</param>
/// <param name="Regime">Regime juridico do vinculo.</param>
/// <param name="Verba">Verba rescisoria.</param>
/// <param name="Devida">Se a verba e devida para o par (tipo, regime).</param>
public sealed record RegraVerbaRescisoria(
    TipoDesligamento Tipo,
    RegimeVinculo Regime,
    VerbaRescisoria Verba,
    bool Devida);

/// <summary>
/// Compositor PURO da rescisao: dado o tipo de desligamento e o regime, decide quais verbas rescisorias
/// sao devidas a partir de uma MATRIZ PARAMETRIZAVEL (design §4.2 / pesquisa §3.2). O default codifica a
/// tabela da pesquisa; um tenant pode sobrescrever passando a propria matriz. Nada de calculo de valor
/// aqui (isso e do handler, via catalogo de rubricas e calculadoras) — so a composicao.
/// </summary>
public static class CompositorRescisao
{
    /// <summary>
    /// Matriz default (pesquisa §3.2): mapeia, por tipo de desligamento, as verbas devidas e se exigem
    /// regime celetista (FGTS/aviso). Estatutario nunca tem aviso/multa (design §4.1). // TODO(validar-oficial):
    /// distrato pela metade; verbas finas do Estatuto 327/2008 (licenca-premio indenizavel, dobra).
    /// </summary>
    private static readonly Dictionary<TipoDesligamento, VerbasDevidas> MatrizDefault =
        new()
        {
            [TipoDesligamento.DispensaSemJustaCausa] = new(Saldo: true, Decimo: true, FeriasVenc: true, FeriasProp: true, Aviso: true, Multa: true),
            [TipoDesligamento.PedidoDemissaoExoneracao] = new(Saldo: true, Decimo: true, FeriasVenc: true, FeriasProp: true, Aviso: false, Multa: false),
            [TipoDesligamento.JustaCausa] = new(Saldo: true, Decimo: false, FeriasVenc: true, FeriasProp: false, Aviso: false, Multa: false),
            [TipoDesligamento.Distrato] = new(Saldo: true, Decimo: true, FeriasVenc: true, FeriasProp: true, Aviso: true, Multa: true),
            [TipoDesligamento.Aposentadoria] = new(Saldo: true, Decimo: true, FeriasVenc: true, FeriasProp: true, Aviso: false, Multa: false),
            [TipoDesligamento.Falecimento] = new(Saldo: true, Decimo: true, FeriasVenc: true, FeriasProp: true, Aviso: false, Multa: false),
            [TipoDesligamento.ExoneracaoVacancia] = new(Saldo: true, Decimo: true, FeriasVenc: true, FeriasProp: true, Aviso: false, Multa: false),
        };

    /// <summary>
    /// Compoe as verbas rescisorias devidas para o par (tipo, regime). Aviso previo e multa de FGTS so
    /// para celetista (design §4.1), independentemente da matriz.
    /// </summary>
    /// <param name="tipo">Tipo de desligamento.</param>
    /// <param name="regime">Regime juridico do vinculo.</param>
    /// <returns>Lista (ordenada) das verbas devidas.</returns>
    public static IReadOnlyList<VerbaRescisoria> Compor(TipoDesligamento tipo, RegimeVinculo regime)
    {
        if (!MatrizDefault.TryGetValue(tipo, out var devidas))
        {
            return [];
        }

        var verbas = new List<VerbaRescisoria>();
        if (devidas.Saldo)
        {
            verbas.Add(VerbaRescisoria.SaldoSalario);
        }

        if (devidas.Decimo)
        {
            verbas.Add(VerbaRescisoria.DecimoTerceiroProporcional);
        }

        if (devidas.FeriasVenc)
        {
            verbas.Add(VerbaRescisoria.FeriasVencidas);
        }

        if (devidas.FeriasProp)
        {
            verbas.Add(VerbaRescisoria.FeriasProporcionais);
        }

        // Aviso/multa: SO celetista (estatutario nao tem FGTS/aviso — design §4.1).
        if (regime == RegimeVinculo.Celetista)
        {
            if (devidas.Aviso)
            {
                verbas.Add(VerbaRescisoria.AvisoPrevio);
            }

            if (devidas.Multa)
            {
                verbas.Add(VerbaRescisoria.MultaFgts);
            }
        }

        return verbas;
    }

    /// <summary>
    /// Calcula o saldo de salario: <c>vencimento x diasTrabalhados / diasDoMes</c> (design §4.3). Datas
    /// sao entrada — nao ha relogio.
    /// </summary>
    /// <param name="vencimento">Vencimento mensal do servidor.</param>
    /// <param name="diasTrabalhadosNoMes">Dias trabalhados no mes do desligamento (1..diasDoMes).</param>
    /// <param name="diasDoMes">Dias do mes do desligamento (28..31).</param>
    /// <returns>Saldo de salario (2 casas).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os parametros forem invalidos.</exception>
    public static decimal CalcularSaldoSalario(decimal vencimento, int diasTrabalhadosNoMes, int diasDoMes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vencimento);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(diasDoMes);
        ArgumentOutOfRangeException.ThrowIfNegative(diasTrabalhadosNoMes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(diasTrabalhadosNoMes, diasDoMes);
        return decimal.Round(vencimento * diasTrabalhadosNoMes / diasDoMes, 2, MidpointRounding.AwayFromZero);
    }

    private readonly record struct VerbasDevidas(
        bool Saldo,
        bool Decimo,
        bool FeriasVenc,
        bool FeriasProp,
        bool Aviso,
        bool Multa);
}
