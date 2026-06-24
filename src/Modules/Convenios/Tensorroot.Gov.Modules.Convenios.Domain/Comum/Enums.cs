namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>Esfera do orgao concedente do convenio federal recebido (fluxo A).</summary>
public enum EsferaConcedente
{
    /// <summary>Uniao (orgao/entidade federal).</summary>
    Uniao = 1,

    /// <summary>Estado/Distrito Federal.</summary>
    Estado = 2,
}

/// <summary>Sistema de origem do cadastro do convenio federal (fluxo A).</summary>
public enum SistemaOrigemConvenio
{
    /// <summary>Transferegov.br (plataforma vigente — Dec. 11.531/2023).</summary>
    Transferegov = 1,

    /// <summary>SICONV (plataforma legada, ainda referenciada em convenios antigos).</summary>
    SiconvLegado = 2,

    /// <summary>Outra origem (convenio estadual fora do Transferegov, instrumento proprio).</summary>
    Outro = 3,
}

/// <summary>Modalidade da contrapartida do convenio recebido (fluxo A).</summary>
public enum ModalidadeContrapartida
{
    /// <summary>Contrapartida financeira (aporte em dinheiro).</summary>
    Financeira = 1,

    /// <summary>Contrapartida em bens e servicos economicamente mensuraveis.</summary>
    BensServicos = 2,
}

/// <summary>Situacao de uma parcela/repasse (previsto, liberado, bloqueado) nos dois fluxos.</summary>
public enum SituacaoRepasse
{
    /// <summary>Prevista no cronograma de desembolso, ainda nao liberada.</summary>
    Prevista = 1,

    /// <summary>Liberada/recebida (fluxo A) ou paga a OSC (fluxo B).</summary>
    Liberada = 2,

    /// <summary>Bloqueada (inadimplencia/gatilho LRF) — nao pode ser liberada.</summary>
    Bloqueada = 3,
}

/// <summary>Tipo de prestacao de contas do convenio recebido (fluxo A).</summary>
public enum TipoPrestacaoConvenio
{
    /// <summary>Parcial (continua, por parcela/etapa).</summary>
    Parcial = 1,

    /// <summary>Final (apos a vigencia, fecha o objeto).</summary>
    Final = 2,
}

/// <summary>Tipo de instrumento da parceria-saida OSC (MROSC, fluxo B).</summary>
public enum TipoInstrumentoMrosc
{
    /// <summary>Termo de Colaboracao (proposta da Administracao — Lei 13.019 art. 16).</summary>
    TermoColaboracao = 1,

    /// <summary>Termo de Fomento (proposta da OSC — art. 17).</summary>
    TermoFomento = 2,

    /// <summary>Acordo de Cooperacao (sem transferencia de recursos — art. 2o VIII-A).</summary>
    AcordoCooperacao = 3,
}

/// <summary>Forma de selecao da OSC (mutuamente exclusivas — fluxo B).</summary>
public enum TipoFormaSelecao
{
    /// <summary>Chamamento publico (art. 24) — regra geral.</summary>
    Chamamento = 1,

    /// <summary>Dispensa de chamamento (art. 30) — hipoteses taxativas.</summary>
    Dispensa = 2,

    /// <summary>Inexigibilidade de chamamento (art. 31) — inviabilidade de competicao.</summary>
    Inexigibilidade = 3,
}

/// <summary>Resultado da analise de uma prestacao de contas (ambos os fluxos).</summary>
public enum ResultadoAnalise
{
    /// <summary>Aprovada (regular).</summary>
    Aprovada = 1,

    /// <summary>Aprovada com ressalva (regular com impropriedades que nao implicam dano).</summary>
    AprovadaComRessalva = 2,

    /// <summary>Rejeitada (irregular) — enseja devolucao/inadimplencia.</summary>
    Rejeitada = 3,
}
