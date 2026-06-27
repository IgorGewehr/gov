using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Credenciamentos;

/// <summary>Item credenciavel (objeto + preco fixado).</summary>
/// <param name="ItemId">Identificador do item.</param>
/// <param name="Numero">Numero sequencial.</param>
/// <param name="ItemCatalogoId">Item de catalogo (opcional).</param>
/// <param name="Descricao">Descricao do objeto.</param>
/// <param name="UnidadeMedida">Unidade de prestacao/medida.</param>
/// <param name="PrecoFixado">Preco fixado pela Administracao.</param>
public sealed record ItemCredenciamentoDetalhe(
    Guid ItemId,
    int Numero,
    Guid? ItemCatalogoId,
    string Descricao,
    string UnidadeMedida,
    decimal PrecoFixado);

/// <summary>Inscricao/credenciado no rol.</summary>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
/// <param name="FornecedorId">Fornecedor interessado.</param>
/// <param name="DataInscricao">Data de protocolo.</param>
/// <param name="Situacao">Situacao da inscricao.</param>
/// <param name="DataCredenciamento">Data de deferimento, quando houver.</param>
/// <param name="DataDescredenciamento">Data de descredenciamento, quando houver.</param>
/// <param name="Motivo">Motivacao do ultimo ato.</param>
public sealed record CredenciadoDetalhe(
    Guid CredenciadoId,
    Guid FornecedorId,
    DateOnly DataInscricao,
    string Situacao,
    DateOnly? DataCredenciamento,
    DateOnly? DataDescredenciamento,
    string? Motivo);

/// <summary>Detalhe completo do credenciamento.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Objeto">Objeto.</param>
/// <param name="Hipotese">Hipotese autorizadora (art. 79, I a III).</param>
/// <param name="Situacao">Situacao do edital.</param>
/// <param name="NumeroEdital">Numero do edital de chamamento.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="FundamentacaoLegal">Amparo legal.</param>
/// <param name="NumeroPncp">Numero de controle no PNCP.</param>
/// <param name="QuantidadeCredenciadosAptos">Quantidade de credenciados vigentes.</param>
/// <param name="Itens">Itens credenciaveis.</param>
/// <param name="Credenciados">Rol de inscricoes.</param>
public sealed record CredenciamentoDetalhe(
    Guid Id,
    string Objeto,
    string Hipotese,
    string Situacao,
    string? NumeroEdital,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    string FundamentacaoLegal,
    string? NumeroPncp,
    int QuantidadeCredenciadosAptos,
    IReadOnlyList<ItemCredenciamentoDetalhe> Itens,
    IReadOnlyList<CredenciadoDetalhe> Credenciados);

/// <summary>Resumo do credenciamento, para listagem.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Objeto">Objeto.</param>
/// <param name="Hipotese">Hipotese autorizadora.</param>
/// <param name="Situacao">Situacao do edital.</param>
/// <param name="NumeroEdital">Numero do edital.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="QuantidadeCredenciadosAptos">Quantidade de credenciados vigentes.</param>
public sealed record CredenciamentoResumo(
    Guid Id,
    string Objeto,
    string Hipotese,
    string Situacao,
    string? NumeroEdital,
    DateOnly VigenciaFim,
    int QuantidadeCredenciadosAptos);

/// <summary>Obtem o detalhe de um credenciamento (itens + rol).</summary>
/// <param name="CredenciamentoId">Identificador.</param>
public sealed record ObterCredenciamentoPorIdQuery(Guid CredenciamentoId) : IQuery<CredenciamentoDetalhe?>;

/// <summary>Lista credenciamentos por situacao (ou todos).</summary>
/// <param name="Situacao">Filtro de situacao (opcional).</param>
public sealed record ListarCredenciamentosQuery(SituacaoCredenciamento? Situacao) : IQuery<IReadOnlyList<CredenciamentoResumo>>;

/// <summary>Handler do detalhe do credenciamento.</summary>
public sealed class ObterCredenciamentoPorIdHandler(ICredenciamentoRepository credenciamentos)
    : IQueryHandler<ObterCredenciamentoPorIdQuery, CredenciamentoDetalhe?>
{
    /// <inheritdoc />
    public async Task<CredenciamentoDetalhe?> Handle(ObterCredenciamentoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var c = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false);
        if (c is null)
        {
            return null;
        }

        return new CredenciamentoDetalhe(
            c.Id.Value,
            c.Objeto,
            c.Hipotese.ToString(),
            c.Situacao.ToString(),
            c.NumeroEdital,
            c.VigenciaInicio,
            c.VigenciaFim,
            c.FundamentacaoLegal,
            c.NumeroPncp,
            c.QuantidadeCredenciadosAptos,
            c.Itens
                .OrderBy(i => i.Numero)
                .Select(i => new ItemCredenciamentoDetalhe(i.Id.Value, i.Numero, i.ItemCatalogoId, i.Descricao, i.UnidadeMedida, i.PrecoFixado.Valor))
                .ToList(),
            c.Credenciados
                .OrderBy(cr => cr.DataInscricao)
                .Select(cr => new CredenciadoDetalhe(
                    cr.Id.Value,
                    cr.FornecedorId,
                    cr.DataInscricao,
                    cr.Situacao.ToString(),
                    cr.DataCredenciamento,
                    cr.DataDescredenciamento,
                    cr.Motivo))
                .ToList());
    }
}

/// <summary>Handler da listagem de credenciamentos.</summary>
public sealed class ListarCredenciamentosHandler(ICredenciamentoRepository credenciamentos)
    : IQueryHandler<ListarCredenciamentosQuery, IReadOnlyList<CredenciamentoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CredenciamentoResumo>> Handle(ListarCredenciamentosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lista = await credenciamentos.ListarAsync(request.Situacao, cancellationToken).ConfigureAwait(false);
        return lista
            .Select(c => new CredenciamentoResumo(
                c.Id.Value,
                c.Objeto,
                c.Hipotese.ToString(),
                c.Situacao.ToString(),
                c.NumeroEdital,
                c.VigenciaFim,
                c.QuantidadeCredenciadosAptos))
            .ToList();
    }
}
