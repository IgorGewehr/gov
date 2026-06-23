namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Common;

/// <summary>
/// Envelope de leitura paginada (navegabilidade): pagina os itens projetados e expoe o total
/// para o front-end montar a tabela (Table sticky) e a navegacao de paginas.
/// </summary>
/// <typeparam name="T">Tipo do item projetado.</typeparam>
/// <param name="Itens">Itens da pagina corrente (ja projetados).</param>
/// <param name="Total">Total de registros que atendem ao filtro (todas as paginas).</param>
/// <param name="Pagina">Numero da pagina corrente (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina aplicado.</param>
public sealed record ResultadoPaginado<T>(
    IReadOnlyList<T> Itens,
    int Total,
    int Pagina,
    int Tamanho);

/// <summary>Parametros e saneamento de paginacao das buscas de navegabilidade (Onda 0).</summary>
public static class Paginacao
{
    /// <summary>Tamanho de pagina padrao quando nao informado.</summary>
    public const int TamanhoPadrao = 20;

    /// <summary>Tamanho de pagina maximo aceito (protege contra varreduras abusivas).</summary>
    public const int TamanhoMaximo = 100;

    /// <summary>Normaliza pagina (>= 1) e tamanho (1..<see cref="TamanhoMaximo"/>, default <see cref="TamanhoPadrao"/>).</summary>
    /// <param name="pagina">Pagina informada (pode ser nula/invalida).</param>
    /// <param name="tamanho">Tamanho informado (pode ser nulo/invalido).</param>
    /// <returns>Par (pagina, tamanho) saneado.</returns>
    public static (int Pagina, int Tamanho) Sanear(int? pagina, int? tamanho)
    {
        var paginaSaneada = pagina is { } p && p >= 1 ? p : 1;
        var tamanhoSaneado = tamanho is { } t && t >= 1 ? Math.Min(t, TamanhoMaximo) : TamanhoPadrao;
        return (paginaSaneada, tamanhoSaneado);
    }
}
