using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;

/// <summary>
/// COLETA ONLINE de um REP: resolve o driver (<see cref="IColetorRep"/>) da marca, coleta o AFD de
/// forma INCREMENTAL (a partir do ultimo NSR ja coletado) e despacha a ingestao idempotente. O caminho
/// online e o de arquivo CONVERGEM no mesmo pipeline (<see cref="IngestarMarcacoesAfdCommand"/>) — o
/// dedup por (REP, NSR) torna ambos seguros e reentrantes.
/// </summary>
/// <param name="RepId">Equipamento a coletar (cadastrado e ativo no tenant).</param>
public sealed record ColetarRepCommand(Guid RepId) : ICommand<ResultadoIngestaoAfd>;

/// <summary>Validacao da coleta online.</summary>
public sealed class ColetarRepValidator : AbstractValidator<ColetarRepCommand>
{
    /// <summary>Define as regras.</summary>
    public ColetarRepValidator()
        => RuleFor(c => c.RepId).NotEmpty().WithMessage("REP e obrigatorio.");
}

/// <summary>Handler da coleta online.</summary>
public sealed class ColetarRepHandler(
    IRepRepository reps,
    IColetorRepFactory coletores,
    ISender sender)
    : ICommandHandler<ColetarRepCommand, ResultadoIngestaoAfd>
{
    /// <inheritdoc />
    public async Task<ResultadoIngestaoAfd> Handle(ColetarRepCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rep = await reps.ObterPorIdAsync(new RepConfiguradoId(request.RepId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("REP nao cadastrado no tenant.");

        var coletor = coletores.Resolver(rep.Marca);
        var conexao = new RepConexao(rep.EnderecoOuReferencia, rep.ReferenciaCredencialCofre, rep.IdentificacaoEquipamento);

        var lote = await coletor.ColetarAsync(conexao, rep.UltimoNsrColetado, cancellationToken).ConfigureAwait(false);

        // Mesmo pipeline de ingestao da importacao de arquivo — idempotente, fail-closed na integridade.
        return await sender
            .Send(new IngestarMarcacoesAfdCommand(request.RepId, lote.ConteudoAfd, lote.AssinaturaCades), cancellationToken)
            .ConfigureAwait(false);
    }
}
