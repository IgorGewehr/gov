namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

/// <summary>
/// Finalidade da certidao de tempo de servico/contribuicao. Determina o regime de apuracao e o texto-fim
/// do documento. Aposentadoria/disponibilidade contam tempo de CONTRIBUICAO (EC 103/2019); demais finalidades
/// (gratificacao/adicional por tempo de servico, licenca-premio) contam tempo de SERVICO (efetivo exercicio).
/// // TODO(validar-oficial): rol/textos exatos conforme regulamento do RPPS do tenant e Portaria MPS 154/2008.
/// </summary>
public enum FinalidadeCertidao
{
    /// <summary>Averbacao/contagem reciproca para APOSENTADORIA em outro regime (CTC — EC 103/2019, art. 25 §9).</summary>
    Aposentadoria = 1,

    /// <summary>Disponibilidade (servidor estavel cujo cargo foi extinto — CF art. 41 §3).</summary>
    Disponibilidade = 2,

    /// <summary>Adicional/gratificacao por tempo de servico (anuenio/quinquenio — conforme estatuto do tenant).</summary>
    AdicionalTempoServico = 3,

    /// <summary>Licenca-premio / licenca-capacitacao por tempo de efetivo exercicio (conforme estatuto do tenant).</summary>
    LicencaPremio = 4,

    /// <summary>Averbacao de tempo para fins gerais/declaratorios (sem efeito previdenciario direto).</summary>
    Declaratoria = 9,
}

/// <summary>
/// Natureza do periodo computado na certidao: tempo prestado no PROPRIO ente (efetivo exercicio apurado
/// do vinculo) ou tempo AVERBADO de outro orgao/regime (importado por certidao de origem).
/// </summary>
public enum NaturezaPeriodo
{
    /// <summary>Tempo de efetivo exercicio no proprio ente (apurado do vinculo do servidor).</summary>
    EfetivoExercicio = 1,

    /// <summary>Tempo averbado de outro orgao/regime (RGPS/RPPS de origem; comprovado por certidao).</summary>
    Averbado = 2,
}

/// <summary>
/// Regime previdenciario de ORIGEM de um periodo averbado (para a contagem reciproca — EC 103/2019).
/// // TODO(validar-oficial): dominio/codigos conforme manual de CTC do regime de destino.
/// </summary>
public enum RegimeOrigemPeriodo
{
    /// <summary>Regime Geral (INSS).</summary>
    Rgps = 1,

    /// <summary>Regime Proprio (RPPS federal/estadual/municipal de origem).</summary>
    Rpps = 2,

    /// <summary>Regime proprio militar/sistema de protecao social dos militares.</summary>
    Militar = 3,
}

/// <summary>
/// Situacao (estado) da certidao na sua maquina de vida: <c>Emitida</c> (vigente/autenticada) ou
/// <c>Anulada</c> (tornada sem efeito; estado terminal — a numeracao consumida nao retrocede).
/// </summary>
public enum SituacaoCertidao
{
    /// <summary>Certidao emitida e autenticada (vigente).</summary>
    Emitida = 1,

    /// <summary>Certidao anulada (sem efeito; estado terminal).</summary>
    Anulada = 2,
}
