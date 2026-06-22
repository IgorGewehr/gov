namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Hipotese legal (LGPD) que autoriza o tratamento/acesso a um dado pessoal — exigida pela trilha
/// de leitura de recursos sensiveis (LG-2/LG-A2). Para dado pessoal SENSIVEL (saude, assistencia
/// social), as hipoteses vivem no art. 11; para dado pessoal comum, no art. 7. O valor e gravado
/// estruturado na trilha de acesso para accountability perante ANPD/TCE.
/// </summary>
public enum BaseLegalLgpd
{
    /// <summary>Tutela da saude, em procedimento por profissional/servico de saude (art. 11, II, "f").</summary>
    TutelaDaSaude = 1,

    /// <summary>Execucao de politicas publicas previstas em lei (art. 11, II, "b"; art. 23).</summary>
    PoliticaPublica = 2,

    /// <summary>Cumprimento de obrigacao legal/regulatoria pelo controlador (art. 7, II; art. 11, II, "a").</summary>
    ObrigacaoLegal = 3,

    /// <summary>Exercicio regular de direitos em processo (art. 7, VI; art. 11, II, "d").</summary>
    ExercicioDeDireitos = 4,

    /// <summary>Consentimento especifico e destacado do titular (art. 7, I; art. 11, I).</summary>
    Consentimento = 5,
}
