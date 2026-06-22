namespace Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

/// <summary>
/// Infraestrutura fisica de uma escola, conforme os campos cadastrais exigidos pelo
/// EducaCenso/INEP: numero de salas de aula, dependencias gerais e itens de
/// acessibilidade (rampas, banheiros adaptados). Objeto de valor imutavel.
/// </summary>
/// <param name="NumeroSalas">Numero de salas de aula.</param>
/// <param name="NumeroDependencias">Numero de dependencias gerais (laboratorios, biblioteca, quadra etc.).</param>
/// <param name="PossuiAcessibilidade">Indica se a escola possui itens de acessibilidade.</param>
public readonly record struct Infraestrutura(
    int NumeroSalas,
    int NumeroDependencias,
    bool PossuiAcessibilidade)
{
    /// <summary>Cria uma infraestrutura validada (contagens nao negativas).</summary>
    /// <param name="numeroSalas">Numero de salas de aula (nao negativo).</param>
    /// <param name="numeroDependencias">Numero de dependencias gerais (nao negativo).</param>
    /// <param name="possuiAcessibilidade">Indica se ha itens de acessibilidade.</param>
    /// <returns>Instancia de <see cref="Infraestrutura"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se alguma contagem for negativa.</exception>
    public static Infraestrutura Criar(int numeroSalas, int numeroDependencias, bool possuiAcessibilidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numeroSalas);
        ArgumentOutOfRangeException.ThrowIfNegative(numeroDependencias);
        return new Infraestrutura(numeroSalas, numeroDependencias, possuiAcessibilidade);
    }
}
