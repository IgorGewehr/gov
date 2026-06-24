using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>
/// Conclui a obra (100% físico — I-12) e a INCORPORA ao acervo como bem patrimonial (imobilizado, MCASP)
/// no MESMO contexto (incorporação in-process — design §3.2), numa única transação. Publica
/// <see cref="ObraConcluidaIntegrationEvent"/> (Portal do Gestor/Transparência) e
/// <see cref="BemIncorporadoIntegrationEvent"/> (Finanças — variação patrimonial aumentativa) via Outbox.
/// </summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="DataConclusao">Data de conclusão (base do relógio art. 94 §3 — 45 d.u.).</param>
/// <param name="VidaUtilMeses">Vida útil do imobilizado resultante (meses), para a depreciação MCASP.</param>
/// <param name="TipoBem">Tipo do bem resultante (1=Móvel, 2=Imóvel; obras de edificação = Imóvel).</param>
/// <param name="ValorResidual">Valor residual estimado do imobilizado (piso da depreciação).</param>
/// <param name="ValorTerreno">Parcela do terreno (imóvel) que não deprecia; zero quando não aplicável.</param>
public sealed record ConcluirObraCommand(
    Guid ObraId,
    DateOnly DataConclusao,
    int VidaUtilMeses,
    int TipoBem,
    decimal ValorResidual,
    decimal ValorTerreno) : ICommand<Guid>;

/// <summary>Regras de validação da conclusão de obra.</summary>
public sealed class ConcluirObraValidator : AbstractValidator<ConcluirObraCommand>
{
    /// <summary>Define as regras.</summary>
    public ConcluirObraValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.VidaUtilMeses).GreaterThan(0);
        RuleFor(comando => comando.TipoBem)
            .Must(tipo => Enum.IsDefined(typeof(TipoBem), tipo))
            .WithMessage("Tipo de bem inválido (1=Movel, 2=Imovel).");
        RuleFor(comando => comando.ValorResidual).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.ValorTerreno).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler da conclusão + incorporação patrimonial da obra.</summary>
public sealed class ConcluirObraHandler(
    IObraRepository obras,
    IBemPatrimonialRepository bens,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ConcluirObraCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConcluirObraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        // I-12: conclui (exige 100% físico; emite ObraConcluida no agregado).
        obra.Concluir(request.DataConclusao);

        // Incorporação IN-PROCESS (design §3.2): a obra concluída vira um BemPatrimonial no MESMO contexto.
        var valorFinal = obra.ValorMedidoAcumulado.Valor;
        var residual = Math.Min(request.ValorResidual, valorFinal);
        var bem = BemPatrimonial.Incorporar(
            tenant.TenantId,
            obra.Objeto,
            (TipoBem)request.TipoBem,
            ValorMonetario.De(valorFinal),
            ValorMonetario.De(residual),
            request.VidaUtilMeses,
            request.DataConclusao,
            "Obra concluida",
            Math.Min(request.ValorTerreno, valorFinal));
        bens.Adicionar(bem);

        // I-13: vincula o bem à obra (transição Concluida → Incorporada; emite ObraIncorporada no agregado).
        obra.Incorporar(bem.Id);

        // Eventos de integração via OUTBOX (transacionais com o estado): conclusão (Portal/Transparência) e
        // incorporação do bem (Finanças — MCASP). O BemPatrimonial.Incorporar já emitiu o Domain Event interno;
        // aqui publicamos os Integration Events públicos correspondentes. SICOE (remessa obras TCE-RS) = M10.
        var agora = timeProvider.GetUtcNow().UtcDateTime;
        integrationEvents.Enfileirar(new ObraConcluidaIntegrationEvent(
            Guid.NewGuid(),
            agora,
            tenant.TenantId,
            obra.Id.Value,
            obra.ContratoId,
            bem.Id.Value,
            valorFinal,
            request.DataConclusao));

        integrationEvents.Enfileirar(new BemIncorporadoIntegrationEvent(
            Guid.NewGuid(),
            agora,
            tenant.TenantId,
            bem.Id.Value,
            valorFinal,
            "Obra concluida"));

        // TODO(M10): gerar o artefato/leiaute SICOE (remessa de obras ao TCE-RS) reusando o pipeline
        // Transparencia/RemessaTce; a transmissão real (cert A1/Key Vault, endpoint SICOE) é do M10, atrás de ACL.

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return bem.Id.Value;
    }
}

/// <summary>Rescinde a obra antes da conclusão (terminal). Preserva medições aprovadas (I-15).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="Motivo">Motivo da rescisão.</param>
public sealed record RescindirObraCommand(Guid ObraId, string Motivo) : ICommand;

/// <summary>Regras de validação da rescisão.</summary>
public sealed class RescindirObraValidator : AbstractValidator<RescindirObraCommand>
{
    /// <summary>Define as regras.</summary>
    public RescindirObraValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler da rescisão.</summary>
public sealed class RescindirObraHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<RescindirObraCommand>
{
    /// <inheritdoc />
    public async Task Handle(RescindirObraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        obra.Rescindir(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
