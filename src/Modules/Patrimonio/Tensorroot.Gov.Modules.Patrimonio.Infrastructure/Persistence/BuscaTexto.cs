namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence;

/// <summary>
/// Utilitarios de busca textual translatavel em SQL (navegabilidade — Onda 0). As colunas de
/// descricao/codigo/placa/tombamento/RENAVAM sao strings reais; a busca usa LIKE com os curingas
/// do termo escapados (literais). A insensibilidade a caixa vem da collation do banco (SQLite e
/// SQL Server tratam LIKE como case-insensitive para ASCII por padrao) — sem coluna-sombra nem
/// dependencia da cultura corrente do processo.
/// </summary>
internal static class BuscaTexto
{
    /// <summary>Monta o padrao LIKE "%termo%" com os curingas do termo escapados (literais).</summary>
    /// <param name="termo">Termo livre informado pelo usuario.</param>
    /// <returns>Padrao pronto para <c>EF.Functions.Like(..., padrao, "\\")</c>.</returns>
    public static string MontarPadraoContains(string termo)
        => "%" + Escapar(termo.Trim()) + "%";

    // Escapa os curingas do LIKE (\, %, _) para tratar o termo do usuario como literal.
    private static string Escapar(string termo)
        => termo.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
