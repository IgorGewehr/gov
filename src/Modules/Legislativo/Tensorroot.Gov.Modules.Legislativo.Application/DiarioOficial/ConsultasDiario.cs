using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

namespace Tensorroot.Gov.Modules.Legislativo.Application.DiarioOficial;

/// <summary>Resumo de uma edicao do Diario.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Numero.</param>
/// <param name="Ano">Ano.</param>
/// <param name="Situacao">Situacao.</param>
/// <param name="DataPublicacao">Data de publicacao (se publicada).</param>
/// <param name="TotalMaterias">Quantidade de materias.</param>
public sealed record EdicaoResumo(Guid Id, int Numero, int Ano, string Situacao, DateTimeOffset? DataPublicacao, int TotalMaterias);

/// <summary>Materia de uma edicao (projecao de leitura).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Tipo">Especie.</param>
/// <param name="Titulo">Titulo.</param>
/// <param name="Conteudo">Conteudo bruto (se houver).</param>
/// <param name="ReferenciaId">Referencia a entidade de origem (se houver).</param>
/// <param name="Ordem">Ordem na edicao.</param>
public sealed record MateriaDto(Guid Id, string Tipo, string Titulo, string? Conteudo, Guid? ReferenciaId, int Ordem);

/// <summary>Detalhe de uma edicao com suas materias.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Numero.</param>
/// <param name="Ano">Ano.</param>
/// <param name="Situacao">Situacao.</param>
/// <param name="DataPublicacao">Data de publicacao (se publicada).</param>
/// <param name="EdicaoOriginalId">Edicao original (se esta for retificacao).</param>
/// <param name="Materias">Materias da edicao.</param>
public sealed record EdicaoDetalhe(
    Guid Id,
    int Numero,
    int Ano,
    string Situacao,
    DateTimeOffset? DataPublicacao,
    Guid? EdicaoOriginalId,
    IReadOnlyList<MateriaDto> Materias);

/// <summary>Pagina de edicoes.</summary>
/// <param name="Itens">Itens.</param>
/// <param name="Total">Total.</param>
/// <param name="Pagina">Pagina.</param>
/// <param name="Tamanho">Tamanho.</param>
public sealed record PaginaEdicoes(IReadOnlyList<EdicaoResumo> Itens, int Total, int Pagina, int Tamanho);

/// <summary>Lista as edicoes do tenant (filtros por ano/situacao), paginadas (backoffice).</summary>
/// <param name="Ano">Ano (opcional).</param>
/// <param name="Situacao">Situacao (<see cref="SituacaoEdicao"/>) opcional.</param>
/// <param name="Pagina">Pagina (default 1).</param>
/// <param name="Tamanho">Tamanho (default 20).</param>
public sealed record ListarEdicoesQuery(int? Ano = null, int? Situacao = null, int Pagina = 1, int Tamanho = 20)
    : IQuery<PaginaEdicoes>;

/// <summary>Handler da listagem de edicoes (backoffice — inclui rascunhos).</summary>
public sealed class ListarEdicoesHandler(IEdicaoDiarioRepository edicoes)
    : IQueryHandler<ListarEdicoesQuery, PaginaEdicoes>
{
    /// <inheritdoc />
    public async Task<PaginaEdicoes> Handle(ListarEdicoesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pagina = request.Pagina < 1 ? 1 : request.Pagina;
        var tamanho = request.Tamanho is < 1 or > 200 ? 20 : request.Tamanho;
        var situacao = request.Situacao is { } s && Enum.IsDefined(typeof(SituacaoEdicao), s) ? (SituacaoEdicao?)s : null;

        var (itens, total) = await edicoes
            .ListarAsync(request.Ano, situacao, apenasPublicadas: false, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        return new PaginaEdicoes(itens.Select(MapearResumo).ToList(), total, pagina, tamanho);
    }

    internal static EdicaoResumo MapearResumo(EdicaoDiario edicao)
        => new(edicao.Id.Value, edicao.Numero, edicao.Ano, edicao.Situacao.ToString(), edicao.DataPublicacao, edicao.Materias.Count);
}

/// <summary>Consulta cidada (LAI): apenas edicoes publicadas, por ano/termo, paginadas.</summary>
/// <param name="Ano">Ano (opcional).</param>
/// <param name="Pagina">Pagina (default 1).</param>
/// <param name="Tamanho">Tamanho (default 20).</param>
public sealed record ConsultarDiarioPublicoQuery(int? Ano = null, int Pagina = 1, int Tamanho = 20)
    : IQuery<PaginaEdicoes>;

/// <summary>Handler da consulta publica (so publicadas).</summary>
public sealed class ConsultarDiarioPublicoHandler(IEdicaoDiarioRepository edicoes)
    : IQueryHandler<ConsultarDiarioPublicoQuery, PaginaEdicoes>
{
    /// <inheritdoc />
    public async Task<PaginaEdicoes> Handle(ConsultarDiarioPublicoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pagina = request.Pagina < 1 ? 1 : request.Pagina;
        var tamanho = request.Tamanho is < 1 or > 200 ? 20 : request.Tamanho;

        var (itens, total) = await edicoes
            .ListarAsync(request.Ano, situacao: null, apenasPublicadas: true, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        return new PaginaEdicoes(itens.Select(ListarEdicoesHandler.MapearResumo).ToList(), total, pagina, tamanho);
    }
}

/// <summary>Obtem o detalhe de uma edicao por identificador (com materias), tenant-scoped.</summary>
/// <param name="EdicaoId">Identificador.</param>
public sealed record ObterEdicaoPorIdQuery(Guid EdicaoId) : IQuery<EdicaoDetalhe?>;

/// <summary>Handler do detalhe de edicao.</summary>
public sealed class ObterEdicaoPorIdHandler(IEdicaoDiarioRepository edicoes)
    : IQueryHandler<ObterEdicaoPorIdQuery, EdicaoDetalhe?>
{
    /// <inheritdoc />
    public async Task<EdicaoDetalhe?> Handle(ObterEdicaoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var edicao = await edicoes.ObterPorIdAsync(new EdicaoDiarioId(request.EdicaoId), cancellationToken).ConfigureAwait(false);
        if (edicao is null)
        {
            return null;
        }

        var materias = edicao.Materias
            .OrderBy(materia => materia.Ordem)
            .Select(materia => new MateriaDto(
                materia.Id.Value,
                materia.Tipo.ToString(),
                materia.Titulo,
                materia.Conteudo,
                materia.ReferenciaId,
                materia.Ordem))
            .ToList();

        return new EdicaoDetalhe(
            edicao.Id.Value,
            edicao.Numero,
            edicao.Ano,
            edicao.Situacao.ToString(),
            edicao.DataPublicacao,
            edicao.EdicaoOriginalId?.Value,
            materias);
    }
}
