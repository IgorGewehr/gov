using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Contracts;

namespace Tensorroot.Gov.Modules.Financas.Application.Fiscal;

/// <summary>
/// Registra a <b>Receita Corrente Líquida (RCL)</b> apurada de um período (LRF — LC 101/2000 art. 2º, IV)
/// e a publica para o Painel do Gestor (denominador do limite de Despesa com Pessoal — LRF art. 19/20).
/// <para>
/// <b>Sem fonte automática de RCL no módulo Finanças hoje</b> (a apuração das exclusões legais da RCL
/// depende de detalhamento contábil ainda não materializado — ver TODO no contrato). Em vez de inventar
/// um valor, este comando expõe um ponto de entrada <b>parametrizável</b>: o ente informa a RCL apurada
/// (ex.: extraída do RREO/anexo da LRF) e o sistema a propaga. Quando a apuração automática existir, ela
/// substitui este comando como produtor do mesmo evento.
/// </para>
/// </summary>
/// <param name="Exercicio">Exercício (ano) de referência da apuração.</param>
/// <param name="MesReferencia">Mês de referência (1-12) ao qual se ancora a janela de 12 meses.</param>
/// <param name="ValorRcl">Valor da RCL apurada (12 meses) — informado pelo ente, &gt; 0.</param>
public sealed record RegistrarReceitaCorrenteLiquidaCommand(int Exercicio, int MesReferencia, decimal ValorRcl) : ICommand;

/// <summary>Regras de validação do registro de RCL apurada.</summary>
public sealed class RegistrarReceitaCorrenteLiquidaValidator : AbstractValidator<RegistrarReceitaCorrenteLiquidaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarReceitaCorrenteLiquidaValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
        RuleFor(c => c.MesReferencia).InclusiveBetween(1, 12);
        // RCL é denominador do percentual — deve ser estritamente positiva (não inventamos valor).
        RuleFor(c => c.ValorRcl).GreaterThan(0m).WithMessage("A RCL apurada deve ser maior que zero.");
    }
}

/// <summary>
/// Handler do registro de RCL apurada. Enfileira no Outbox (consistência transacional — padrão
/// FecharFolha/GerarMsc) a <see cref="ReceitaCorrenteLiquidaApuradaIntegrationEvent"/>, consumida pelo
/// Painel do Gestor (substitui por (Tenant, Exercicio, Mês) — idempotente). Não há agregado próprio: é
/// um ponto de propagação de um número apurado fora do sistema (aditivo, sem mutar a contabilidade).
/// </summary>
public sealed class RegistrarReceitaCorrenteLiquidaHandler(
    IDotacaoOrcamentariaRepository dotacoes,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider) : ICommandHandler<RegistrarReceitaCorrenteLiquidaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarReceitaCorrenteLiquidaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Âncora de escopo (H5): este comando não tem agregado próprio, mas o Outbox/IUnitOfWork exigem
        // que o DbContext do MÓDULO esteja resolvido no escopo (ScopeDbContextHolder). Tocar o repositório
        // de Finanças resolve o FinancasDbContext, fixando o Outbox onde enfileirar — sem efeito de escrita.
        _ = await dotacoes.ListarPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        var evento = new ReceitaCorrenteLiquidaApuradaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            request.Exercicio,
            request.MesReferencia,
            request.ValorRcl);

        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
