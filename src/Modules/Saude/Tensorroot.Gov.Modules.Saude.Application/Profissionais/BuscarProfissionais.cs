using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Common;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;

namespace Tensorroot.Gov.Modules.Saude.Application.Profissionais;

/// <summary>
/// Lista/busca paginada de profissionais por nome/CPF, com filtros por CBO, estabelecimento e situacao
/// (navegabilidade). Tenant-scoped via Global Query Filter; read-only. CPF NAO e devolvido (LGPD).
/// </summary>
/// <param name="Termo">Termo livre (nome ou CPF); nulo lista tudo.</param>
/// <param name="Cbo">Filtro opcional por CBO (vinculo).</param>
/// <param name="EstabelecimentoId">Filtro opcional por estabelecimento (vinculo).</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarProfissionaisQuery(
    string? Termo,
    string? Cbo,
    Guid? EstabelecimentoId,
    SituacaoProfissional? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<ProfissionalItemLista>>;

/// <summary>Handler da busca paginada de profissionais.</summary>
public sealed class BuscarProfissionaisHandler(IProfissionalCadastroRepository profissionais)
    : IQueryHandler<BuscarProfissionaisQuery, ResultadoPaginado<ProfissionalItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<ProfissionalItemLista>> Handle(
        BuscarProfissionaisQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await profissionais
            .BuscarAsync(request.Termo, request.Cbo, request.EstabelecimentoId, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(profissional => new ProfissionalItemLista(
                profissional.Id.Value,
                profissional.Nome,
                profissional.Registro is { } registro ? registro.ToString() : null,
                profissional.Vinculos.Count(vinculo => !vinculo.Encerrado),
                profissional.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<ProfissionalItemLista>(projetados, total, pagina, tamanho);
    }
}
