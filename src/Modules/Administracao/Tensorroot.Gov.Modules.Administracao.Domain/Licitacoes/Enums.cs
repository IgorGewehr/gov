namespace Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

/// <summary>Modalidade do certame (Lei 14.133/2021, art. 28/74/75).</summary>
public enum ModalidadeLicitacao
{
    /// <summary>Pregao — bens/servicos comuns; menor preco/maior desconto (art. 28, I; art. 6, XLI).</summary>
    Pregao = 1,

    /// <summary>Concorrencia — bens/servicos especiais e obras (art. 28, II).</summary>
    Concorrencia = 2,

    /// <summary>Dialogo competitivo — solucoes inovadoras/complexas (art. 28, V; art. 32).</summary>
    DialogoCompetitivo = 3,

    /// <summary>Dispensa — contratacao direta por valor/hipotese legal (art. 75).</summary>
    Dispensa = 4,

    /// <summary>Inexigibilidade — contratacao direta por inviabilidade de competicao (art. 74).</summary>
    Inexigibilidade = 5,
}

/// <summary>Criterio de julgamento das propostas (Lei 14.133/2021, art. 33).</summary>
public enum CriterioJulgamento
{
    /// <summary>Menor preco (art. 33, I).</summary>
    MenorPreco = 1,

    /// <summary>Maior desconto (art. 33, II).</summary>
    MaiorDesconto = 2,

    /// <summary>Melhor tecnica ou conteudo artistico (art. 33, III).</summary>
    MelhorTecnica = 3,

    /// <summary>Tecnica e preco (art. 33, IV).</summary>
    TecnicaEPreco = 4,

    /// <summary>Maior lance, em leilao (art. 33, V).</summary>
    MaiorLance = 5,

    /// <summary>Maior retorno economico (art. 33, VI).</summary>
    MaiorRetornoEconomico = 6,
}

/// <summary>Situacao (estado) da licitacao no ciclo de vida.</summary>
public enum SituacaoLicitacao
{
    /// <summary>Edital publicado; certame em andamento (estado inicial).</summary>
    Aberta = 1,

    /// <summary>Propostas em classificacao/julgamento.</summary>
    EmJulgamento = 2,

    /// <summary>Resultado homologado pela autoridade (terminal de sucesso).</summary>
    Homologada = 3,

    /// <summary>Sem proposta valida/habilitada (terminal).</summary>
    Fracassada = 4,

    /// <summary>Sem interessados (terminal).</summary>
    Deserta = 5,

    /// <summary>Encerrada por conveniencia/oportunidade (terminal).</summary>
    Revogada = 6,

    /// <summary>Encerrada por ilegalidade (terminal).</summary>
    Anulada = 7,
}

/// <summary>Situacao de uma proposta no certame.</summary>
public enum SituacaoProposta
{
    /// <summary>Proposta recebida, ainda sem analise.</summary>
    Recebida = 1,

    /// <summary>Proposta classificada na disputa.</summary>
    Classificada = 2,

    /// <summary>Proposta desclassificada.</summary>
    Desclassificada = 3,

    /// <summary>Proposta julgada vencedora.</summary>
    Vencedora = 4,
}

/// <summary>Resultado da verificacao de habilitacao de um licitante.</summary>
public enum ResultadoHabilitacao
{
    /// <summary>Licitante habilitado (apto).</summary>
    Habilitado = 1,

    /// <summary>Licitante inabilitado (inapto).</summary>
    Inabilitado = 2,
}
