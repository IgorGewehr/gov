namespace Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

/// <summary>Especie do demonstrativo fiscal transmitido ao SICONFI.</summary>
public enum TipoDeclaracaoFiscal
{
    /// <summary>Matriz de Saldos Contabeis (mensal).</summary>
    Msc = 1,

    /// <summary>Relatorio Resumido da Execucao Orcamentaria (bimestral).</summary>
    Rreo = 2,

    /// <summary>Relatorio de Gestao Fiscal (quadrimestral).</summary>
    Rgf = 3,

    /// <summary>Declaracao de Contas Anuais.</summary>
    Dca = 4,
}

/// <summary>Estado atual da declaracao no ciclo Consolidada -&gt; Transmitida -&gt; Homologada/Rejeitada.</summary>
public enum SituacaoDeclaracaoFiscal
{
    /// <summary>Matriz/declaracao consolidada (estado inicial), ainda nao transmitida.</summary>
    Consolidada = 1,

    /// <summary>Transmitida ao SICONFI (protocolo recebido).</summary>
    Transmitida = 2,

    /// <summary>Homologada pelo SICONFI/STN (terminal de sucesso).</summary>
    Homologada = 3,

    /// <summary>Rejeitada pelo SICONFI (terminal de falha; admite reconsolidacao).</summary>
    Rejeitada = 4,
}

/// <summary>Natureza do saldo de uma linha contabil (PCASP).</summary>
public enum NaturezaSaldo
{
    /// <summary>Saldo devedor.</summary>
    Devedor = 1,

    /// <summary>Saldo credor.</summary>
    Credor = 2,
}
