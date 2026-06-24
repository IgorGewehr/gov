namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>
/// Regime de execução do contrato de obra/serviço de engenharia (Lei 14.133/2021, art. 46).
/// Define como a contratada executa e como a medição se relaciona com o pagamento.
/// </summary>
public enum RegimeExecucao
{
    /// <summary>Empreitada por preço unitário (medição por unidade de serviço executada — art. 46, II).</summary>
    EmpreitadaPorPrecoUnitario = 1,

    /// <summary>Empreitada por preço global (preço certo e total — art. 46, I).</summary>
    EmpreitadaPorPrecoGlobal = 2,

    /// <summary>Tarefa (mão de obra para pequenos trabalhos, com ou sem material — art. 46, III).</summary>
    Tarefa = 3,

    /// <summary>Empreitada integral (empreendimento em sua integralidade — art. 46, IV).</summary>
    EmpreitadaIntegral = 4,

    /// <summary>Contratação integrada (projeto básico e executivo + execução — art. 46, V).</summary>
    ContratacaoIntegrada = 5,

    /// <summary>Contratação semi-integrada (projeto executivo + execução — art. 46, VI).</summary>
    ContratacaoSemiIntegrada = 6,
}

/// <summary>
/// Situação (estado) da obra no ciclo de execução física. Máquina de estados:
/// Planejada → EmExecucao → (Paralisada ⇄ EmExecucao) → Concluida → Incorporada; também Rescindida.
/// </summary>
public enum SituacaoObra
{
    /// <summary>Aberta, com cronograma a definir/definido, antes da ordem de início.</summary>
    Planejada = 1,

    /// <summary>Em execução (ordem de início emitida); aceita RDO e medição.</summary>
    EmExecucao = 2,

    /// <summary>Paralisada (suspende novas medições/RDOs; pode reiniciar).</summary>
    Paralisada = 3,

    /// <summary>Concluída fisicamente (100%); pendente de incorporação patrimonial (terminal de execução).</summary>
    Concluida = 4,

    /// <summary>Incorporada ao acervo como bem patrimonial (imobilizado) — terminal.</summary>
    Incorporada = 5,

    /// <summary>Rescindida antes da conclusão (terminal).</summary>
    Rescindida = 6,
}

/// <summary>Situação de uma etapa do cronograma físico-financeiro.</summary>
public enum SituacaoEtapa
{
    /// <summary>Prevista (ainda sem medição).</summary>
    Prevista = 1,

    /// <summary>Em andamento (medição parcial reconhecida).</summary>
    EmAndamento = 2,

    /// <summary>Concluída (100% físico medido).</summary>
    Concluida = 3,
}

/// <summary>Situação de um boletim de medição periódica.</summary>
public enum SituacaoMedicao
{
    /// <summary>Rascunho — registrada pela contratada/fiscalização, aguardando aprovação.</summary>
    Rascunho = 1,

    /// <summary>Aprovada pelo fiscal — gatilho da liquidação em Finanças.</summary>
    Aprovada = 2,

    /// <summary>Rejeitada pelo fiscal (não compõe o valor medido acumulado).</summary>
    Rejeitada = 3,
}

/// <summary>Tipo de ocorrência registrada pela fiscalização (Lei 14.133/2021, art. 117).</summary>
public enum TipoOcorrenciaFiscalizacao
{
    /// <summary>Notificação formal à contratada.</summary>
    Notificacao = 1,

    /// <summary>Advertência (sanção branda / registro de descumprimento).</summary>
    Advertencia = 2,

    /// <summary>Registro técnico (anotação de fato relevante da execução).</summary>
    RegistroTecnico = 3,
}

/// <summary>Motivo da paralisação da obra (suspensão da execução física).</summary>
public enum MotivoParalisacao
{
    /// <summary>Ordem da fiscalização (descumprimento/risco técnico).</summary>
    OrdemFiscalizacao = 1,

    /// <summary>Falta/insuficiência de projeto.</summary>
    FaltaProjeto = 2,

    /// <summary>Condição climática adversa.</summary>
    Clima = 3,

    /// <summary>Restrição orçamentária/financeira.</summary>
    Orcamentaria = 4,

    /// <summary>Determinação judicial.</summary>
    Judicial = 5,
}

/// <summary>Tipo do prazo legal do art. 94 §3 (Lei 14.133/2021) aplicável à obra.</summary>
public enum TipoPrazoArt94
{
    /// <summary>Publicação/registro após a assinatura do contrato (padrão: 25 dias úteis).</summary>
    Assinatura25 = 1,

    /// <summary>Publicação/registro após a conclusão da obra (padrão: 45 dias úteis).</summary>
    Conclusao45 = 2,
}
