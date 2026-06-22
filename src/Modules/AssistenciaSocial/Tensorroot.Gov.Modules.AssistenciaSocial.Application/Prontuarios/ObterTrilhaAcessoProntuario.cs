using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>Resumo imutavel de um acesso da trilha (quem/quando/por que), para o controle social/TCE.</summary>
/// <param name="AcessoId">Identificador do acesso.</param>
/// <param name="UsuarioId">Usuario que acessou.</param>
/// <param name="MotivoAcesso">Justificativa do acesso.</param>
/// <param name="DataHoraAcessoUtc">Momento (UTC) do acesso.</param>
public sealed record AcessoResumo(
    Guid AcessoId,
    Guid UsuarioId,
    string MotivoAcesso,
    DateTime DataHoraAcessoUtc);

/// <summary>
/// Lista a trilha de acesso imutavel de um prontuario (quem/quando/por que), exigivel pelo
/// controle social/Tribunal de Contas. Nao expoe o conteudo sigiloso do acompanhamento.
/// </summary>
/// <param name="ProntuarioId">Prontuario cuja trilha sera consultada.</param>
public sealed record ObterTrilhaAcessoProntuarioQuery(Guid ProntuarioId) : IQuery<IReadOnlyList<AcessoResumo>>;

/// <summary>Handler da consulta da trilha de acesso ao prontuario.</summary>
public sealed class ObterTrilhaAcessoProntuarioHandler(IProntuarioSuasRepository prontuarios)
    : IQueryHandler<ObterTrilhaAcessoProntuarioQuery, IReadOnlyList<AcessoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AcessoResumo>> Handle(ObterTrilhaAcessoProntuarioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prontuario = await prontuarios.ObterPorIdAsync(new ProntuarioSuasId(request.ProntuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Prontuario nao encontrado.");

        return prontuario.Acessos
            .OrderBy(acesso => acesso.DataHoraAcessoUtc)
            .Select(acesso => new AcessoResumo(
                acesso.Id.Value,
                acesso.UsuarioId,
                acesso.MotivoAcesso,
                acesso.DataHoraAcessoUtc))
            .ToList();
    }
}
