using FluentAssertions;
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// Fitness functions de MANUTENIBILIDADE: impedem god-files (arquivos gigantes) no código
/// de produção. Rodam no CI — a organização do código não pode regredir.
/// </summary>
public sealed class MaintainabilityTests
{
    private const int LimiteLinhas = 500;

    [Fact]
    public void Nenhum_arquivo_de_codigo_de_producao_deve_passar_do_limite_de_linhas()
    {
        var src = Path.Combine(LocalizarRepoRoot(), "src");
        var separador = Path.DirectorySeparatorChar;

        var grandes = Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(arquivo =>
                !arquivo.Contains($"{separador}obj{separador}", StringComparison.Ordinal) &&
                !arquivo.Contains($"{separador}bin{separador}", StringComparison.Ordinal) &&
                !arquivo.Contains($"{separador}Migrations{separador}", StringComparison.Ordinal) &&
                !arquivo.EndsWith(".Designer.cs", StringComparison.Ordinal) &&
                !arquivo.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .Select(arquivo => new { Nome = Path.GetFileName(arquivo), Linhas = File.ReadAllLines(arquivo).Length })
            .Where(item => item.Linhas > LimiteLinhas)
            .OrderByDescending(item => item.Linhas)
            .ToList();

        grandes.Should().BeEmpty(
            $"nenhum .cs de produção deve passar de {LimiteLinhas} linhas (manutenibilidade — sem god-files); refatore: " +
            string.Join(", ", grandes.Select(item => $"{item.Nome}={item.Linhas}")));
    }

    private static string LocalizarRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Tensorroot.Gov.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Raiz da solução (Tensorroot.Gov.sln) não localizada.");
    }
}
