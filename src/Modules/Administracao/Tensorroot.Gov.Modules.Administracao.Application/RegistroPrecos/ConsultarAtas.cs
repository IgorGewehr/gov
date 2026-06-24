using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

namespace Tensorroot.Gov.Modules.Administracao.Application.RegistroPrecos;

/// <summary>Resumo de ata para listagem.</summary>
/// <param name="Id">Identificador da ata.</param>
/// <param name="Numero">Numero da ata.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Termo final da vigencia.</param>
/// <param name="QuantidadeItens">Quantidade de itens registrados.</param>
public sealed record AtaResumo(
    Guid Id,
    string Numero,
    string Situacao,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    int QuantidadeItens);

/// <summary>Item registrado com saldo, para o detalhe da ata.</summary>
/// <param name="ItemAtaId">Identificador do item na ata.</param>
/// <param name="ItemCatalogoId">Item de catalogo registrado.</param>
/// <param name="FornecedorBeneficiarioId">Fornecedor beneficiario.</param>
/// <param name="PrecoRegistrado">Preco unitario registrado.</param>
/// <param name="QuantidadeRegistrada">Quantidade maxima registrada.</param>
/// <param name="QuantidadeContratada">Quantidade ja contratada.</param>
/// <param name="SaldoDisponivel">Saldo ainda disponivel.</param>
public sealed record ItemAtaDetalhe(
    Guid ItemAtaId,
    Guid ItemCatalogoId,
    Guid FornecedorBeneficiarioId,
    decimal PrecoRegistrado,
    decimal QuantidadeRegistrada,
    decimal QuantidadeContratada,
    decimal SaldoDisponivel);

/// <summary>Adesao registrada, para o detalhe da ata.</summary>
/// <param name="ItemCatalogoId">Item objeto da adesao.</param>
/// <param name="OrgaoAderente">Orgao aderente.</param>
/// <param name="Quantidade">Quantidade aderida.</param>
/// <param name="Data">Data da adesao.</param>
public sealed record AdesaoDetalhe(Guid ItemCatalogoId, string OrgaoAderente, decimal Quantidade, DateOnly Data);

/// <summary>Detalhe completo de uma ata (itens, saldos e adesoes).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Numero da ata.</param>
/// <param name="LicitacaoId">Licitacao SRP de origem.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Termo final da vigencia.</param>
/// <param name="Itens">Itens registrados com saldos.</param>
/// <param name="Adesoes">Adesoes (carona) registradas.</param>
public sealed record AtaDetalhe(
    Guid Id,
    string Numero,
    Guid? LicitacaoId,
    string Situacao,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    IReadOnlyList<ItemAtaDetalhe> Itens,
    IReadOnlyList<AdesaoDetalhe> Adesoes);

/// <summary>Lista as atas do tenant, com filtro opcional por situacao.</summary>
/// <param name="Situacao">Situacao a filtrar (opcional).</param>
public sealed record ListarAtasQuery(SituacaoAta? Situacao) : IQuery<IReadOnlyList<AtaResumo>>;

/// <summary>Obtem o detalhe de uma ata por identificador.</summary>
/// <param name="AtaId">Identificador da ata.</param>
public sealed record ObterAtaPorIdQuery(Guid AtaId) : IQuery<AtaDetalhe?>;

/// <summary>Handler da listagem de atas.</summary>
public sealed class ListarAtasHandler(IAtaRepository atas)
    : IQueryHandler<ListarAtasQuery, IReadOnlyList<AtaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AtaResumo>> Handle(ListarAtasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await atas.ListarAsync(request.Situacao, cancellationToken).ConfigureAwait(false);
        return itens
            .Select(a => new AtaResumo(
                a.Id.Value,
                a.Numero,
                a.Situacao.ToString(),
                a.VigenciaInicio,
                a.VigenciaFim,
                a.Itens.Count))
            .ToList();
    }
}

/// <summary>Handler do detalhe de ata.</summary>
public sealed class ObterAtaPorIdHandler(IAtaRepository atas)
    : IQueryHandler<ObterAtaPorIdQuery, AtaDetalhe?>
{
    /// <inheritdoc />
    public async Task<AtaDetalhe?> Handle(ObterAtaPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ata = await atas.ObterPorIdAsync(new AtaId(request.AtaId), cancellationToken).ConfigureAwait(false);
        if (ata is null)
        {
            return null;
        }

        return new AtaDetalhe(
            ata.Id.Value,
            ata.Numero,
            ata.LicitacaoId,
            ata.Situacao.ToString(),
            ata.VigenciaInicio,
            ata.VigenciaFim,
            ata.Itens.Select(i => new ItemAtaDetalhe(
                i.Id.Value,
                i.ItemCatalogoId.Value,
                i.FornecedorBeneficiarioId,
                i.PrecoRegistrado.Valor,
                i.QuantidadeRegistrada,
                i.QuantidadeContratada,
                i.SaldoDisponivel)).ToList(),
            ata.Adesoes.Select(a => new AdesaoDetalhe(
                a.ItemCatalogoId.Value,
                a.OrgaoAderente,
                a.Quantidade,
                a.Data)).ToList());
    }
}
