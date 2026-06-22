namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Situação (estado) de uma ordem de serviço de manutenção do veículo.</summary>
public enum SituacaoOrdemServico
{
    /// <summary>Ordem de serviço aberta.</summary>
    Aberta = 1,

    /// <summary>Manutenção concluída.</summary>
    Concluida = 2,

    /// <summary>Ordem cancelada.</summary>
    Cancelada = 3,
}

/// <summary>Situação (estado) de uma multa de trânsito atribuída ao veículo (CTB).</summary>
public enum SituacaoMulta
{
    /// <summary>Multa pendente de pagamento.</summary>
    Pendente = 1,

    /// <summary>Multa em recurso (defesa/JARI).</summary>
    EmRecurso = 2,

    /// <summary>Multa paga.</summary>
    Paga = 3,
}

/// <summary>Situação (estado) do licenciamento anual/IPVA de um exercício (CTB).</summary>
public enum SituacaoLicenciamento
{
    /// <summary>Licenciamento pendente no exercício.</summary>
    Pendente = 1,

    /// <summary>Licenciamento regular no exercício.</summary>
    Regular = 2,
}
