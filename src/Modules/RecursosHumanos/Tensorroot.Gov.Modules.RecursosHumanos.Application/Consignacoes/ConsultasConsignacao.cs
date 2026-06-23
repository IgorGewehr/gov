using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>Limite/comprometido/disponivel de um balde de margem.</summary>
/// <param name="Grupo">Balde de margem (reserva legal).</param>
/// <param name="Limite">Teto consignavel do balde (base x percentual legal).</param>
/// <param name="Comprometido">Valor ja averbado no balde.</param>
/// <param name="Disponivel">Margem ainda disponivel no balde.</param>
public sealed record BaldeMargemDto(GrupoMargem Grupo, decimal Limite, decimal Comprometido, decimal Disponivel);

/// <summary>Margem consignavel de um servidor numa competencia (3 baldes).</summary>
/// <param name="ServidorId">Servidor.</param>
/// <param name="Competencia">Competencia (AAAA-MM).</param>
/// <param name="BaseDeCalculo">Base consignavel apurada da folha.</param>
/// <param name="Baldes">Limite/comprometido/disponivel por balde.</param>
public sealed record MargemConsignavelDto(
    Guid ServidorId,
    string Competencia,
    decimal BaseDeCalculo,
    IReadOnlyList<BaldeMargemDto> Baldes);

/// <summary>Consulta a margem consignavel de um servidor numa competencia (3 baldes: limite/comprometido/disponivel).</summary>
/// <param name="ServidorId">Servidor.</param>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
public sealed record ConsultarMargemQuery(Guid ServidorId, int Ano, int Mes) : IQuery<MargemConsignavelDto>;

/// <summary>Handler da consulta de margem consignavel.</summary>
public sealed class ConsultarMargemHandler(CalculadoraMargemConsignavel calculadoraMargem)
    : IQueryHandler<ConsultarMargemQuery, MargemConsignavelDto>
{
    /// <inheritdoc />
    public async Task<MargemConsignavelDto> Handle(ConsultarMargemQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var competencia = Competencia.De(request.Ano, request.Mes);
        var margem = await calculadoraMargem.CalcularAsync(request.ServidorId, competencia, cancellationToken).ConfigureAwait(false);

        var baldes = new[] { GrupoMargem.Geral, GrupoMargem.CartaoConsignado, GrupoMargem.CartaoBeneficio }
            .Select(g => new BaldeMargemDto(g, margem.Limite(g), margem.Comprometido(g), margem.Disponivel(g)))
            .ToList();

        return new MargemConsignavelDto(request.ServidorId, competencia.ToString(), margem.BaseDeCalculo, baldes);
    }
}

/// <summary>Projecao de leitura de um contrato de consignacao.</summary>
/// <param name="Id">Identificador do contrato.</param>
/// <param name="ServidorId">Servidor consignante.</param>
/// <param name="ConsignatariaId">Consignataria destinataria.</param>
/// <param name="CodigoRubrica">Codigo da rubrica consignavel.</param>
/// <param name="Categoria">Categoria (prioridade no corte).</param>
/// <param name="GrupoMargem">Balde de margem consumido.</param>
/// <param name="NumeroContratoExterno">Numero do contrato externo.</param>
/// <param name="ValorParcela">Valor mensal da parcela.</param>
/// <param name="QuantidadeParcelas">Quantidade total de parcelas.</param>
/// <param name="ParcelasPagas">Parcelas ja pagas.</param>
/// <param name="ParcelasRestantes">Parcelas restantes.</param>
/// <param name="DataAverbacao">Data da averbacao.</param>
/// <param name="Situacao">Situacao no ciclo de vida.</param>
public sealed record ContratoConsignacaoResumo(
    Guid Id,
    Guid ServidorId,
    Guid ConsignatariaId,
    string CodigoRubrica,
    CategoriaConsignavel Categoria,
    GrupoMargem GrupoMargem,
    string? NumeroContratoExterno,
    decimal ValorParcela,
    int QuantidadeParcelas,
    int ParcelasPagas,
    int ParcelasRestantes,
    DateOnly DataAverbacao,
    SituacaoConsignacao Situacao)
{
    /// <summary>Projeta o agregado para o resumo de leitura.</summary>
    /// <param name="c">Contrato de consignacao.</param>
    /// <returns>Resumo.</returns>
    public static ContratoConsignacaoResumo De(ContratoConsignacao c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new ContratoConsignacaoResumo(
            c.Id.Value,
            c.ServidorId.Value,
            c.ConsignatariaId.Value,
            c.CodigoRubrica,
            c.Categoria,
            c.GrupoMargem,
            c.NumeroContratoExterno,
            c.ValorParcela,
            c.QuantidadeParcelas,
            c.ParcelasPagas,
            c.ParcelasRestantes,
            c.DataAverbacao,
            c.Situacao);
    }
}

/// <summary>Lista as consignacoes (historico) de um servidor.</summary>
/// <param name="ServidorId">Servidor.</param>
public sealed record ListarConsignacoesDoServidorQuery(Guid ServidorId) : IQuery<IReadOnlyList<ContratoConsignacaoResumo>>;

/// <summary>Handler da listagem de consignacoes do servidor.</summary>
public sealed class ListarConsignacoesDoServidorHandler(IContratoConsignacaoRepository contratos)
    : IQueryHandler<ListarConsignacoesDoServidorQuery, IReadOnlyList<ContratoConsignacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContratoConsignacaoResumo>> Handle(
        ListarConsignacoesDoServidorQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await contratos.ListarPorServidorAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false);
        return itens.Select(ContratoConsignacaoResumo.De).ToList();
    }
}

/// <summary>Projecao de leitura de uma consignataria (cadastro mestre). CNPJ exibido sem mascara (dado cadastral, nao PII de pessoa).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Cnpj">CNPJ (14 digitos).</param>
/// <param name="RazaoSocial">Razao social.</param>
/// <param name="Tipo">Natureza.</param>
/// <param name="Situacao">Situacao (ativa/suspensa).</param>
public sealed record ConsignatariaResumo(
    Guid Id,
    string Cnpj,
    string RazaoSocial,
    TipoConsignataria Tipo,
    SituacaoConsignataria Situacao)
{
    /// <summary>Projeta o agregado para o resumo de leitura.</summary>
    /// <param name="c">Consignataria.</param>
    /// <returns>Resumo.</returns>
    public static ConsignatariaResumo De(Consignataria c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new ConsignatariaResumo(c.Id.Value, c.Cnpj.Digitos, c.RazaoSocial, c.Tipo, c.Situacao);
    }
}

/// <summary>Lista as consignatarias cadastradas no tenant.</summary>
public sealed record ListarConsignatariasQuery : IQuery<IReadOnlyList<ConsignatariaResumo>>;

/// <summary>Handler da listagem de consignatarias.</summary>
public sealed class ListarConsignatariasHandler(IConsignatariaRepository consignatarias)
    : IQueryHandler<ListarConsignatariasQuery, IReadOnlyList<ConsignatariaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsignatariaResumo>> Handle(
        ListarConsignatariasQuery request,
        CancellationToken cancellationToken)
    {
        var itens = await consignatarias.ListarAsync(cancellationToken).ConfigureAwait(false);
        return itens.Select(ConsignatariaResumo.De).ToList();
    }
}
