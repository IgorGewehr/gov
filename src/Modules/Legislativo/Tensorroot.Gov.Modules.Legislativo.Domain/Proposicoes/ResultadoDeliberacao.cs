namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Maioria exigida pela especie da materia (CF/88 art. 29) — derivada do <see cref="TipoProposicao"/>.</summary>
public enum MaioriaProposicao
{
    /// <summary>Maioria simples: maior que 50% dos presentes (lei ordinaria).</summary>
    Simples = 1,

    /// <summary>Maioria absoluta: maior que 50% dos membros (LC, Regimento, derrubada de veto).</summary>
    Absoluta = 2,

    /// <summary>Maioria qualificada: 2/3 em dois turnos (Emenda a LOM).</summary>
    Qualificada = 3,
}

/// <summary>
/// Resultado de uma deliberacao plenaria: indica se a materia foi aprovada e qual a maior
/// maioria efetivamente alcancada na votacao. Alimenta <see cref="Proposicao.Aprovar"/>,
/// que confronta a maioria atingida com a exigida pelo tipo (I-8).
/// </summary>
public readonly record struct ResultadoDeliberacao
{
    private ResultadoDeliberacao(bool aprovado, MaioriaProposicao maioriaAtingida)
    {
        Aprovado = aprovado;
        MaioriaAtingida = maioriaAtingida;
    }

    /// <summary>Indica se a deliberacao aprovou a materia.</summary>
    public bool Aprovado { get; }

    /// <summary>Maior maioria efetivamente atingida na votacao.</summary>
    public MaioriaProposicao MaioriaAtingida { get; }

    /// <summary>Cria um resultado aprovado com a maioria efetivamente atingida.</summary>
    /// <param name="maioriaAtingida">Maioria alcancada na votacao.</param>
    /// <returns>Resultado aprovado.</returns>
    public static ResultadoDeliberacao Aprovada(MaioriaProposicao maioriaAtingida)
        => new(aprovado: true, maioriaAtingida);

    /// <summary>Cria um resultado rejeitado (maioria nao alcancada).</summary>
    /// <returns>Resultado rejeitado.</returns>
    public static ResultadoDeliberacao Rejeitada()
        => new(aprovado: false, MaioriaProposicao.Simples);

    /// <summary>Verifica se o resultado satisfaz a maioria exigida pelo tipo da proposicao.</summary>
    /// <param name="exigida">Maioria exigida.</param>
    /// <returns><c>true</c> se aprovado e a maioria atingida for igual ou superior a exigida.</returns>
    public bool Satisfaz(MaioriaProposicao exigida)
        => Aprovado && MaioriaAtingida >= exigida;
}
