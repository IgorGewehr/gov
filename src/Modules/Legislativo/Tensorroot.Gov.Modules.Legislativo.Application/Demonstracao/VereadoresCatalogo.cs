using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Demonstracao;

/// <summary>Definicao declarativa de um vereador do seed de demonstracao (sem TenantId).</summary>
/// <param name="NomeCivil">Nome civil.</param>
/// <param name="NomeParlamentar">Nome parlamentar.</param>
/// <param name="Partido">Sigla partidaria.</param>
/// <param name="CargoMesa">Cargo na Mesa Diretora.</param>
public sealed record VereadorSeed(string NomeCivil, string NomeParlamentar, string Partido, CargoMesa CargoMesa);

/// <summary>
/// Catalogo dos vereadores de demonstracao (legislatura 2025-2028): uma Camara ficticia com Mesa
/// Diretora completa (Presidente, Vice, 1.º e 2.º Secretarios) e bancada plural — material para uma
/// demo convincente de painel/ata. Nomes ficticios.
/// </summary>
public static class VereadoresCatalogo
{
    /// <summary>Ano de inicio da legislatura de demonstracao.</summary>
    public const int LegislaturaInicio = 2025;

    /// <summary>Ano de fim da legislatura de demonstracao.</summary>
    public const int LegislaturaFim = 2028;

    /// <summary>Os 9 vereadores nomeados da Camara de demonstracao.</summary>
    /// <returns>Definicoes de vereadores a semear.</returns>
    public static IReadOnlyList<VereadorSeed> Vereadores() =>
    [
        new("Ana Paula Rodrigues", "Ana Paula", "PSDB", CargoMesa.Presidente),
        new("Bruno Carvalho Lima", "Bruno Carvalho", "MDB", CargoMesa.VicePresidente),
        new("Carla Menezes Souza", "Carla Menezes", "PT", CargoMesa.PrimeiroSecretario),
        new("Diego Fernandes Alves", "Diego Fernandes", "PP", CargoMesa.SegundoSecretario),
        new("Eliane Tavares Pinto", "Eliane Tavares", "PL", CargoMesa.Nenhum),
        new("Fabio Ramos Oliveira", "Fabio Ramos", "PDT", CargoMesa.Nenhum),
        new("Gabriela Nunes Castro", "Gabriela Nunes", "PSB", CargoMesa.Nenhum),
        new("Henrique Barros Dias", "Henrique Barros", "REPUBLICANOS", CargoMesa.Nenhum),
        new("Isabela Moreira Cruz", "Isabela Moreira", "PSD", CargoMesa.Nenhum),
    ];
}
