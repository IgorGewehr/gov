namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Especie (tipo) da materia submetida a apreciacao do Plenario.</summary>
public enum TipoProposicao
{
    /// <summary>PLO — Projeto de Lei Ordinaria (maioria simples).</summary>
    ProjetoDeLeiOrdinaria = 1,

    /// <summary>PLC — Projeto de Lei Complementar (maioria absoluta).</summary>
    ProjetoDeLeiComplementar = 2,

    /// <summary>Emenda a Lei Organica Municipal (rito qualificado: 2/3 em dois turnos).</summary>
    EmendaALOM = 3,

    /// <summary>PDL — Projeto de Decreto Legislativo (competencia exclusiva da Camara).</summary>
    ProjetoDeDecretoLegislativo = 4,

    /// <summary>PR — Projeto de Resolucao (assuntos internos, ex.: Regimento; maioria absoluta).</summary>
    ProjetoDeResolucao = 5,

    /// <summary>Requerimento — manifestacao parlamentar.</summary>
    Requerimento = 6,

    /// <summary>Indicacao — manifestacao parlamentar.</summary>
    Indicacao = 7,

    /// <summary>Mocao — manifestacao parlamentar.</summary>
    Mocao = 8,
}

/// <summary>Situacao (estado) da proposicao no ciclo de tramitacao.</summary>
public enum SituacaoProposicao
{
    /// <summary>Protocolada/apresentada (estado inicial).</summary>
    Apresentada = 1,

    /// <summary>Distribuida as Comissoes para instrucao.</summary>
    Distribuida = 2,

    /// <summary>Incluida em Ordem do Dia para deliberacao.</summary>
    EmOrdemDoDia = 3,

    /// <summary>Aprovada em Plenario.</summary>
    Aprovada = 4,

    /// <summary>Rejeitada em Plenario (terminal).</summary>
    Rejeitada = 5,

    /// <summary>Arquivada (terminal).</summary>
    Arquivada = 6,

    /// <summary>Autografo gerado e enviado ao Executivo.</summary>
    AutografoEnviado = 7,
}

/// <summary>Regime (rito) de tramitacao aplicavel a proposicao.</summary>
public enum RegimeTramitacao
{
    /// <summary>Rito ordinario (padrao).</summary>
    Ordinario = 1,

    /// <summary>Rito de urgencia.</summary>
    Urgencia = 2,

    /// <summary>Rito de prioridade.</summary>
    Prioridade = 3,

    /// <summary>Rito qualificado (ex.: Emenda a LOM).</summary>
    Qualificado = 4,
}

/// <summary>Fase de uma <see cref="Tramitacao"/> na trilha imutavel da proposicao.</summary>
public enum FaseTramitacao
{
    /// <summary>Apresentacao/protocolo da proposicao.</summary>
    Apresentacao = 1,

    /// <summary>Distribuicao as Comissoes.</summary>
    Distribuicao = 2,

    /// <summary>Emissao de parecer por Comissao.</summary>
    Parecer = 3,

    /// <summary>Inclusao em Ordem do Dia.</summary>
    OrdemDoDia = 4,

    /// <summary>Aprovacao em Plenario.</summary>
    Aprovacao = 5,

    /// <summary>Rejeicao em Plenario.</summary>
    Rejeicao = 6,

    /// <summary>Geracao e remessa de autografo ao Executivo.</summary>
    Autografo = 7,

    /// <summary>Arquivamento.</summary>
    Arquivamento = 8,

    /// <summary>Sancao recebida do Executivo.</summary>
    Sancao = 9,

    /// <summary>Veto recebido do Executivo.</summary>
    Veto = 10,
}
