using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

namespace Tensorroot.Gov.Modules.Legislativo.Application.LimiteCamara;

/// <summary>Parcela discriminada de despesa realizada da Camara, lancada na apuracao.</summary>
/// <param name="Natureza">Natureza (1=PessoalAtivo, 2=InativosPensionistas, 3=OutrasDespesas).</param>
/// <param name="Valor">Valor realizado (&gt;= 0).</param>
/// <param name="Descricao">Descricao livre (opcional).</param>
public sealed record DespesaCamaraInput(int Natureza, decimal Valor, string? Descricao);

/// <summary>
/// Abre a apuracao do art. 29-A para um exercicio: registra a base de receita do exercicio ANTERIOR
/// (informada, OU obtida de Financas via Contracts quando o tenant detem a contabilidade), o repasse/
/// duodecimo recebido e a despesa realizada discriminada por natureza. Aplica os parametros do tenant
/// (faixas/subteto/EC109). Demonstrativo nasce em Rascunho.
/// </summary>
/// <param name="Exercicio">Exercicio orcamentario da Camara sob teto.</param>
/// <param name="Populacao">Populacao do municipio (define a faixa do caput).</param>
/// <param name="RepasseRecebido">Repasse/duodecimo recebido no exercicio (base do subteto §1).</param>
/// <param name="ReceitaTributaria">
/// Receita tributaria do exercicio anterior. Se nula, o handler tenta obter a base de Financas.
/// </param>
/// <param name="Transferencias">
/// Transferencias do exercicio anterior. Se nula, o handler tenta obter a base de Financas.
/// </param>
/// <param name="Despesas">Parcelas de despesa realizada discriminadas por natureza.</param>
public sealed record AbrirApuracaoArt29ACommand(
    int Exercicio,
    int Populacao,
    decimal RepasseRecebido,
    decimal? ReceitaTributaria,
    decimal? Transferencias,
    IReadOnlyList<DespesaCamaraInput> Despesas) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura da apuracao do art. 29-A.</summary>
public sealed class AbrirApuracaoArt29AValidator : AbstractValidator<AbrirApuracaoArt29ACommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirApuracaoArt29AValidator()
    {
        RuleFor(comando => comando.Exercicio).GreaterThan(0);
        RuleFor(comando => comando.Populacao).GreaterThan(0);
        RuleFor(comando => comando.RepasseRecebido).GreaterThanOrEqualTo(0m);
        RuleFor(comando => comando.ReceitaTributaria).GreaterThanOrEqualTo(0m).When(comando => comando.ReceitaTributaria.HasValue);
        RuleFor(comando => comando.Transferencias).GreaterThanOrEqualTo(0m).When(comando => comando.Transferencias.HasValue);
        RuleFor(comando => comando.Despesas).NotNull();
        RuleForEach(comando => comando.Despesas).ChildRules(despesa =>
        {
            despesa.RuleFor(item => item.Natureza).Must(valor => Enum.IsDefined(typeof(NaturezaDespesaCamara), valor))
                .WithMessage("Natureza de despesa invalida.");
            despesa.RuleFor(item => item.Valor).GreaterThanOrEqualTo(0m);
        });
    }
}

/// <summary>Handler da abertura da apuracao do art. 29-A.</summary>
public sealed class AbrirApuracaoArt29AHandler(
    IApuracaoArt29ARepository apuracoes,
    ILegislativoParametros parametros,
    IConsultaReceitaEmEscopoDedicado consultaReceita,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<AbrirApuracaoArt29ACommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirApuracaoArt29ACommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Unicidade logica: uma apuracao por exercicio/tenant (o demonstrativo do ano).
        if (await apuracoes.ExisteParaExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Ja existe apuracao do art. 29-A para o exercicio {request.Exercicio} neste tenant.");
        }

        var exercicioReferencia = request.Exercicio - 1;
        var baseReceita = await ResolverBaseAsync(request, exercicioReferencia, cancellationToken).ConfigureAwait(false);

        var apuracao = ApuracaoArt29A.Abrir(
            tenant.TenantId,
            request.Exercicio,
            request.Populacao,
            baseReceita,
            request.RepasseRecebido,
            parametros.ParametrosArt29A());

        foreach (var despesa in request.Despesas)
        {
            apuracao.LancarDespesa((NaturezaDespesaCamara)despesa.Natureza, despesa.Valor, despesa.Descricao);
        }

        apuracoes.Adicionar(apuracao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return apuracao.Id.Value;
    }

    private async Task<ReceitaBaseArt29A> ResolverBaseAsync(
        AbrirApuracaoArt29ACommand request,
        int exercicioReferencia,
        CancellationToken cancellationToken)
    {
        // Caminho 1 — base INFORMADA (entrada auditada): usada quando a Camara nao detem a receita do
        // municipio (tenants distintos) ou quando se quer fixar valores oficiais do balanco.
        if (request.ReceitaTributaria.HasValue && request.Transferencias.HasValue)
        {
            return ReceitaBaseArt29A.De(exercicioReferencia, request.ReceitaTributaria.Value, request.Transferencias.Value);
        }

        // Caminho 2 — base OBTIDA de Financas via Contracts (escopo dedicado, sem ferir a guarda H5),
        // quando o tenant corrente detem a contabilidade municipal.
        var doFinancas = await consultaReceita.ConsultarBaseAsync(exercicioReferencia, cancellationToken).ConfigureAwait(false);
        if (doFinancas is not null)
        {
            return ReceitaBaseArt29A.De(exercicioReferencia, doFinancas.ReceitaTributaria, doFinancas.Transferencias);
        }

        throw new InvalidOperationException(
            $"Base de receita do exercicio {exercicioReferencia} nao informada e indisponivel em Financas para este tenant.");
    }
}
