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

/// <summary>Item registrado com saldos (registrado e de adesao), para o detalhe da ata.</summary>
/// <param name="ItemAtaId">Identificador do item na ata.</param>
/// <param name="ItemCatalogoId">Item de catalogo registrado.</param>
/// <param name="FornecedorBeneficiarioId">Fornecedor beneficiario.</param>
/// <param name="PrecoRegistrado">Preco unitario registrado.</param>
/// <param name="QuantidadeRegistrada">Quantidade maxima registrada (gerenciador + participantes).</param>
/// <param name="QuantidadeContratada">Quantidade ja contratada (consumo direto).</param>
/// <param name="SaldoDisponivel">Saldo registrado ainda disponivel.</param>
/// <param name="QuantidadeAderida">Quantidade ja aderida por orgaos nao participantes (carona).</param>
/// <param name="LimiteAdesaoPorOrgao">Teto de adesao por orgao aderente (50% do registrado — art. 86, §4º).</param>
/// <param name="LimiteTotalAdesao">Teto total de adesoes (200% do registrado — art. 86, §5º).</param>
/// <param name="SaldoAdesaoDisponivel">Saldo de adesao ainda disponivel.</param>
public sealed record ItemAtaDetalhe(
    Guid ItemAtaId,
    Guid ItemCatalogoId,
    Guid FornecedorBeneficiarioId,
    decimal PrecoRegistrado,
    decimal QuantidadeRegistrada,
    decimal QuantidadeContratada,
    decimal SaldoDisponivel,
    decimal QuantidadeAderida,
    decimal LimiteAdesaoPorOrgao,
    decimal LimiteTotalAdesao,
    decimal SaldoAdesaoDisponivel);

/// <summary>Orgao gerenciador/participante da ata, para o detalhe.</summary>
/// <param name="CnpjOrgao">CNPJ do orgao.</param>
/// <param name="NomeOrgao">Nome do orgao.</param>
/// <param name="Tipo">Papel no SRP (Gerenciador|Participante).</param>
public sealed record ParticipanteAtaDetalhe(string CnpjOrgao, string NomeOrgao, string Tipo);

/// <summary>Adesao registrada, para o detalhe da ata.</summary>
/// <param name="ItemCatalogoId">Item objeto da adesao.</param>
/// <param name="CnpjOrgaoAderente">CNPJ do orgao aderente.</param>
/// <param name="OrgaoAderente">Nome do orgao aderente.</param>
/// <param name="Quantidade">Quantidade aderida.</param>
/// <param name="Data">Data da adesao.</param>
public sealed record AdesaoDetalhe(
    Guid ItemCatalogoId,
    string CnpjOrgaoAderente,
    string OrgaoAderente,
    decimal Quantidade,
    DateOnly Data);

/// <summary>Detalhe completo de uma ata (orgaos, itens, saldos e adesoes).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Numero da ata.</param>
/// <param name="LicitacaoId">Licitacao SRP de origem.</param>
/// <param name="CnpjOrgaoGerenciador">CNPJ do orgao gerenciador.</param>
/// <param name="NomeOrgaoGerenciador">Nome do orgao gerenciador.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Termo final da vigencia.</param>
/// <param name="Prorrogada">Indica se a ata ja foi prorrogada (art. 84).</param>
/// <param name="Participantes">Orgaos gerenciador e participantes.</param>
/// <param name="Itens">Itens registrados com saldos.</param>
/// <param name="Adesoes">Adesoes (carona) registradas.</param>
public sealed record AtaDetalhe(
    Guid Id,
    string Numero,
    Guid? LicitacaoId,
    string CnpjOrgaoGerenciador,
    string NomeOrgaoGerenciador,
    string Situacao,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    bool Prorrogada,
    IReadOnlyList<ParticipanteAtaDetalhe> Participantes,
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
            ata.CnpjOrgaoGerenciador,
            ata.NomeOrgaoGerenciador,
            ata.Situacao.ToString(),
            ata.VigenciaInicio,
            ata.VigenciaFim,
            ata.Prorrogada,
            ata.Participantes.Select(p => new ParticipanteAtaDetalhe(
                p.CnpjOrgao,
                p.NomeOrgao,
                p.Tipo.ToString())).ToList(),
            ata.Itens.Select(i => new ItemAtaDetalhe(
                i.Id.Value,
                i.ItemCatalogoId.Value,
                i.FornecedorBeneficiarioId,
                i.PrecoRegistrado.Valor,
                i.QuantidadeRegistrada,
                i.QuantidadeContratada,
                i.SaldoDisponivel,
                i.QuantidadeAderida,
                i.LimiteAdesaoPorOrgao,
                i.LimiteTotalAdesao,
                i.SaldoAdesaoDisponivel)).ToList(),
            ata.Adesoes.Select(a => new AdesaoDetalhe(
                a.ItemCatalogoId.Value,
                a.CnpjOrgaoAderente,
                a.OrgaoAderente,
                a.Quantidade,
                a.Data)).ToList());
    }
}
