using Tensorroot.Gov.BuildingBlocks.Application.Messaging;

namespace Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;

/// <summary>Entrada do dicionario/catalogo de datasets de dados abertos (LAI / e-MAG dados abertos).</summary>
/// <param name="Dataset">Identificador do dataset (slug, ex.: "despesas").</param>
/// <param name="Titulo">Titulo legivel.</param>
/// <param name="Descricao">Descricao do conteudo.</param>
/// <param name="Colunas">Nomes das colunas do CSV (dicionario).</param>
/// <param name="UrlCsv">Caminho relativo do CSV (resolvido contra o slug do ente).</param>
public sealed record DatasetCatalogoDto(string Dataset, string Titulo, string Descricao, IReadOnlyList<string> Colunas, string UrlCsv);

/// <summary>
/// Catalogo (dicionario) FIXO de datasets de dados abertos do portal. Deterministico, sem I/O — apenas
/// descreve o que pode ser baixado em CSV. Os dados em si vem do <c>IConsultaPublicaRepository</c>
/// (tenant-scoped) no momento do download. Mesma regra de mascaramento das telas (CPF mascarado).
/// </summary>
public static class CatalogoDadosAbertos
{
    /// <summary>Datasets disponiveis e suas colunas (dicionario de dados).</summary>
    public static IReadOnlyList<DatasetCatalogoDto> Datasets { get; } =
    [
        new("despesas", "Despesas publicas", "Empenho/liquidacao/pagamento por exercicio (LAI).",
            ["Exercicio", "Fase", "NumeroEmpenho", "Credor", "CredorDocumento", "FuncaoSubfuncao", "FonteRecurso", "Valor", "Data"], "despesas.csv"),
        new("receitas", "Receitas arrecadadas", "Receita por rubrica/fonte por exercicio.",
            ["Exercicio", "Rubrica", "FonteRecurso", "Valor", "Data"], "receitas.csv"),
        new("contratos", "Contratos", "Contratos celebrados/publicados (Lei 14.133/2021).",
            ["Exercicio", "NumeroContrato", "Fornecedor", "Objeto", "Valor", "Modalidade", "PNCP"], "contratos.csv"),
        new("folha", "Folha nominal", "Remuneracao por servidor (sem CPF/matricula — Dec. 7.724/2012).",
            ["Competencia", "Servidor", "Cargo", "Lotacao", "RemuneracaoBruta", "Descontos", "Liquido"], "folha.csv"),
        new("diarias", "Diarias", "Despesas com diarias (elemento 339014), derivadas das despesas.",
            ["Exercicio", "Credor", "FuncaoSubfuncao", "FonteRecurso", "Valor", "Data"], "diarias.csv"),
        new("repasses", "Repasses/transferencias", "Repasses a OSC/fundos (elementos 335043/444042), derivados das despesas.",
            ["Exercicio", "Beneficiario", "FuncaoSubfuncao", "FonteRecurso", "Valor", "Data"], "repasses.csv"),
    ];
}

/// <summary>PUBLICA: retorna o catalogo/dicionario de datasets (read-only, sem dado pessoal).</summary>
public sealed record ObterCatalogoDadosAbertosQuery : IQuery<IReadOnlyList<DatasetCatalogoDto>>;

/// <summary>Handler do catalogo de dados abertos.</summary>
public sealed class ObterCatalogoDadosAbertosHandler
    : IQueryHandler<ObterCatalogoDadosAbertosQuery, IReadOnlyList<DatasetCatalogoDto>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<DatasetCatalogoDto>> Handle(ObterCatalogoDadosAbertosQuery request, CancellationToken cancellationToken)
        => Task.FromResult(CatalogoDadosAbertos.Datasets);
}
