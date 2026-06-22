using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using FluentAssertions;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// Spec-Code Consistency Check (governança Rules-as-Code): valida que os manifestos
/// dos arquivos <c>*.rules.md</c> de cada módulo batem EXATAMENTE com o que existe no código
/// (comandos, consultas, eventos de domínio e eventos de integração) — nos dois sentidos.
/// Garante que a especificação é a fonte da verdade e que não há deriva (drift).
/// </summary>
public sealed class SpecCodeConsistency
{
    [Fact]
    public void Manifestos_dos_rules_md_devem_bater_com_o_codigo()
    {
        var modulosDir = LocalizarModulosDir();
        var arquivosRegras = Directory
            .EnumerateFiles(modulosDir, "*.rules.md", SearchOption.AllDirectories)
            .ToList();

        arquivosRegras.Should().NotBeEmpty("deve existir ao menos um *.rules.md (Sprint 1 Rules-as-Code)");

        var porModulo = new Dictionary<string, Manifesto>(StringComparer.Ordinal);
        foreach (var arquivo in arquivosRegras)
        {
            var modulo = ModuloDoCaminho(arquivo, modulosDir);
            var manifesto = ExtrairManifesto(File.ReadAllText(arquivo));
            if (!porModulo.TryGetValue(modulo, out var acumulado))
            {
                acumulado = new Manifesto();
                porModulo[modulo] = acumulado;
            }

            acumulado.Merge(manifesto);
        }

        foreach (var (modulo, regras) in porModulo)
        {
            var codigo = LerCodigo(modulo);

            // Módulo apenas ESPECIFICADO (rules.md sem código ainda) é pendente de geração — não falha.
            var temCodigo = codigo.Commands.Count > 0 || codigo.Queries.Count > 0 || codigo.DomainEvents.Count > 0
                || codigo.IntegrationPublished.Count > 0 || codigo.IntegrationConsumed.Count > 0;
            if (!temCodigo)
            {
                continue;
            }

            Normalizar(regras.Commands).Should().BeEquivalentTo(Normalizar(codigo.Commands),
                $"[{modulo}] os comandos do código devem bater com os manifestos dos *.rules.md");
            Normalizar(regras.Queries).Should().BeEquivalentTo(Normalizar(codigo.Queries),
                $"[{modulo}] as consultas devem bater");
            Normalizar(regras.DomainEvents).Should().BeEquivalentTo(Normalizar(codigo.DomainEvents),
                $"[{modulo}] os eventos de domínio devem bater");
            Normalizar(regras.IntegrationPublished).Should().BeEquivalentTo(Normalizar(codigo.IntegrationPublished),
                $"[{modulo}] os integration events PUBLICADOS devem bater");
            Normalizar(regras.IntegrationConsumed).Should().BeEquivalentTo(Normalizar(codigo.IntegrationConsumed),
                $"[{modulo}] os integration events CONSUMIDOS devem bater");
        }
    }

    private static List<string> Normalizar(IEnumerable<string> itens)
        => itens.Select(i => i.Trim()).Where(i => i.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(i => i, StringComparer.Ordinal).ToList();

    private static string LocalizarModulosDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Tensorroot.Gov.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Raiz da solução (Tensorroot.Gov.sln) não localizada.");
        }

        return Path.Combine(dir.FullName, "src", "Modules");
    }

    private static string ModuloDoCaminho(string arquivo, string modulosDir)
    {
        var relativo = Path.GetRelativePath(modulosDir, arquivo);
        return relativo.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private static Manifesto ExtrairManifesto(string conteudo)
    {
        var bloco = Regex.Match(conteudo, "<!--\\s*manifest(?<corpo>.*?)-->", RegexOptions.Singleline);
        var manifesto = new Manifesto();
        if (!bloco.Success)
        {
            return manifesto;
        }

        foreach (var linha in bloco.Groups["corpo"].Value.Split('\n'))
        {
            var separador = linha.IndexOf(':', StringComparison.Ordinal);
            if (separador <= 0)
            {
                continue;
            }

            var chave = linha[..separador].Trim();
            var valores = linha[(separador + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            manifesto.Adicionar(chave, valores);
        }

        return manifesto;
    }

    private static CodigoModulo LerCodigo(string modulo)
    {
        var assemblies = CarregarAssembliesDoModulo(modulo);
        var tipos = assemblies.SelectMany(GetTiposSeguro).ToList();
        var resultado = new CodigoModulo();

        foreach (var tipo in tipos)
        {
            if (tipo.IsInterface || tipo.IsAbstract)
            {
                continue;
            }

            if (typeof(IBaseCommand).IsAssignableFrom(tipo))
            {
                resultado.Commands.Add(RemoverSufixo(tipo.Name, "Command"));
            }

            if (tipo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
            {
                resultado.Queries.Add(RemoverSufixo(tipo.Name, "Query"));
            }

            if (typeof(IDomainEvent).IsAssignableFrom(tipo))
            {
                resultado.DomainEvents.Add(tipo.Name);
            }

            if (typeof(IIntegrationEvent).IsAssignableFrom(tipo)
                && tipo.Assembly.GetName().Name?.EndsWith($".Modules.{modulo}.Contracts", StringComparison.Ordinal) == true)
            {
                resultado.IntegrationPublished.Add(tipo.Name);
            }

            foreach (var iface in tipo.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                {
                    var evento = iface.GetGenericArguments()[0];
                    if (typeof(IIntegrationEvent).IsAssignableFrom(evento))
                    {
                        resultado.IntegrationConsumed.Add(evento.Name);
                    }
                }
            }
        }

        return resultado;
    }

    private static string RemoverSufixo(string nome, string sufixo)
        => nome.EndsWith(sufixo, StringComparison.Ordinal) ? nome[..^sufixo.Length] : nome;

    private static IEnumerable<Assembly> CarregarAssembliesDoModulo(string modulo)
    {
        foreach (var dll in Directory.EnumerateFiles(AppContext.BaseDirectory, $"Tensorroot.Gov.Modules.{modulo}.*.dll"))
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

    private static IEnumerable<Type> GetTiposSeguro(Assembly assembly)
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

    private sealed class Manifesto
    {
        public List<string> Commands { get; } = [];

        public List<string> Queries { get; } = [];

        public List<string> DomainEvents { get; } = [];

        public List<string> IntegrationPublished { get; } = [];

        public List<string> IntegrationConsumed { get; } = [];

        public void Adicionar(string chave, IEnumerable<string> valores)
        {
            var alvo = chave switch
            {
                "commands" => Commands,
                "queries" => Queries,
                "domainEvents" => DomainEvents,
                "integrationEventsPublished" => IntegrationPublished,
                "integrationEventsConsumed" => IntegrationConsumed,
                _ => null,
            };
            alvo?.AddRange(valores);
        }

        public void Merge(Manifesto outro)
        {
            Commands.AddRange(outro.Commands);
            Queries.AddRange(outro.Queries);
            DomainEvents.AddRange(outro.DomainEvents);
            IntegrationPublished.AddRange(outro.IntegrationPublished);
            IntegrationConsumed.AddRange(outro.IntegrationConsumed);
        }
    }

    private sealed class CodigoModulo
    {
        public List<string> Commands { get; } = [];

        public List<string> Queries { get; } = [];

        public List<string> DomainEvents { get; } = [];

        public List<string> IntegrationPublished { get; } = [];

        public List<string> IntegrationConsumed { get; } = [];
    }
}
