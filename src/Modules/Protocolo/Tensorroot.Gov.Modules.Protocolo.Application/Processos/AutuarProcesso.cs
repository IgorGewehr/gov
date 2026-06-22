using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Contracts;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>Autua um processo administrativo, gerando o NUP unico (Lei 9.784/1999; Decreto 8.539/2015).</summary>
/// <param name="RequerimentoId">Requerimento que originou o processo (opcional).</param>
/// <param name="Classificacao">Classe documental (vincula a Tabela de Temporalidade).</param>
/// <param name="NivelAcesso">Visibilidade do processo.</param>
/// <param name="OrigemModulo">Modulo originador (quando autuado por outro modulo).</param>
/// <param name="OrigemId">Identificador da origem no modulo originador.</param>
public sealed record AutuarProcessoCommand(
    Guid RequerimentoId,
    string Classificacao,
    NivelDeAcesso NivelAcesso,
    string? OrigemModulo,
    Guid? OrigemId) : ICommand<Guid>;

/// <summary>Regras de validacao da autuacao de processo.</summary>
public sealed class AutuarProcessoValidator : AbstractValidator<AutuarProcessoCommand>
{
    /// <summary>Define as regras.</summary>
    public AutuarProcessoValidator()
    {
        RuleFor(comando => comando.Classificacao).NotEmpty().MaximumLength(Domain.ValueObjects.Classificacao.ComprimentoMaximo);
        RuleFor(comando => comando.NivelAcesso).IsInEnum();
        RuleFor(comando => comando.OrigemId).NotEmpty().When(comando => comando.OrigemModulo != null);
    }
}

/// <summary>Handler da autuacao de processo.</summary>
public sealed class AutuarProcessoHandler(
    IProcessoRepository processos,
    INupGenerator nupGenerator,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AutuarProcessoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AutuarProcessoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var nup = await nupGenerator.GerarAsync(cancellationToken).ConfigureAwait(false);
        var requerimentoId = request.RequerimentoId == Guid.Empty ? (Guid?)null : request.RequerimentoId;

        var processo = Processo.Autuar(
            tenant.TenantId,
            nup,
            new Classificacao(request.Classificacao),
            request.NivelAcesso,
            requerimentoId,
            request.OrigemModulo,
            request.OrigemId,
            hoje);

        processos.Adicionar(processo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Devolve o NUP ao modulo originador quando a autuacao foi solicitada por outro modulo (I-12).
        if (processo.OrigemModulo is not null)
        {
            var evento = new ProcessoAutuadoIntegrationEvent(
                Guid.NewGuid(),
                timeProvider.GetUtcNow().UtcDateTime,
                tenant.TenantId,
                processo.Nup.Valor,
                processo.OrigemModulo,
                processo.OrigemId);

            await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
        }

        return processo.Id.Value;
    }
}
