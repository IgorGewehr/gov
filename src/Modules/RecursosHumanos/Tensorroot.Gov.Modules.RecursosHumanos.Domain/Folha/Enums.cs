namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>Natureza de um evento (verba) da folha de pagamento.</summary>
public enum TipoEvento
{
    /// <summary>Verba crediticia (provento).</summary>
    Provento = 1,

    /// <summary>Verba debitoria (desconto — inclui abate-teto e consignacoes).</summary>
    Desconto = 2,
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
