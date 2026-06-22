using System.Reflection;
using System.Runtime.Loader;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;

namespace Tensorroot.Gov.ApiHost.Modularity;

/// <summary>
/// Descobre as implementações de <see cref="IModule"/> presentes no diretório da aplicação.
/// Cada módulo declara sua própria ativação; o licenciamento por tenant é aplicado adiante.
/// </summary>
internal static class ModuleRegistry
{
    /// <summary>Carrega os assemblies de módulo e instancia todos os <see cref="IModule"/> encontrados.</summary>
    /// <returns>Lista ordenada (por nome) de módulos descobertos.</returns>
    public static IReadOnlyList<IModule> Discover()
    {
        LoadModuleAssemblies();

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.FullName?.StartsWith("Tensorroot.Gov", StringComparison.Ordinal) == true)
            .SelectMany(GetLoadableTypes)
            .Where(type => typeof(IModule).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false })
            .Select(type => (IModule)Activator.CreateInstance(type)!)
            .OrderBy(module => module.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static void LoadModuleAssemblies()
    {
        foreach (var dll in Directory.EnumerateFiles(AppContext.BaseDirectory, "Tensorroot.Gov.Modules.*.dll"))
        {
            try
            {
                AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            }
            catch (BadImageFormatException)
            {
                // Arquivo não gerenciado/ inválido: ignorar.
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }
}
