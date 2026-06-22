using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Normas;

/// <summary>Evento da trilha de vigencia (projecao de leitura).</summary>
/// <param name="Tipo">Tipo do evento.</param>
/// <param name="Data">Data.</param>
/// <param name="NormaReferenciaId">Norma de referencia (opcional).</param>
/// <param name="Observacao">Observacao (opcional).</param>
public sealed record EventoVigenciaDto(string Tipo, DateOnly Data, Guid? NormaReferenciaId, string? Observacao);

/// <summary>Detalhe completo de uma norma (inclui historico de vigencia e proposicao de origem).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Tipo">Especie.</param>
/// <param name="Numero">Numero.</param>
/// <param name="Ano">Ano.</param>
/// <param name="Ementa">Resumo do objeto.</param>
/// <param name="DataPromulgacao">Data de promulgacao.</param>
/// <param name="Situacao">Situacao de vigencia.</param>
/// <param name="DataRevogacao">Data de revogacao (se houver).</param>
/// <param name="TextoArticulado">Texto articulado (opcional).</param>
/// <param name="ProposicaoOrigemId">Proposicao de origem (opcional).</param>
/// <param name="Historico">Trilha de vigencia.</param>
public sealed record NormaDetalhe(
    Guid Id,
    string Tipo,
    int Numero,
    int Ano,
    string Ementa,
    DateOnly DataPromulgacao,
    string Situacao,
    DateOnly? DataRevogacao,
    string? TextoArticulado,
    Guid? ProposicaoOrigemId,
    IReadOnlyList<EventoVigenciaDto> Historico);

/// <summary>Obtem o detalhe de uma norma por identificador (tenant-scoped).</summary>
/// <param name="NormaId">Identificador.</param>
public sealed record ObterNormaPorIdQuery(Guid NormaId) : IQuery<NormaDetalhe?>;

/// <summary>Handler do detalhe de norma.</summary>
public sealed class ObterNormaPorIdHandler(INormaRepository normas)
    : IQueryHandler<ObterNormaPorIdQuery, NormaDetalhe?>
{
    /// <inheritdoc />
    public async Task<NormaDetalhe?> Handle(ObterNormaPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var norma = await normas.ObterPorIdAsync(new NormaId(request.NormaId), cancellationToken).ConfigureAwait(false);
        if (norma is null)
        {
            return null;
        }

        var historico = norma.HistoricoVigencia
            .Select(evento => new EventoVigenciaDto(
                evento.Tipo.ToString(),
                evento.Data,
                evento.NormaReferenciaId?.Value,
                evento.Observacao))
            .ToList();

        return new NormaDetalhe(
            norma.Id.Value,
            norma.Tipo.ToString(),
            norma.Numero,
            norma.Ano,
            norma.Ementa.Valor,
            norma.DataPromulgacao,
            norma.SituacaoVigencia.ToString(),
            norma.DataRevogacao,
            norma.TextoArticulado,
            norma.ProposicaoOrigemId?.Value,
            historico);
    }
}
