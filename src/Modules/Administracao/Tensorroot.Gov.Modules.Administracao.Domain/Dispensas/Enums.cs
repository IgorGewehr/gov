namespace Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

/// <summary>
/// Situacao (estado) da dispensa eletronica no ciclo de vida procedimental, conforme o fluxo da
/// IN SEGES/ME 67/2021 (Sistema de Dispensa Eletronica) sobre a Lei 14.133/2021, art. 75:
/// aviso de contratacao direta -> disputa (lances) -> julgamento -> habilitacao -> homologacao.
/// </summary>
public enum SituacaoDispensa
{
    /// <summary>Dispensa aberta (rascunho): itens em cadastro, ainda sem aviso publicado (estado inicial).</summary>
    Aberta = 1,

    /// <summary>Aviso de contratacao direta publicado; aguardando a abertura da disputa (prazo minimo de divulgacao — art. 75 §3; IN 67/2021).</summary>
    AvisoPublicado = 2,

    /// <summary>Etapa de envio de lances sucessivos em andamento (janela de disputa aberta; IN 67/2021).</summary>
    EmDisputa = 3,

    /// <summary>Disputa encerrada; propostas em classificacao/julgamento e negociacao com o melhor colocado.</summary>
    EmJulgamento = 4,

    /// <summary>Resultado homologado pela autoridade competente — habilita a contratacao direta (terminal de sucesso).</summary>
    Homologada = 5,

    /// <summary>Sem proposta valida/habilitada apos a disputa (terminal).</summary>
    Fracassada = 6,

    /// <summary>Sem cotacoes/interessados (terminal).</summary>
    Deserta = 7,

    /// <summary>Encerrada por conveniencia/oportunidade (terminal).</summary>
    Revogada = 8,

    /// <summary>Encerrada por ilegalidade (terminal).</summary>
    Anulada = 9,
}

/// <summary>
/// Fundamento legal da dispensa em razao do valor (Lei 14.133/2021, art. 75, I e II) — as duas
/// hipoteses que comportam o procedimento competitivo de dispensa eletronica por faixa de valor.
/// Os limites monetarios sao parametrizaveis por tenant (Dec. 12.343/2024 e atualizacoes anuais
/// pelo IPCA-E, art. 182), nunca fixados no codigo.
/// </summary>
public enum FundamentoDispensaValor
{
    /// <summary>Art. 75, I — obras e servicos de engenharia ou de manutencao de veiculos automotores.</summary>
    ObrasEServicosEngenharia = 1,

    /// <summary>Art. 75, II — outros servicos e compras.</summary>
    OutrosServicosECompras = 2,
}

/// <summary>
/// Criterio de julgamento admitido na dispensa eletronica (IN SEGES/ME 67/2021, art. 1 §2): apenas
/// menor preco ou maior desconto.
/// </summary>
public enum CriterioJulgamentoDispensa
{
    /// <summary>Menor preco (ordena ascendente pelo valor da cotacao).</summary>
    MenorPreco = 1,

    /// <summary>Maior desconto (ordena descendente pelo percentual de desconto ofertado).</summary>
    MaiorDesconto = 2,
}

/// <summary>Situacao de uma cotacao (lance/proposta) recebida no procedimento de dispensa.</summary>
public enum SituacaoCotacao
{
    /// <summary>Cotacao recebida, ainda sem analise.</summary>
    Recebida = 1,

    /// <summary>Cotacao classificada na disputa.</summary>
    Classificada = 2,

    /// <summary>Cotacao desclassificada.</summary>
    Desclassificada = 3,

    /// <summary>Cotacao julgada vencedora.</summary>
    Vencedora = 4,
}
