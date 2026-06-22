using System.Reflection;
using System.Runtime.Loader;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// Fitness Functions: testes de arquitetura que blindam a Clean Architecture e o
/// isolamento entre Bounded Contexts (constituição do Tensorroot.Gov, §2 e §12).
/// </summary>
public sealed class FitnessFunctions
{
    private static readonly string[] Modulos =
    [
        "Administracao", "Financas", "Tributos", "RecursosHumanos", "Patrimonio",
        "Protocolo", "Saude", "Educacao", "AssistenciaSocial", "Legislativo", "Transparencia",
    ];

    [Fact]
    public void Dominio_naoDeveDependerDe_Infraestrutura_EFCore_ou_AspNet()
    {
        foreach (var assembly in AssembliesDeCamada("Domain"))
        {
            var resultado = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.AspNetCore",
                    "Tensorroot.Gov.BuildingBlocks.Infrastructure")
                .GetResult();

            resultado.IsSuccessful.Should().BeTrue(Motivo(assembly, resultado));
        }
    }

    [Fact]
    public void Aplicacao_naoDeveDependerDe_Infraestrutura_ou_EFCore()
    {
        foreach (var assembly in AssembliesDeCamada("Application"))
        {
            var resultado = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.EntityFrameworkCore",
                    "Tensorroot.Gov.BuildingBlocks.Infrastructure")
                .GetResult();

            resultado.IsSuccessful.Should().BeTrue(Motivo(assembly, resultado));
        }
    }

    [Fact]
    public void Modulos_soPodemReferenciar_ContractsDeOutrosModulos()
    {
        foreach (var (modulo, assembly) in AssembliesDosModulos())
        {
            var internosProibidos = Modulos
                .Where(outro => !string.Equals(outro, modulo, StringComparison.Ordinal))
                .SelectMany(outro => new[]
                {
                    $"Tensorroot.Gov.Modules.{outro}.Domain",
                    $"Tensorroot.Gov.Modules.{outro}.Application",
                    $"Tensorroot.Gov.Modules.{outro}.Infrastructure",
                })
                .ToArray();

            var resultado = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(internosProibidos)
                .GetResult();

            resultado.IsSuccessful.Should().BeTrue(Motivo(assembly, resultado));
        }
    }

    [Fact]
    public void SharedKernel_naoDeveDependerDe_EFCore_nem_AspNet()
    {
        var assembly = typeof(Tensorroot.Gov.SharedKernel.AssemblyReference).Assembly;
        var resultado = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(Motivo(assembly, resultado));
    }

    private static string Motivo(Assembly assembly, TestResult resultado)
    {
        var tipos = resultado.FailingTypeNames ?? Enumerable.Empty<string>();
        return $"{assembly.GetName().Name} viola a regra de arquitetura. Tipos infratores: {string.Join(", ", tipos)}";
    }

    private static List<Assembly> AssembliesDeCamada(string camada)
        => Carregar($"Tensorroot.Gov.Modules.*.{camada}.dll").ToList();

    private static List<(string Modulo, Assembly Assembly)> AssembliesDosModulos()
    {
        var lista = new List<(string, Assembly)>();
        foreach (var assembly in Carregar("Tensorroot.Gov.Modules.*.dll"))
        {
            var nome = assembly.GetName().Name ?? string.Empty;
            var modulo = Modulos.FirstOrDefault(m => nome.StartsWith($"Tensorroot.Gov.Modules.{m}.", StringComparison.Ordinal));
            if (modulo is not null)
            {
                lista.Add((modulo, assembly));
            }
        }

        return lista;
    }

    private static IEnumerable<Assembly> Carregar(string padrao)
    {
        foreach (var dll in Directory.EnumerateFiles(AppContext.BaseDirectory, padrao))
        {
            Assembly? assembly = null;
            try
            {
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            }
            catch (BadImageFormatException)
            {
                assembly = null;
            }

            if (assembly is not null)
            {
                yield return assembly;
            }
        }
    }
}
