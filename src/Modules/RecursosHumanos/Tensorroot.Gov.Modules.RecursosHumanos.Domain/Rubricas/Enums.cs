namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;

/// <summary>
/// Natureza de uma rubrica (verba) parametrizavel da folha, alinhada ao campo tpRubr do eSocial
/// S-1010 (1=provento, 2=desconto, 3/4=informativo).
/// </summary>
public enum NaturezaRubrica
{
    /// <summary>Provento (verba crediticia que aumenta a remuneracao). eSocial tpRubr=1.</summary>
    Provento = 1,

    /// <summary>Desconto (verba debitoria que reduz a remuneracao). eSocial tpRubr=2.</summary>
    Desconto = 2,

    /// <summary>Informativa (nao integra liquido; ex.: base/referencia). eSocial tpRubr=3.</summary>
    Informativa = 3,

    /// <summary>Informativa dedutora (nao integra liquido). eSocial tpRubr=4.</summary>
    InformativaDedutora = 4,
}
