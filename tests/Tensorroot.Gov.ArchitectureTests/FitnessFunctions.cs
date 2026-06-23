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
        "PainelGestor", "Cidadao",
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

    [Fact] // LG-1: operacoes de TRILHA DE ACESSO a dado sigiloso (acesso/prontuario) NUNCA aceitam a
    // identidade de quem acessa via parametro do request — ela vem do principal (ICurrentUser). Em
    // outros comandos, "usuarioId" e o SUJEITO gerenciado (dado de negocio); aqui seria o AUTOR da
    // trilha, que so pode ser derivado do JWT, sob pena de trilha forjavel.
    public void TrilhaDeAcesso_naoDeveAceitar_identidadeDeQuemAcessa_doCliente()
    {
        var nomesProibidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "usuarioId", "userId", "usuarioAcessoId", "usuarioDoAcessoId",
        };

        var infratores = new List<string>();
        foreach (var assembly in AssembliesDeCamada("Application"))
        {
            foreach (var tipo in TiposCarregaveis(assembly).Where(EhRequestMediatR).Where(EhOperacaoDeAcessoSigiloso))
            {
                var parametros = tipo.GetConstructors()
                    .SelectMany(ctor => ctor.GetParameters())
                    .Select(p => p.Name ?? string.Empty);

                if (parametros.Any(nomesProibidos.Contains))
                {
                    infratores.Add(tipo.FullName ?? tipo.Name);
                }
            }
        }

        infratores.Should().BeEmpty(
            "a identidade de quem acessa o conteudo sigiloso deve vir de ICurrentUser, nunca do request (LG-1). " +
            $"Infratores: {string.Join(", ", infratores)}");
    }

    // Operacao de acesso a conteudo sigiloso (gera/registra trilha de acesso): nome contem
    // "Acesso" ou "Prontuario" (prontuario SUAS, historico clinico etc.). E a superficie do LG-1.
    private static bool EhOperacaoDeAcessoSigiloso(Type tipo)
    {
        var nome = tipo.Name;
        return nome.Contains("Acesso", StringComparison.OrdinalIgnoreCase)
            || nome.Contains("Prontuario", StringComparison.OrdinalIgnoreCase);
    }

    // Carrega os tipos de um assembly tolerando dependencias ausentes (mesma robustez do NetArchTest).
    private static IEnumerable<Type> TiposCarregaveis(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    // Um request MediatR e qualquer tipo que implemente IRequest/IRequest<T> (Command/Query do projeto).
    private static bool EhRequestMediatR(Type tipo)
        => !tipo.IsAbstract
        && !tipo.IsInterface
        && tipo.GetInterfaces().Any(i =>
            i == typeof(MediatR.IBaseRequest)
            || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)));

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
