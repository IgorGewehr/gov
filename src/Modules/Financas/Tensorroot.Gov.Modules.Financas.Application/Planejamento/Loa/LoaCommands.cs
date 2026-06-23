using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Planejamento.Loa;

/// <summary>Cria uma LOA (projeto de lei) vinculada a uma LDO vigente do exercício.</summary>
public sealed record CriarLoaCommand(int Exercicio, Guid LdoId, decimal LimiteSuplementacaoPercentual, string NumeroLei, int AnoLei) : ICommand<Guid>;

/// <summary>Validação da criação de LOA.</summary>
public sealed class CriarLoaValidator : AbstractValidator<CriarLoaCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarLoaValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(DotacaoOrcamentaria.ExercicioMinimo);
        RuleFor(c => c.LdoId).NotEmpty();
        RuleFor(c => c.LimiteSuplementacaoPercentual).InclusiveBetween(0m, 100m);
        RuleFor(c => c.NumeroLei).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da criação de LOA.</summary>
public sealed class CriarLoaHandler(ILdoRepository ldos, ILoaRepository loas, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<CriarLoaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarLoaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ldo = await ldos.ObterPorIdAsync(new LdoId(request.LdoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LDO nao encontrada.");

        var loa = LeiOrcamentariaAnual.Criar(tenant.TenantId, request.Exercicio, ldo, request.LimiteSuplementacaoPercentual, request.NumeroLei, request.AnoLei);
        loas.Adicionar(loa);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return loa.Id.Value;
    }
}

/// <summary>Prevê uma receita por natureza/fonte na LOA.</summary>
public sealed record PreverReceitaCommand(
    Guid LoaId,
    CategoriaEconomicaReceita Categoria,
    string Origem,
    string Especie,
    string Rubrica,
    string FonteRecurso,
    decimal ValorPrevisto) : ICommand<Guid>;

/// <summary>Validação da previsão de receita.</summary>
public sealed class PreverReceitaValidator : AbstractValidator<PreverReceitaCommand>
{
    /// <summary>Define as regras.</summary>
    public PreverReceitaValidator()
    {
        RuleFor(c => c.LoaId).NotEmpty();
        RuleFor(c => c.Categoria).Must(c => Enum.IsDefined(c)).WithMessage("Categoria economica da receita invalida.");
        RuleFor(c => c.Origem).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Especie).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Rubrica).NotEmpty().MaximumLength(40);
        RuleFor(c => c.FonteRecurso).NotEmpty().MaximumLength(20);
        RuleFor(c => c.ValorPrevisto).GreaterThan(0m);
    }
}

/// <summary>Handler da previsão de receita.</summary>
public sealed class PreverReceitaHandler(ILoaRepository loas, IUnitOfWork unitOfWork)
    : ICommandHandler<PreverReceitaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(PreverReceitaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LOA nao encontrada.");

        var natureza = NaturezaReceita.De(request.Categoria, request.Origem, request.Especie, request.Rubrica);
        var id = loa.PreverReceita(natureza, request.FonteRecurso, ValorMonetario.De(request.ValorPrevisto));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id.Value;
    }
}

/// <summary>Fixa uma despesa (linha do QDD) vinculada a uma ação do PPA.</summary>
public sealed record FixarDespesaCommand(
    Guid LoaId,
    string Orgao,
    string Unidade,
    string FuncionalProgramatica,
    CategoriaEconomica CategoriaEconomica,
    string FonteRecurso,
    Guid AcaoPpaId,
    string NaturezaDespesa,
    decimal ValorFixado) : ICommand<Guid>;

/// <summary>Validação da fixação de despesa.</summary>
public sealed class FixarDespesaValidator : AbstractValidator<FixarDespesaCommand>
{
    /// <summary>Define as regras.</summary>
    public FixarDespesaValidator()
    {
        RuleFor(c => c.LoaId).NotEmpty();
        RuleFor(c => c.Orgao).NotEmpty().MaximumLength(10);
        RuleFor(c => c.Unidade).NotEmpty().MaximumLength(20);
        RuleFor(c => c.FuncionalProgramatica).NotEmpty().MaximumLength(50);
        RuleFor(c => c.CategoriaEconomica).Must(c => Enum.IsDefined(c)).WithMessage("Categoria economica invalida.");
        RuleFor(c => c.FonteRecurso).NotEmpty().MaximumLength(20);
        RuleFor(c => c.AcaoPpaId).NotEmpty();
        RuleFor(c => c.NaturezaDespesa).NotEmpty().MaximumLength(30);
        RuleFor(c => c.ValorFixado).GreaterThan(0m);
    }
}

