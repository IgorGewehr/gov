using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Fiscal;

/// <summary>
/// Apura os mínimos constitucionais setoriais (Saúde 15% ASPS · Educação 25% MDE) de um exercício e
/// <b>publica</b> o resultado para o Painel do Gestor via Outbox. A apuração em si é a mesma do
/// <see cref="ApurarMinimosQuery"/> (read-only) — este comando é o produtor do
/// <see cref="MinimoConstitucionalApuradoIntegrationEvent"/>, separado da query porque escreve no Outbox
/// (idempotente no consumidor por (Tenant, Exercicio) — a apuração mais recente substitui a anterior).
/// </summary>
/// <param name="Exercicio">Ano de exercício a apurar e publicar.</param>
public sealed record PublicarMinimosConstitucionaisCommand(int Exercicio) : ICommand;

/// <summary>Regras de validação da publicação dos mínimos.</summary>
public sealed class PublicarMinimosConstitucionaisValidator : AbstractValidator<PublicarMinimosConstitucionaisCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarMinimosConstitucionaisValidator()
        => RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1988);
}

/// <summary>
/// Handler da publicação dos mínimos. Reusa as portas de apuração (<see cref="IExecucaoSetorialReadModel"/>
/// — que resolve o <c>TransparenciaDbContext</c>, fixando o Outbox no escopo — e
/// <see cref="IParametroMinimoProvider"/>), delega ao <see cref="ApuradorMinimo"/> (toda a regra fiscal é
/// do domínio) e enfileira o evento de integração no Outbox na MESMA transação (padrão FecharFolha/GerarMsc).
/// </summary>
public sealed class PublicarMinimosConstitucionaisHandler(
    IExecucaoSetorialReadModel execucao,
    IParametroMinimoProvider parametros,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider) : ICommandHandler<PublicarMinimosConstitucionaisCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarMinimosConstitucionaisCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var execucaoSetorial = await execucao.ObterExecucaoAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        var parametrosVigentes = await parametros.ObterParametrosAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        var indicadores = ApuradorMinimo.ApurarTodos(
            execucaoSetorial.ReceitaBaseImpostosTransferencias,
            execucaoSetorial.DespesasPorSetor,
            parametrosVigentes);

        var setores = indicadores
            .Select(indicador => new MinimoSetorialApuradoDto(
                indicador.Setor.ToString(),
                indicador.ReceitaBase,
                indicador.Aplicado,
                indicador.PercentualAplicado,
                indicador.PercentualMinimo,
                indicador.Situacao.ToString()))
            .ToList();

        var evento = new MinimoConstitucionalApuradoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            request.Exercicio,
            setores);

        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
