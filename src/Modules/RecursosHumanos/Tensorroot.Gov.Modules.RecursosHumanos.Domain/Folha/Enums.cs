namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>Natureza de um evento (verba) da folha de pagamento.</summary>
public enum TipoEvento
{
    /// <summary>Verba crediticia (provento).</summary>
    Provento = 1,

    /// <summary>Verba debitoria (desconto — inclui abate-teto e consignacoes).</summary>
    Desconto = 2,
}

/// <summary>
/// Tipo (natureza) de uma folha de pagamento no ciclo anual. Roteia incidencias, abate-teto e eSocial:
/// a folha MENSAL aplica o teto (CF art. 37, XI); 13o/ferias/rescisao apuram-se em folha propria, sem
/// abate-teto. O 13o usa BASE SEPARADA de tributacao (Lei 7.713/88 art. 12-A). Parametrizavel via design
/// FOLHA-CICLO-ANUAL-DESIGN §1.1 — nada hardcoded no calculo.
/// </summary>
public enum TipoFolha
{
    /// <summary>Folha mensal ordinaria (aplica abate-teto — CF art. 37, XI).</summary>
    Mensal = 1,

    /// <summary>Gratificacao natalina (13o salario), base separada de tributacao (Lei 7.713/88 art. 12-A).</summary>
    DecimoTerceiro = 2,

    /// <summary>Ferias (remuneracao do periodo + 1/3 constitucional + abono pecuniario opcional).</summary>
    Ferias = 3,

    /// <summary>Rescisao/desligamento (verbas rescisorias por tipo de desligamento e regime).</summary>
    Rescisao = 4,
}

/// <summary>Situacao (estado) da folha de pagamento na competencia.</summary>
public enum SituacaoFolha
{
    /// <summary>Folha aberta, aceitando lancamento de eventos (estado inicial).</summary>
    Aberta = 1,

    /// <summary>Proventos/descontos/liquido apurados (S-1200/S-1202 prontos).</summary>
    Calculada = 2,

    /// <summary>Competencia fechada (S-1299, S-1210, totalizadores, DCTFWeb).</summary>
    Fechada = 3,

    /// <summary>Liquido pago (terminal).</summary>
    Paga = 4,
}
