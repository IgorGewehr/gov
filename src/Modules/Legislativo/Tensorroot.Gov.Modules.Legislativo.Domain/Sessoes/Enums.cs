namespace Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

/// <summary>Especie (tipo) da sessao plenaria.</summary>
public enum TipoSessao
{
    /// <summary>Sessao ordinaria (calendario regular).</summary>
    Ordinaria = 1,

    /// <summary>Sessao extraordinaria (convocacao especial fora do calendario).</summary>
    Extraordinaria = 2,
}

/// <summary>Situacao (estado) atual da sessao plenaria.</summary>
public enum SituacaoSessao
{
    /// <summary>Agendada (estado inicial), aguardando instalacao.</summary>
    Agendada = 1,

    /// <summary>Instalada/aberta (quorum de instalacao atingido).</summary>
    Aberta = 2,

    /// <summary>Suspensa temporariamente.</summary>
    Suspensa = 3,

    /// <summary>Encerrada (terminal).</summary>
    Encerrada = 4,

    /// <summary>Cancelada por falta de quorum ou decisao da Mesa (terminal).</summary>
    Cancelada = 5,
}
