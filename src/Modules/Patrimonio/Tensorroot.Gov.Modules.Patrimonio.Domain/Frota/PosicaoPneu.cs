using System.Globalization;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>
/// Posição de montagem de um pneu no layout de eixos de um veículo (gestão de frota): a combinação
/// de <see cref="Eixo"/> (dianteiro→traseiro) e <see cref="LadoMontagem"/> (esquerdo/direito, interno
/// para rodado duplo, ou estepe). Identifica univocamente um "slot" de pneu em um veículo, base do
/// controle de posicionamento e do rodízio/reposicionamento.
/// </summary>
/// <param name="Eixo">Eixo do veículo onde o pneu é montado.</param>
/// <param name="Lado">Lado/posição de montagem no eixo.</param>
public readonly record struct PosicaoPneu(Eixo Eixo, LadoMontagem Lado)
{
    /// <summary>Cria uma posição de pneu no layout de eixos.</summary>
    /// <param name="eixo">Eixo do veículo.</param>
    /// <param name="lado">Lado/posição de montagem.</param>
    /// <returns>Instância de <see cref="PosicaoPneu"/>.</returns>
    public static PosicaoPneu De(Eixo eixo, LadoMontagem lado) => new(eixo, lado);

    /// <summary>Posição do estepe (pneu reserva), no primeiro eixo apenas por convenção de chave.</summary>
    public static PosicaoPneu Estepe { get; } = new(Eixo.Dianteiro, LadoMontagem.Estepe);

    /// <summary>Indica se a posição é a do estepe (reserva, sem rodagem).</summary>
    public bool EhEstepe => Lado == LadoMontagem.Estepe;

    /// <summary>Código curto e estável da posição (ex.: "E1-ESQ", "E2-DIR-INT", "ESTEPE"), para layout/UI.</summary>
    /// <returns>Código textual da posição.</returns>
    public string Codigo => Lado == LadoMontagem.Estepe
        ? "ESTEPE"
        : string.Create(CultureInfo.InvariantCulture, $"E{(int)Eixo}-{SiglaLado(Lado)}");

    private static string SiglaLado(LadoMontagem lado) => lado switch
    {
        LadoMontagem.Esquerdo => "ESQ",
        LadoMontagem.Direito => "DIR",
        LadoMontagem.EsquerdoInterno => "ESQ-INT",
        LadoMontagem.DireitoInterno => "DIR-INT",
        LadoMontagem.Estepe => "ESTEPE",
        _ => "?",
    };

    /// <inheritdoc />
    public override string ToString() => Codigo;
}
