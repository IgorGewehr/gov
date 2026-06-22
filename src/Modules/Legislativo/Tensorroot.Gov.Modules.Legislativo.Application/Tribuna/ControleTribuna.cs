using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Tribuna;

/// <summary>Inscreve um orador na tribuna (idempotente por vereador+fase) — T-2.</summary>
/// <param name="TribunaId">Tribuna alvo.</param>
/// <param name="VereadorId">Vereador a inscrever.</param>
/// <param name="Fase">Fase de uso da palavra (<see cref="FaseUsoPalavra"/>).</param>
/// <param name="TempoConcedidoSegundos">Tempo concedido em segundos (opcional; default = tempo padrao da tribuna).</param>
public sealed record InscreverOradorCommand(Guid TribunaId, Guid VereadorId, int Fase, int? TempoConcedidoSegundos)
    : ICommand<Guid>;

/// <summary>Regras de validacao da inscricao de orador.</summary>
public sealed class InscreverOradorValidator : AbstractValidator<InscreverOradorCommand>
{
    /// <summary>Define as regras.</summary>
    public InscreverOradorValidator()
    {
        RuleFor(comando => comando.TribunaId).NotEmpty();
        RuleFor(comando => comando.VereadorId).NotEmpty();
        RuleFor(comando => comando.Fase).Must(valor => Enum.IsDefined(typeof(FaseUsoPalavra), valor))
            .WithMessage("Fase de uso da palavra invalida.");
        RuleFor(comando => comando.TempoConcedidoSegundos!.Value)
            .GreaterThan(0)
            .When(comando => comando.TempoConcedidoSegundos.HasValue);
    }
}

/// <summary>Handler da inscricao de orador.</summary>
public sealed class InscreverOradorHandler(ITribunaSessaoRepository tribunas, IUnitOfWork unitOfWork)
    : ICommandHandler<InscreverOradorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(InscreverOradorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tribuna = await tribunas.ObterPorIdAsync(new TribunaSessaoId(request.TribunaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Tribuna nao encontrada.");

        var tempo = request.TempoConcedidoSegundos is { } segundos ? TimeSpan.FromSeconds(segundos) : (TimeSpan?)null;
        var inscricao = tribuna.Inscrever(new VereadorId(request.VereadorId), (FaseUsoPalavra)request.Fase, tempo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return inscricao.Id.Value;
    }
}

/// <summary>Base dos comandos de controle do cronometro (operam sobre uma inscricao).</summary>
/// <param name="TribunaId">Tribuna alvo.</param>
/// <param name="InscricaoId">Inscricao alvo.</param>
public sealed record IniciarFalaCommand(Guid TribunaId, Guid InscricaoId) : ICommand;

/// <summary>Pausa a fala em curso — T-5.</summary>
/// <param name="TribunaId">Tribuna alvo.</param>
/// <param name="InscricaoId">Inscricao alvo.</param>
public sealed record PausarFalaCommand(Guid TribunaId, Guid InscricaoId) : ICommand;

/// <summary>Retoma a fala pausada — T-5.</summary>
/// <param name="TribunaId">Tribuna alvo.</param>
/// <param name="InscricaoId">Inscricao alvo.</param>
public sealed record RetomarFalaCommand(Guid TribunaId, Guid InscricaoId) : ICommand;

/// <summary>Encerra a fala (apura tempo/excedente) — T-6.</summary>
/// <param name="TribunaId">Tribuna alvo.</param>
/// <param name="InscricaoId">Inscricao alvo.</param>
public sealed record EncerrarFalaCommand(Guid TribunaId, Guid InscricaoId) : ICommand;

/// <summary>Cancela uma inscricao ainda nao iniciada — T-7.</summary>
/// <param name="TribunaId">Tribuna alvo.</param>
/// <param name="InscricaoId">Inscricao alvo.</param>
public sealed record CancelarInscricaoCommand(Guid TribunaId, Guid InscricaoId) : ICommand;

/// <summary>
/// Handlers de controle da tribuna: o <c>momento</c> e SEMPRE resolvido por <see cref="TimeProvider"/>
/// no servidor (nunca recebido do cliente) — T-8.
/// </summary>
public sealed class ControleTribunaHandlers(
    ITribunaSessaoRepository tribunas,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) :
    ICommandHandler<IniciarFalaCommand>,
    ICommandHandler<PausarFalaCommand>,
    ICommandHandler<RetomarFalaCommand>,
    ICommandHandler<EncerrarFalaCommand>,
    ICommandHandler<CancelarInscricaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(IniciarFalaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tribuna, inscricaoId) = await CarregarAsync(request.TribunaId, request.InscricaoId, cancellationToken).ConfigureAwait(false);
        tribuna.IniciarFala(inscricaoId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Handle(PausarFalaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tribuna, inscricaoId) = await CarregarAsync(request.TribunaId, request.InscricaoId, cancellationToken).ConfigureAwait(false);
        tribuna.PausarFala(inscricaoId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Handle(RetomarFalaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tribuna, inscricaoId) = await CarregarAsync(request.TribunaId, request.InscricaoId, cancellationToken).ConfigureAwait(false);
        tribuna.RetomarFala(inscricaoId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Handle(EncerrarFalaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tribuna, inscricaoId) = await CarregarAsync(request.TribunaId, request.InscricaoId, cancellationToken).ConfigureAwait(false);
        tribuna.EncerrarFala(inscricaoId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Handle(CancelarInscricaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tribuna, inscricaoId) = await CarregarAsync(request.TribunaId, request.InscricaoId, cancellationToken).ConfigureAwait(false);
        tribuna.CancelarInscricao(inscricaoId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<(TribunaSessao Tribuna, InscricaoOradorId InscricaoId)> CarregarAsync(
        Guid tribunaId,
        Guid inscricaoId,
        CancellationToken cancellationToken)
    {
        var tribuna = await tribunas.ObterPorIdAsync(new TribunaSessaoId(tribunaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Tribuna nao encontrada.");
        return (tribuna, new InscricaoOradorId(inscricaoId));
    }
}