/// <summary>Handler da fixação de despesa (QDD).</summary>
public sealed class FixarDespesaHandler(ILoaRepository loas, IUnitOfWork unitOfWork)
    : ICommandHandler<FixarDespesaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(FixarDespesaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LOA nao encontrada.");

        var classificacao = ClassificacaoOrcamentaria.De(
            request.Orgao, request.Unidade, request.FuncionalProgramatica, request.CategoriaEconomica, request.FonteRecurso);

        var id = loa.FixarDespesa(classificacao, new AcaoPpaId(request.AcaoPpaId), request.NaturezaDespesa, ValorMonetario.De(request.ValorFixado));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id.Value;
    }
}

/// <summary>Coloca a LOA em tramitação.</summary>
public sealed record TramitarLoaCommand(Guid LoaId) : ICommand;

/// <summary>Handler da tramitação da LOA.</summary>
public sealed class TramitarLoaHandler(ILoaRepository loas, IUnitOfWork unitOfWork) : ICommandHandler<TramitarLoaCommand>
{
    /// <inheritdoc />
    public async Task Handle(TramitarLoaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LOA nao encontrada.");
        loa.ColocarEmTramitacao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Aprova a LOA (valida compatibilidade PPA/LDO + equilíbrio).</summary>
public sealed record AprovarLoaCommand(Guid LoaId) : ICommand;

/// <summary>Handler da aprovação da LOA.</summary>
public sealed class AprovarLoaHandler(
    ILoaRepository loas,
    ICompatibilidadeOrcamentariaService compatibilidade,
    IUnitOfWork unitOfWork) : ICommandHandler<AprovarLoaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AprovarLoaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LOA nao encontrada.");

        await loa.AprovarAsync(compatibilidade, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Coloca a LOA em execução (gera as dotações dos itens via domain event).</summary>
public sealed record ColocarLoaEmExecucaoCommand(Guid LoaId) : ICommand;

/// <summary>
/// Handler da entrada em execução. Ao colocar a LOA em execução nascem as dotações do exercício
/// (via domain event <c>ItemLoaEntrouEmExecucao</c>, na mesma transação); em seguida publica a
/// <see cref="DotacaoOrcamentariaPublicadaIntegrationEvent"/> com o TOTAL do exercício — denominador
/// da execução orçamentária no Painel do Gestor. Enfileirado no Outbox na MESMA transação.
/// </summary>
public sealed class ColocarLoaEmExecucaoHandler(
    ILoaRepository loas,
    IDotacaoOrcamentariaRepository dotacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IIntegrationEventWriter integrationEvents,
    TimeProvider timeProvider)
    : ICommandHandler<ColocarLoaEmExecucaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ColocarLoaEmExecucaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LOA nao encontrada.");
        loa.EntrarEmExecucao();

        // Publica o total do exercício como denominador da execução (substitui no consumidor por
        // (Tenant, Exercicio) — idempotente). Aditivo ao ciclo de planejamento.
        await DotacaoPublisher.PublicarTotalDoExercicioAsync(
            dotacoes, integrationEvents, tenant, timeProvider, loa.Exercicio, cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Helper de publicação do total de dotação atualizada de um exercício (denominador da execução
/// orçamentária no Painel do Gestor). Soma as dotações vigentes do exercício e enfileira o evento no
/// Outbox — reusável pela entrada em execução da LOA e pela abertura de crédito adicional (que altera
/// a dotação atualizada). A leitura inclui as dotações recém-criadas no change tracker da MESMA
/// transação (são consultadas via repositório após o domain event tê-las adicionado).
/// </summary>
internal static class DotacaoPublisher
{
    public static async Task PublicarTotalDoExercicioAsync(
        IDotacaoOrcamentariaRepository dotacoes,
        IIntegrationEventWriter integrationEvents,
        ITenantContext tenant,
        TimeProvider timeProvider,
        int exercicio,
        CancellationToken cancellationToken)
    {
        var doExercicio = await dotacoes.ListarPorExercicioAsync(exercicio, cancellationToken).ConfigureAwait(false);
        var inicial = doExercicio.Aggregate(0m, (acc, d) => acc + d.ValorDotadoInicial.Valor);
        var atualizada = doExercicio.Aggregate(0m, (acc, d) => acc + d.ValorAtualizado.Valor);

        integrationEvents.Enfileirar(new DotacaoOrcamentariaPublicadaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            exercicio,
            inicial,
            atualizada));
    }
}

/// <summary>Encerra a LOA no fim do exercício.</summary>
public sealed record EncerrarLoaCommand(Guid LoaId) : ICommand;

/// <summary>Handler do encerramento da LOA.</summary>
public sealed class EncerrarLoaHandler(ILoaRepository loas, IUnitOfWork unitOfWork) : ICommandHandler<EncerrarLoaCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarLoaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LOA nao encontrada.");
        loa.Encerrar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
