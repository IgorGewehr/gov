using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Comissoes;

/// <summary>Membro de comissao (projecao de leitura).</summary>
/// <param name="VereadorId">Vereador.</param>
/// <param name="Papel">Papel.</param>
/// <param name="Cargo">Cargo de direcao.</param>
public sealed record MembroComissaoDto(Guid VereadorId, string Papel, string Cargo);

/// <summary>Resumo de comissao para listagem.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nome">Nome.</param>
/// <param name="Tipo">Natureza.</param>
/// <param name="Situacao">Situacao.</param>
/// <param name="TotalMembros">Quantidade de membros.</param>
public sealed record ComissaoResumo(Guid Id, string Nome, string Tipo, string Situacao, int TotalMembros);

/// <summary>Detalhe de comissao com composicao.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nome">Nome.</param>
/// <param name="Tipo">Natureza.</param>
/// <param name="Situacao">Situacao.</param>
/// <param name="PresidenteVereadorId">Vereador presidente (se houver).</param>
/// <param name="Membros">Composicao.</param>
public sealed record ComissaoDetalhe(
    Guid Id,
    string Nome,
    string Tipo,
    string Situacao,
    Guid? PresidenteVereadorId,
    IReadOnlyList<MembroComissaoDto> Membros);

/// <summary>Lista as comissoes do tenant.</summary>
public sealed record ListarComissoesQuery : IQuery<IReadOnlyList<ComissaoResumo>>;

/// <summary>Handler da listagem de comissoes.</summary>
public sealed class ListarComissoesHandler(IComissaoRepository comissoes)
    : IQueryHandler<ListarComissoesQuery, IReadOnlyList<ComissaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ComissaoResumo>> Handle(ListarComissoesQuery request, CancellationToken cancellationToken)
    {
        var lista = await comissoes.ListarAsync(cancellationToken).ConfigureAwait(false);
        return lista
            .Select(comissao => new ComissaoResumo(
                comissao.Id.Value,
                comissao.Nome,
                comissao.Tipo.ToString(),
                comissao.Situacao.ToString(),
                comissao.Membros.Count))
            .ToList();
    }
}

/// <summary>Obtem o detalhe de uma comissao por identificador (com composicao), tenant-scoped.</summary>
/// <param name="ComissaoId">Identificador.</param>
public sealed record ObterComissaoPorIdQuery(Guid ComissaoId) : IQuery<ComissaoDetalhe?>;

/// <summary>Handler do detalhe de comissao.</summary>
public sealed class ObterComissaoPorIdHandler(IComissaoRepository comissoes)
    : IQueryHandler<ObterComissaoPorIdQuery, ComissaoDetalhe?>
{
    /// <inheritdoc />
    public async Task<ComissaoDetalhe?> Handle(ObterComissaoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comissao = await comissoes.ObterPorIdAsync(new ComissaoId(request.ComissaoId), cancellationToken).ConfigureAwait(false);
        if (comissao is null)
        {
            return null;
        }

        var membros = comissao.Membros
            .Select(membro => new MembroComissaoDto(membro.VereadorId.Value, membro.Papel.ToString(), membro.Cargo.ToString()))
            .ToList();

        return new ComissaoDetalhe(
            comissao.Id.Value,
            comissao.Nome,
            comissao.Tipo.ToString(),
            comissao.Situacao.ToString(),
            comissao.Presidente?.VereadorId.Value,
            membros);
    }
}
