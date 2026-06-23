using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;

/// <summary>
/// IMPORTACAO DE ARQUIVO AFD: ponto de entrada para o AFD ja exportado de um REP (upload pela UI ou
/// pendrive coletado na porta fiscal). Reusa o MESMO pipeline de ingestao idempotente da coleta online
/// (<see cref="IngestarMarcacoesAfdCommand"/>). Funciona para QUALQUER REP-C/A/P — driver universal.
/// </summary>
/// <param name="RepId">Equipamento de origem (cadastrado no tenant) ao qual o AFD pertence.</param>
/// <param name="ConteudoAfd">Bytes do AFD posicional 671 (ISO-8859-1).</param>
/// <param name="AssinaturaCades">Assinatura CAdES detached (.p7s), quando o arquivo a acompanhar; opcional.</param>
public sealed record ImportarAfdCommand(
    Guid RepId,
    byte[] ConteudoAfd,
    byte[]? AssinaturaCades) : ICommand<ResultadoIngestaoAfd>;

/// <summary>Validacao da importacao de arquivo AFD.</summary>
public sealed class ImportarAfdValidator : AbstractValidator<ImportarAfdCommand>
{
    /// <summary>Define as regras.</summary>
    public ImportarAfdValidator()
    {
        RuleFor(c => c.RepId).NotEmpty().WithMessage("REP de origem e obrigatorio.");
        RuleFor(c => c.ConteudoAfd).NotNull().Must(c => c.Length > 0).WithMessage("Arquivo AFD vazio.");
    }
}

/// <summary>Handler da importacao de arquivo AFD (delega ao pipeline de ingestao).</summary>
public sealed class ImportarAfdHandler(ISender sender)
    : ICommandHandler<ImportarAfdCommand, ResultadoIngestaoAfd>
{
    /// <inheritdoc />
    public Task<ResultadoIngestaoAfd> Handle(ImportarAfdCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return sender.Send(
            new IngestarMarcacoesAfdCommand(request.RepId, request.ConteudoAfd, request.AssinaturaCades),
            cancellationToken);
    }
}
