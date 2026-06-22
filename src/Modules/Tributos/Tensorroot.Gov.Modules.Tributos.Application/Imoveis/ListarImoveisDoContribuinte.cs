using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

namespace Tensorroot.Gov.Modules.Tributos.Application.Imoveis;

/// <summary>Resumo de um imóvel para leitura.</summary>
/// <param name="Id">Identificador do imóvel.</param>
/// <param name="InscricaoMunicipal">Inscrição municipal cadastral.</param>
/// <param name="CibCodigo">Código CIB, se houver.</param>
/// <param name="Logradouro">Logradouro.</param>
/// <param name="ZonaFiscal">Zona fiscal.</param>
/// <param name="AreaTerreno">Área do terreno (m²).</param>
/// <param name="AreaConstruida">Área construída (m²).</param>
/// <param name="TipoUso">Tipo de uso.</param>
/// <param name="Ativo">Se está ativo no cadastro.</param>
public sealed record ImovelResumo(
    Guid Id,
    string InscricaoMunicipal,
    string? CibCodigo,
    string Logradouro,
    string ZonaFiscal,
    decimal AreaTerreno,
    decimal AreaConstruida,
    string TipoUso,
    bool Ativo);

/// <summary>Lista os imóveis de um contribuinte (proprietário).</summary>
/// <param name="ContribuinteId">Contribuinte proprietário.</param>
public sealed record ListarImoveisDoContribuinteQuery(Guid ContribuinteId)
    : IQuery<IReadOnlyList<ImovelResumo>>;

/// <summary>Handler da consulta de imóveis do contribuinte.</summary>
public sealed class ListarImoveisDoContribuinteHandler(IImovelRepository imoveis)
    : IQueryHandler<ListarImoveisDoContribuinteQuery, IReadOnlyList<ImovelResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ImovelResumo>> Handle(
        ListarImoveisDoContribuinteQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await imoveis
            .ListarPorProprietarioAsync(new ContribuinteId(request.ContribuinteId), cancellationToken)
            .ConfigureAwait(false);

        return encontrados
            .Select(imovel => new ImovelResumo(
                imovel.Id.Value,
                imovel.Identificacao.InscricaoMunicipal,
                imovel.Identificacao.CibCodigo,
                imovel.Endereco.Logradouro,
                imovel.Endereco.ZonaFiscal,
                imovel.Caracteristicas.AreaTerreno,
                imovel.Caracteristicas.AreaConstruida,
                imovel.Caracteristicas.TipoUso.ToString(),
                imovel.Ativo))
            .ToList();
    }
}
