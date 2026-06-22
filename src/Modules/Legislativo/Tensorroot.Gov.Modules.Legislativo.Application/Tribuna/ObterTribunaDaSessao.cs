using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Tribuna;

/// <summary>Inscricao de orador (projecao de leitura para o painel).</summary>
/// <param name="InscricaoId">Identificador.</param>
/// <param name="VereadorId">Vereador.</param>
/// <param name="Fase">Fase de uso da palavra.</param>
/// <param name="Ordem">Ordem na fila.</param>
/// <param name="Situacao">Situacao da inscricao.</param>
/// <param name="TempoConcedidoSegundos">Tempo concedido (segundos).</param>
/// <param name="IniciadoEm">Inicio da fala (servidor) — base para o cronometro no front.</param>
/// <param name="Pausada">Indica se a fala esta pausada.</param>
/// <param name="TempoUtilizadoSegundos">Tempo utilizado, apos encerrar (segundos).</param>
/// <param name="ExcedenteSegundos">Excedente, apos encerrar (segundos).</param>
public sealed record InscricaoDto(
    Guid InscricaoId,
    Guid VereadorId,
    string Fase,
    int Ordem,
    string Situacao,
    double TempoConcedidoSegundos,
    DateTimeOffset? IniciadoEm,
    bool Pausada,
    double? TempoUtilizadoSegundos,
    double? ExcedenteSegundos);

/// <summary>Estado da tribuna de uma sessao (fila + orador atual + tempos).</summary>
/// <param name="TribunaId">Identificador.</param>
/// <param name="SessaoId">Sessao.</param>
/// <param name="TempoPadraoSegundos">Tempo padrao por orador (segundos).</param>
/// <param name="OradorAtualId">Inscricao do orador em uso (se houver).</param>
/// <param name="Inscricoes">Fila de inscricoes.</param>
public sealed record TribunaDto(
    Guid TribunaId,
    Guid SessaoId,
    double TempoPadraoSegundos,
    Guid? OradorAtualId,
    IReadOnlyList<InscricaoDto> Inscricoes);

/// <summary>Obtem o estado da tribuna de uma sessao (tenant-scoped).</summary>
/// <param name="SessaoId">Sessao.</param>
public sealed record ObterTribunaDaSessaoQuery(Guid SessaoId) : IQuery<TribunaDto?>;

/// <summary>Handler do estado da tribuna.</summary>
public sealed class ObterTribunaDaSessaoHandler(ITribunaSessaoRepository tribunas)
    : IQueryHandler<ObterTribunaDaSessaoQuery, TribunaDto?>
{
    /// <inheritdoc />
    public async Task<TribunaDto?> Handle(ObterTribunaDaSessaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tribuna = await tribunas.ObterPorSessaoAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false);
        if (tribuna is null)
        {
            return null;
        }

        var inscricoes = tribuna.Inscricoes
            .OrderBy(inscricao => inscricao.Ordem)
            .Select(inscricao => new InscricaoDto(
                inscricao.Id.Value,
                inscricao.VereadorId.Value,
                inscricao.Fase.ToString(),
                inscricao.Ordem,
                inscricao.Situacao.ToString(),
                inscricao.TempoConcedido.TotalSeconds,
                inscricao.IniciadoEm,
                inscricao.Pausada,
                inscricao.TempoUtilizado?.TotalSeconds,
                inscricao.Excedente?.TotalSeconds))
            .ToList();

        return new TribunaDto(
            tribuna.Id.Value,
            tribuna.SessaoId.Value,
            tribuna.TempoPadraoOrador.TotalSeconds,
            tribuna.OradorEmUso?.Id.Value,
            inscricoes);
    }
}
