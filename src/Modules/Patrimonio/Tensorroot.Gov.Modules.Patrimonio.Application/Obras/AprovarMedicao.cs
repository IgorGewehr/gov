using System.Globalization;
using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>
/// Aprova um boletim de medição (gatilho da liquidação em Finanças — Lei 4.320 art. 63). Invariantes
/// I-1/I-5/I-8/I-10/I-11 protegidas no agregado.
/// <para>
/// SEGREGAÇÃO DE FUNÇÃO (Lei 14.133 art. 117 / I-10): o aprovador NÃO trafega no comando — é
/// SEMPRE derivado do subject ("sub") do JWT no handler (<see cref="ICurrentUser"/>). Aceitar um
/// id de fiscal vindo do cliente permitiria impersonação (qualquer um com a permissão genérica
/// aprovaria "como" o fiscal designado). Negar por padrão: o agregado só aprova se o usuário
/// autenticado FOR o fiscal designado vigente da obra.
/// </para>
/// </summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="MedicaoId">Medição a aprovar.</param>
/// <param name="DataAprovacao">Data da aprovação.</param>
public sealed record AprovarMedicaoCommand(Guid ObraId, Guid MedicaoId, DateOnly DataAprovacao) : ICommand;

/// <summary>Regras de validação da aprovação de medição.</summary>
public sealed class AprovarMedicaoValidator : AbstractValidator<AprovarMedicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AprovarMedicaoValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.MedicaoId).NotEmpty();
    }
}

/// <summary>Handler da aprovação de medição (publica <see cref="MedicaoAprovadaIntegrationEvent"/> para Finanças via Outbox).</summary>
public sealed class AprovarMedicaoHandler(
    IObraRepository obras,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    ITenantContext tenant,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<AprovarMedicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AprovarMedicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // SEGREGAÇÃO DE FUNÇÃO (Lei 14.133 art. 117 / I-10): o aprovador é o subject ("sub") do JWT,
        // NUNCA um id vindo do cliente — caso contrário qualquer usuário com a permissão genérica
        // "patrimonio.gerenciar" aprovaria a medição "como" o fiscal designado (impersonação). Negar
        // por padrão: sub ausente/inválido reprova ANTES de tocar o agregado (fail-closed).
        if (!TentarObterUsuarioAutenticado(out var aprovadorId))
        {
            throw new InvalidOperationException(
                "Aprovação de medição exige usuário autenticado identificável (segregação de função — art. 117 / I-10).");
        }

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        var medicaoId = new MedicaoId(request.MedicaoId);
        // O agregado confronta o aprovador (subject autenticado) contra o FiscalDesignadoId vigente
        // (I-10): só aprova se o usuário do token FOR o fiscal designado da obra ("é-o-fiscal-designado").
        obra.AprovarMedicao(medicaoId, aprovadorId, request.DataAprovacao);

        var medicao = obra.Medicoes.First(item => item.Id == medicaoId);

        // Integration event via OUTBOX (CLAUDE.md §2/§10): a mensagem grava na MESMA UoW que a medição
        // (consistência transacional) e evita resolver o DbContext de Finanças no mesmo escopo. A entrega
        // à liquidação (Finanças) ocorre na drenagem do Outbox, em escopo dedicado. Fecha o laço
        // medição → liquidação → pagamento.
        var evento = new MedicaoAprovadaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            obra.Id.Value,
            obra.ContratoId,
            medicao.Id.Value,
            medicao.Numero,
            medicao.ValorMedido.Valor,
            medicao.CompetenciaAno,
            medicao.CompetenciaMes,
            obra.FornecedorId);

        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deriva o aprovador do subject ("sub") do JWT (<see cref="ICurrentUser.UserId"/>), parseando-o
    /// como <see cref="Guid"/>. Falha (retorna <c>false</c>) se o subject estiver ausente, em branco,
    /// não-parseável ou vazio — fail-closed, jamais aceitando id do cliente.
    /// </summary>
    private bool TentarObterUsuarioAutenticado(out Guid usuarioId)
    {
        usuarioId = Guid.Empty;
        var sub = currentUser.UserId;
        return !string.IsNullOrWhiteSpace(sub)
            && Guid.TryParse(sub, CultureInfo.InvariantCulture, out usuarioId)
            && usuarioId != Guid.Empty;
    }
}

/// <summary>Rejeita um boletim de medição (não compõe o valor medido acumulado).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="MedicaoId">Medição a rejeitar.</param>
/// <param name="Motivo">Motivo da rejeição.</param>
public sealed record RejeitarMedicaoCommand(Guid ObraId, Guid MedicaoId, string Motivo) : ICommand;

/// <summary>Regras de validação da rejeição de medição.</summary>
public sealed class RejeitarMedicaoValidator : AbstractValidator<RejeitarMedicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RejeitarMedicaoValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.MedicaoId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler da rejeição de medição.</summary>
public sealed class RejeitarMedicaoHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<RejeitarMedicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RejeitarMedicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        obra.RejeitarMedicao(new MedicaoId(request.MedicaoId), request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
