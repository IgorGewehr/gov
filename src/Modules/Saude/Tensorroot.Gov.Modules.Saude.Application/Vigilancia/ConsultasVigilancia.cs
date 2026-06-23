using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Common;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Application.Vigilancia;

/// <summary>Busca paginada de estabelecimentos sujeitos a VISA. Dado cadastral (nao sensivel LGPD).</summary>
/// <param name="Termo">Termo livre (razao social/documento); nulo lista tudo.</param>
/// <param name="Ramo">Filtro de ramo (opcional).</param>
/// <param name="Risco">Filtro de risco (opcional).</param>
/// <param name="Situacao">Filtro de situacao (opcional).</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarEstabelecimentosFiscalizaveisQuery(
    string? Termo,
    RamoVisa? Ramo,
    GrauRiscoSanitario? Risco,
    SituacaoEstabelecimentoVisa? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<EstabelecimentoFiscalizavelDto>>;

/// <summary>Handler da busca de estabelecimentos fiscalizaveis.</summary>
public sealed class BuscarEstabelecimentosFiscalizaveisHandler(IEstabelecimentoFiscalizavelRepository estabelecimentos)
    : IQueryHandler<BuscarEstabelecimentosFiscalizaveisQuery, ResultadoPaginado<EstabelecimentoFiscalizavelDto>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<EstabelecimentoFiscalizavelDto>> Handle(
        BuscarEstabelecimentosFiscalizaveisQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await estabelecimentos
            .BuscarAsync(request.Termo, request.Ramo, request.Risco, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        return new ResultadoPaginado<EstabelecimentoFiscalizavelDto>(
            [.. itens.Select(e => e.ParaDto())], total, pagina, tamanho);
    }
}

/// <summary>Agenda/historico de inspecoes num intervalo (filtro opcional de situacao).</summary>
/// <param name="De">Data inicial.</param>
/// <param name="Ate">Data final.</param>
/// <param name="Situacao">Filtro de situacao (opcional).</param>
public sealed record ListarAgendaInspecoesQuery(DateOnly De, DateOnly Ate, SituacaoInspecao? Situacao)
    : IQuery<IReadOnlyList<InspecaoDto>>;

/// <summary>Handler da agenda de inspecoes.</summary>
public sealed class ListarAgendaInspecoesHandler(IInspecaoRepository inspecoes)
    : IQueryHandler<ListarAgendaInspecoesQuery, IReadOnlyList<InspecaoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InspecaoDto>> Handle(ListarAgendaInspecoesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await inspecoes
            .ListarAgendaAsync(request.De, request.Ate, request.Situacao, cancellationToken)
            .ConfigureAwait(false);

        return [.. itens.Select(i => i.ParaDto())];
    }
}

/// <summary>Detalhe de uma inspecao (com itens do roteiro).</summary>
/// <param name="InspecaoId">Identificador da inspecao.</param>
public sealed record ObterInspecaoPorIdQuery(Guid InspecaoId) : IQuery<InspecaoDto?>;

/// <summary>Handler do detalhe de inspecao.</summary>
public sealed class ObterInspecaoPorIdHandler(IInspecaoRepository inspecoes)
    : IQueryHandler<ObterInspecaoPorIdQuery, InspecaoDto?>
{
    /// <inheritdoc />
    public async Task<InspecaoDto?> Handle(ObterInspecaoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(request.InspecaoId), cancellationToken).ConfigureAwait(false);
        return inspecao?.ParaDto();
    }
}

/// <summary>Fila de autos por situacao (processos administrativos sanitarios).</summary>
/// <param name="Situacao">Situacao filtrada (opcional — nulo lista todos).</param>
public sealed record ListarAutosQuery(SituacaoAutoVisa? Situacao) : IQuery<IReadOnlyList<AutoVisaDto>>;

/// <summary>Handler da fila de autos.</summary>
public sealed class ListarAutosHandler(IAutoVisaRepository autos)
    : IQueryHandler<ListarAutosQuery, IReadOnlyList<AutoVisaDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AutoVisaDto>> Handle(ListarAutosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await autos.ListarPorSituacaoAsync(request.Situacao, cancellationToken).ConfigureAwait(false);
        return [.. itens.Select(a => a.ParaDto())];
    }
}

/// <summary>Licencas de um estabelecimento (historico do alvara).</summary>
/// <param name="EstabelecimentoId">Estabelecimento.</param>
public sealed record ListarLicencasDoEstabelecimentoQuery(Guid EstabelecimentoId) : IQuery<IReadOnlyList<LicencaSanitariaDto>>;

/// <summary>Handler das licencas de um estabelecimento.</summary>
public sealed class ListarLicencasDoEstabelecimentoHandler(ILicencaSanitariaRepository licencas, TimeProvider timeProvider)
    : IQueryHandler<ListarLicencasDoEstabelecimentoQuery, IReadOnlyList<LicencaSanitariaDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LicencaSanitariaDto>> Handle(
        ListarLicencasDoEstabelecimentoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var itens = await licencas
            .ListarPorEstabelecimentoAsync(new EstabelecimentoFiscalizavelId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false);

        // Reavalia a vigencia para a leitura (vencida por decurso) — projecao, sem persistir.
        foreach (var licenca in itens)
        {
            licenca.AvaliarVigencia(hoje);
        }

        return [.. itens.Select(l => l.ParaDto())];
    }
}

/// <summary>Licencas vigentes a vencer ate N dias (alerta de renovacao).</summary>
/// <param name="Dias">Janela em dias (default 30).</param>
public sealed record ListarLicencasAVencerQuery(int? Dias) : IQuery<IReadOnlyList<LicencaSanitariaDto>>;

/// <summary>Handler do alerta de licencas a vencer.</summary>
public sealed class ListarLicencasAVencerHandler(ILicencaSanitariaRepository licencas, TimeProvider timeProvider)
    : IQueryHandler<ListarLicencasAVencerQuery, IReadOnlyList<LicencaSanitariaDto>>
{
    private const int JanelaPadraoDias = 30;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LicencaSanitariaDto>> Handle(
        ListarLicencasAVencerQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var ate = hoje.AddDays(request.Dias is > 0 ? request.Dias.Value : JanelaPadraoDias);
        var itens = await licencas.ListarAVencerAsync(ate, cancellationToken).ConfigureAwait(false);
        return [.. itens.Select(l => l.ParaDto())];
    }
}
