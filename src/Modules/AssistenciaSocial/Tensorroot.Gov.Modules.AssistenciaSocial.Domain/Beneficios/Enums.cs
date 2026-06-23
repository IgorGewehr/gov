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

/// <summary>
/// Modalidade do beneficio eventual (LOAS art. 22, com a redacao da Lei 12.435/2011; Decreto
/// 6.307/2007). As modalidades sao definidas em LEI/DECRETO MUNICIPAL + deliberacao do CMAS — o
/// rol abaixo cobre as hipoteses legais federais; o municipio pode habilitar/parametrizar cada uma
/// (valor, criterio de renda, documentacao) por tenant+vigencia. NAO ha teto federal de renda.
/// </summary>
public enum ModalidadeBeneficioEventual
{
    /// <summary>Auxilio natalidade (LOAS art. 22, I; Decreto 6.307/2007 art. 4º).</summary>
    Natalidade = 1,

    /// <summary>Auxilio por morte / funeral (LOAS art. 22, II; Decreto 6.307/2007 art. 5º).</summary>
    Morte = 2,

    /// <summary>Provisao por vulnerabilidade temporaria (LOAS art. 22, caput; Decreto 6.307/2007 art. 6º).</summary>
    VulnerabilidadeTemporaria = 3,

    /// <summary>Provisao em situacao de calamidade publica / emergencia (Decreto 6.307/2007 art. 7º).</summary>
    Calamidade = 4,
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
