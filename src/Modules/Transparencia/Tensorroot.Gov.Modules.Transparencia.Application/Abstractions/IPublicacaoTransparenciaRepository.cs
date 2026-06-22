using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Item já materializado na transparência (consolidação de dados consumidos de outros módulos via
/// Integration Events). Como o módulo é CONSUMIDOR, a remessa é montada a partir destes itens — o
/// agregado <see cref="RemessaTce"/> não materializa fontes primárias (I-13).
/// </summary>
/// <param name="Tipo">Tipo de registro do leiaute correspondente ao item.</param>
/// <param name="Conteudo">Conteúdo estruturado do item (linha do leiaute).</param>
public sealed record ItemConsolidado(string Tipo, string Conteudo);

/// <summary>
/// Uma LINHA estruturada de um arquivo da remessa, já mapeada para os campos do leiaute (valores por nome
/// de campo). O <see cref="EmissorRegistroSiapc"/> a serializa em largura fixa. Cada linha refere-se a um
/// registro/arquivo (<paramref name="NomeArquivo"/>) do leiaute resolvido.
/// </summary>
/// <param name="NomeArquivo">Arquivo físico ao qual a linha pertence (ex.: "EMPENHO.TXT").</param>
/// <param name="Valores">Valores por nome de campo (casam com a grade do registro).</param>
public sealed record LinhaConsolidada(string NomeArquivo, IReadOnlyList<ValorCampo> Valores);

/// <summary>
/// Porta de leitura dos itens consolidados em <c>PublicacaoTransparencia</c> para um período/tenant,
/// usados na montagem do pacote da remessa.
/// </summary>
public interface IPublicacaoTransparenciaRepository
{
    /// <summary>Obtém os itens consolidados do período no tenant atual.</summary>
    /// <param name="periodo">Período (competência/exercício) da remessa.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Itens consolidados do período (vazio se não houver).</returns>
    Task<IReadOnlyList<ItemConsolidado>> ObterItensConsolidadosAsync(Periodo periodo, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém as linhas consolidadas do período já mapeadas para os campos do leiaute (uma por
    /// registro/arquivo), prontas para emissão posicional.
    /// </summary>
    /// <param name="periodo">Período (competência/exercício) da remessa.</param>
    /// <param name="leiaute">Leiaute resolvido cujo conjunto de arquivos será alimentado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linhas consolidadas (vazio se não houver).</returns>
    Task<IReadOnlyList<LinhaConsolidada>> ObterLinhasConsolidadasAsync(
        Periodo periodo,
        LeiauteSiapc leiaute,
        CancellationToken cancellationToken);
}
