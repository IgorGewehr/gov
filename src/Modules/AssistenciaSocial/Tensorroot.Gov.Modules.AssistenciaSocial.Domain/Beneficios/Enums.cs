namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;

/// <summary>Tipo do beneficio socioassistencial.</summary>
public enum TipoBeneficio
{
    /// <summary>Beneficio de Prestacao Continuada (idoso &gt;= 65 / PCD).</summary>
    Bpc = 1,

    /// <summary>Programa Bolsa Familia (Lei 14.601/2023).</summary>
    Pbf = 2,

    /// <summary>Beneficio eventual (natalidade, funeral, cesta basica).</summary>
    Eventual = 3,
}

/// <summary>Situacao (estado) do beneficio no ciclo de avaliacao/concessao.</summary>
public enum SituacaoBeneficio
{
    /// <summary>Solicitado; aguardando avaliacao de elegibilidade (estado inicial).</summary>
    EmAvaliacao = 1,

    /// <summary>Elegibilidade deferida; beneficio concedido (terminal de deferimento).</summary>
    Concedida = 2,

    /// <summary>Elegibilidade negada; motivo registrado (terminal de indeferimento).</summary>
    Indeferida = 3,
}
