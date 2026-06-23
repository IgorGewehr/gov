namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Ramo de atividade do estabelecimento sujeito a Vigilancia Sanitaria (subconjunto operacional
/// das categorias da RDC Anvisa 153/2017 e da classificacao de risco da VISA municipal).
/// Determina o roteiro de inspecao aplicavel e o grau de risco sanitario padrao.
/// </summary>
public enum RamoVisa
{
    /// <summary>Servico de alimentacao (restaurante, lanchonete, padaria, cozinha industrial).</summary>
    Alimentacao = 1,

    /// <summary>Industria/comercio de alimentos (fabrica, distribuidora, mercado).</summary>
    Alimentos = 2,

    /// <summary>Farmacia/drogaria/dispensario de medicamentos.</summary>
    Farmacia = 3,

    /// <summary>Estabelecimento de saude (clinica, consultorio, laboratorio, hospital).</summary>
    Saude = 4,

    /// <summary>Estetica/embelezamento (salao, barbearia, clinica de estetica, tatuagem).</summary>
    Estetica = 5,

    /// <summary>Hospedagem/eventos (hotel, motel, espaco de eventos).</summary>
    Hospedagem = 6,

    /// <summary>Educacao infantil/instituicao de longa permanencia (creche, escola, ILPI).</summary>
    InstituicaoColetiva = 7,

    /// <summary>Outro ramo sujeito a VISA.</summary>
    Outro = 99,
}

/// <summary>
/// Grau de risco sanitario do estabelecimento (classificacao da RDC Anvisa 153/2017 / Lei 13.874/2019,
/// Declaracao de Direitos de Liberdade Economica). Modula prazos, periodicidade de inspecao e a
/// exigencia (ou dispensa) de licenciamento previo.
/// </summary>
public enum GrauRiscoSanitario
{
    /// <summary>Risco I (baixo) — dispensa de licenciamento previo (Lei 13.874/2019, art. 3º, I).</summary>
    Baixo = 1,

    /// <summary>Risco II (medio) — licenciamento com analise simplificada.</summary>
    Medio = 2,

    /// <summary>Risco III (alto) — licenciamento com inspecao previa obrigatoria.</summary>
    Alto = 3,
}

/// <summary>Situacao cadastral do estabelecimento sujeito a VISA.</summary>
public enum SituacaoEstabelecimentoVisa
{
    /// <summary>Ativo (em operacao, fiscalizavel).</summary>
    Ativo = 1,

    /// <summary>Inativo (baixado/encerrado) — fora do ciclo de fiscalizacao.</summary>
    Inativo = 2,

    /// <summary>Interditado (cautelar/definitivo) — operacao vedada por ato da VISA.</summary>
    Interditado = 3,
}

/// <summary>Situacao da inspecao/vistoria sanitaria (maquina de estado do roteiro).</summary>
public enum SituacaoInspecao
{
    /// <summary>Aberta (roteiro em preenchimento; aceita itens).</summary>
    Aberta = 1,

    /// <summary>Concluida (resultado apurado; nao aceita mais itens).</summary>
    Concluida = 2,

    /// <summary>Cancelada (vistoria invalidada — sem efeitos fiscalizatorios).</summary>
    Cancelada = 3,
}

/// <summary>Conformidade de um item do roteiro de inspecao.</summary>
public enum ConformidadeItem
{
    /// <summary>Conforme (atende ao requisito sanitario).</summary>
    Conforme = 1,

    /// <summary>Nao conforme (gera pendencia/possivel auto).</summary>
    NaoConforme = 2,

    /// <summary>Nao se aplica ao ramo/estabelecimento.</summary>
    NaoAplicavel = 3,
}

/// <summary>
/// Resultado consolidado da inspecao, derivado das conformidades dos itens. Define o desfecho
/// administrativo: aprovado (apto a licenca), com pendencias (intimacao) ou reprovado (auto/interdicao).
/// </summary>
public enum ResultadoInspecao
{
    /// <summary>Aprovado sem ressalvas (nenhum item nao conforme).</summary>
    Aprovado = 1,

    /// <summary>Aprovado com pendencias (nao conformidades sanaveis — gera intimacao/prazo).</summary>
    AprovadoComPendencias = 2,

    /// <summary>Reprovado (nao conformidades graves — gera auto de infracao/interdicao).</summary>
    Reprovado = 3,
}

/// <summary>
/// Tipo do auto lavrado pela VISA. Baseado no rito do processo administrativo sanitario
/// (Lei 6.437/1977, que define infracoes a legislacao sanitaria federal e as penalidades).
/// </summary>
public enum TipoAutoVisa
{
    /// <summary>Auto de Intimacao — fixa pendencias e prazo para regularizacao (sem penalidade imediata).</summary>
    Intimacao = 1,

    /// <summary>Auto de Infracao — formaliza a infracao sanitaria, abre prazo de defesa e pode cominar multa.</summary>
    Infracao = 2,

    /// <summary>Auto de Imposicao de Penalidade (apos defesa) — interdicao/multa/cancelamento de licenca.</summary>
    ImposicaoPenalidade = 3,
}

/// <summary>Situacao do auto no processo administrativo sanitario (maquina de estado de prazos/defesa).</summary>
public enum SituacaoAutoVisa
{
    /// <summary>Lavrado (notificado ao autuado; prazo de defesa em curso).</summary>
    Lavrado = 1,

    /// <summary>Defesa apresentada (aguardando julgamento).</summary>
    DefesaApresentada = 2,

    /// <summary>Deferido (defesa acolhida; auto cancelado sem penalidade).</summary>
    Deferido = 3,

    /// <summary>Indeferido (defesa rejeitada / prazo de defesa expirado sem manifestacao).</summary>
    Indeferido = 4,

    /// <summary>Regularizado (pendencias da intimacao sanadas no prazo).</summary>
    Regularizado = 5,
}

/// <summary>Situacao da licenca/alvara sanitario (maquina de estado de emissao/validade/renovacao).</summary>
public enum SituacaoLicenca
{
    /// <summary>Vigente (dentro do prazo de validade).</summary>
    Vigente = 1,

    /// <summary>Vencida (expirou sem renovacao).</summary>
    Vencida = 2,

    /// <summary>Cassada/revogada (por ato da VISA, geralmente apos auto de penalidade).</summary>
    Cassada = 3,
}
