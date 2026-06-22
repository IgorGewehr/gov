namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>Fundamento da contratacao (Lei 14.133/2021).</summary>
public enum OrigemContratacao
{
    /// <summary>Contrato decorrente de certame homologado (art. 89).</summary>
    Licitacao = 1,

    /// <summary>Contratacao direta por valor/hipotese (art. 75).</summary>
    Dispensa = 2,

    /// <summary>Contratacao direta por inviabilidade de competicao (art. 74).</summary>
    Inexigibilidade = 3,
}

/// <summary>Situacao (estado) do contrato no ciclo de vida (Lei 14.133/2021).</summary>
public enum SituacaoContrato
{
    /// <summary>Assinado, ainda sem eficacia (sem publicacao no PNCP / sem dotacao confirmada).</summary>
    Assinado = 1,

    /// <summary>Publicado no PNCP e com cobertura orcamentaria — apto a executar.</summary>
    Eficaz = 2,

    /// <summary>Vigencia iniciada; execucao em andamento.</summary>
    EmExecucao = 3,

    /// <summary>Vigencia concluida / objeto entregue (terminal).</summary>
    Encerrado = 4,

    /// <summary>Extinto antecipadamente (terminal).</summary>
    Rescindido = 5,
}

/// <summary>Tipo do termo aditivo (Lei 14.133/2021, art. 124 a 136).</summary>
public enum TipoAditivo
{
    /// <summary>Acrescimo quantitativo (conta para o limite de 25%/50% — art. 125).</summary>
    Acrescimo = 1,

    /// <summary>Supressao quantitativa (conta para o limite de 25%/50% — art. 125).</summary>
    Supressao = 2,

    /// <summary>Prorrogacao de prazo (nao conta para o limite quantitativo).</summary>
    Prazo = 3,

    /// <summary>Reequilibrio economico-financeiro (recomposicao da equacao economica).</summary>
    Reequilibrio = 4,

    /// <summary>Alteracao qualitativa (nao quantitativa) do objeto.</summary>
    Qualitativo = 5,
}

/// <summary>Tipo do apostilamento — alteracao que dispensa termo aditivo (Lei 14.133/2021, art. 136).</summary>
public enum TipoApostilamento
{
    /// <summary>Reajuste contratual por indice pactuado.</summary>
    Reajuste = 1,

    /// <summary>Atualizacao de dotacao/credito orcamentario.</summary>
    Dotacao = 2,

    /// <summary>Correcao de erro material/formal.</summary>
    Correcao = 3,
}

/// <summary>Modalidade da garantia de execucao (Lei 14.133/2021, art. 96).</summary>
public enum ModalidadeGarantia
{
    /// <summary>Caucao em dinheiro.</summary>
    CaucaoDinheiro = 1,

    /// <summary>Seguro-garantia.</summary>
    SeguroGarantia = 2,

    /// <summary>Fianca bancaria.</summary>
    FiancaBancaria = 3,

    /// <summary>Titulos da divida publica.</summary>
    TitulosDividaPublica = 4,
}
