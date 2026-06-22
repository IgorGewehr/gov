namespace Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

/// <summary>Situacao (estado) do mandato de um vereador na legislatura corrente.</summary>
public enum SituacaoVereador
{
    /// <summary>Em exercicio (mandato ativo).</summary>
    EmExercicio = 1,

    /// <summary>Licenciado (afastamento temporario; suplente pode assumir).</summary>
    Licenciado = 2,

    /// <summary>Afastado (perda temporaria do exercicio, ex.: decisao judicial).</summary>
    Afastado = 3,

    /// <summary>Mandato encerrado/cassado (terminal — nao retorna ao exercicio).</summary>
    Encerrado = 4,
}

/// <summary>Cargo na Mesa Diretora da Camara (CF/88 art. 29; Regimento Interno).</summary>
public enum CargoMesa
{
    /// <summary>Vereador sem cargo na Mesa Diretora.</summary>
    Nenhum = 0,

    /// <summary>Presidente da Camara (dirige as sessoes; voto de qualidade/desempate).</summary>
    Presidente = 1,

    /// <summary>Vice-Presidente (substitui o Presidente nos impedimentos).</summary>
    VicePresidente = 2,

    /// <summary>1.º Secretario (lavra as atas; controla a presenca).</summary>
    PrimeiroSecretario = 3,

    /// <summary>2.º Secretario (auxilia e substitui o 1.º Secretario).</summary>
    SegundoSecretario = 4,
}
