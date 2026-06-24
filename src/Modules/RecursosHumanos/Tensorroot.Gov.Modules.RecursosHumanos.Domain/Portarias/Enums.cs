namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

/// <summary>
/// Natureza do ato de pessoal formalizado por portaria. Determina o efeito esperado sobre o vinculo
/// do servidor (provimento, vacancia, designacao de funcao, concessao de licenca/beneficio etc.).
/// </summary>
public enum TipoPortaria
{
    /// <summary>Nomeacao/provimento em cargo (ato de admissao).</summary>
    Nomeacao = 1,

    /// <summary>Exoneracao/dispensa do cargo (ato de vacancia).</summary>
    Exoneracao = 2,

    /// <summary>Designacao para funcao gratificada/funcao de confianca (sem alterar o cargo).</summary>
    Designacao = 3,

    /// <summary>Concessao de licenca, afastamento ou beneficio (ferias, licenca-premio etc.).</summary>
    Concessao = 4,

    /// <summary>Outros atos de pessoal nao enquadrados nas demais naturezas.</summary>
    Outro = 5,
}

/// <summary>Estado (ciclo de vida) de uma portaria de pessoal.</summary>
public enum SituacaoPortaria
{
    /// <summary>Emitida e vigente (estado inicial).</summary>
    Emitida = 1,

    /// <summary>Revogada/tornada sem efeito (terminal); preserva a numeracao consumida.</summary>
    Revogada = 2,
}
