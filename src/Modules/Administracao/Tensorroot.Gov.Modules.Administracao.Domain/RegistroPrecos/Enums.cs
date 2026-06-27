namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Situacao da Ata de Registro de Precos (ARP) no seu ciclo de vida (art. 82-86, Lei 14.133/2021).</summary>
public enum SituacaoAta
{
    /// <summary>Ata vigente: itens disponiveis para contratacao e adesao.</summary>
    Vigente = 1,

    /// <summary>Ata encerrada por decurso do prazo de vigencia.</summary>
    Encerrada = 2,

    /// <summary>Ata cancelada por ato administrativo (descumprimento, interesse publico, etc.).</summary>
    Cancelada = 3,
}

/// <summary>
/// Papel de um orgao/entidade no Sistema de Registro de Precos (art. 86, Lei 14.133/2021; Dec. 11.462/2023).
/// Determina como o saldo da ata e consumido e quais limites de adesao incidem.
/// </summary>
public enum TipoOrgaoSrp
{
    /// <summary>
    /// Orgao GERENCIADOR — conduz a licitacao SRP e gerencia a ata (art. 86, caput). Consome saldo
    /// diretamente (sem limite de adesao; o saldo registrado ja e o seu + dos participantes).
    /// </summary>
    Gerenciador = 1,

    /// <summary>
    /// Orgao PARTICIPANTE — integrou o planejamento da contratacao (IRP) e teve sua estimativa somada
    /// ao quantitativo registrado (art. 86, §1º). Consome saldo diretamente, sem limite de adesao.
    /// </summary>
    Participante = 2,

    /// <summary>
    /// Orgao NAO PARTICIPANTE ("carona") — adere a ata sem ter participado do planejamento (art. 86,
    /// §§ 2º a 5º). Sujeito aos limites de adesao: por orgao (50% do registrado) e total (dobro/200%).
    /// </summary>
    NaoParticipante = 3,
}

/// <summary>Natureza de um movimento de saldo de item da ata, para a trilha de consumo/remanejamento.</summary>
public enum TipoMovimentoSaldo
{
    /// <summary>Consumo direto pelo orgao gerenciador (uso da propria ata).</summary>
    ConsumoGerenciador = 1,

    /// <summary>Consumo por orgao participante (estimativa propria registrada na IRP).</summary>
    ConsumoParticipante = 2,

    /// <summary>Consumo por adesao de orgao nao participante (carona — art. 86, §§ 2º a 5º).</summary>
    Adesao = 3,

    /// <summary>Remanejamento de quantitativos entre orgaos/itens da ata (Dec. 11.462/2023, art. 33).</summary>
    Remanejamento = 4,
}
