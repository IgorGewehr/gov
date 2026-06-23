using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Application.Common;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>
/// Lista/busca paginada de alunos por nome/CPF (picker do front — fim do GUID digitado na matricula).
/// Tenant-scoped via Global Query Filter; read-only. Projecao minimizada (sem CPF na lista — LGPD).
/// </summary>
/// <param name="Termo">Termo livre (nome ou CPF); nulo lista tudo.</param>
/// <param name="Situacao">Filtro opcional por situacao do cadastro.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarAlunosQuery(
    string? Termo,
    SituacaoAluno? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<AlunoItemLista>>;

/// <summary>Handler da busca paginada de alunos.</summary>
public sealed class BuscarAlunosHandler(IAlunoRepository alunos)
    : IQueryHandler<BuscarAlunosQuery, ResultadoPaginado<AlunoItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<AlunoItemLista>> Handle(
        BuscarAlunosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await alunos
            .BuscarAsync(request.Termo, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens.Select(AlunoMapeamento.ParaItemLista).ToList();
        return new ResultadoPaginado<AlunoItemLista>(projetados, total, pagina, tamanho);
    }
}
