namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;

/// <summary>
/// Tipo de desligamento que determina, junto com o <see cref="RegimeVinculo"/>, as verbas rescisorias
/// devidas (design §4.1 / pesquisa §3.2). Matriz parametrizavel — nenhum <c>if</c> magico no calculo.
/// </summary>
public enum TipoDesligamento
{
    /// <summary>Dispensa sem justa causa (celetista): todas as verbas + aviso + FGTS/multa 40%.</summary>
    DispensaSemJustaCausa = 1,

    /// <summary>Pedido de demissao / exoneracao a pedido: sem multa 40% e sem aviso a receber.</summary>
    PedidoDemissaoExoneracao = 2,

    /// <summary>Justa causa: saldo + ferias vencidas + 1/3; sem 13o proporcional, ferias proporcionais, aviso ou multa.</summary>
    JustaCausa = 3,

    /// <summary>Distrato (acordo CLT 484-A): aviso e multa 40% pela metade. // TODO(validar-oficial): aplicabilidade a celetistas publicos.</summary>
    Distrato = 4,

    /// <summary>Aposentadoria: saldo + 13o proporcional + ferias vencidas/proporcionais + 1/3.</summary>
    Aposentadoria = 5,

    /// <summary>Falecimento: saldo + 13o proporcional + ferias vencidas/proporcionais + 1/3 (aos dependentes).</summary>
    Falecimento = 6,

    /// <summary>Exoneracao/vacancia de estatutario: sem FGTS/aviso/multa; ferias/13o indenizados (CF art. 41; Lei 327/2008).</summary>
    ExoneracaoVacancia = 7,
}

/// <summary>
/// Regime juridico do vinculo, que define a existencia de FGTS, aviso previo e multa de 40% na rescisao
/// (so celetista) versus exoneracao/vacancia do estatutario (design §4.1). Atributo do vinculo, derivado
/// do tipo de cargo/regime previdenciario — mas a rescisao depende do REGIME JURIDICO.
/// </summary>
public enum RegimeVinculo
{
    /// <summary>Estatutario (RPPS/RGPS): sem FGTS, aviso ou multa; usa exoneracao/vacancia.</summary>
    Estatutario = 1,

    /// <summary>Empregado publico celetista (RGPS): verbas celetistas (FGTS+40%, aviso previo).</summary>
    Celetista = 2,
}
