using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>
/// Aplica um REAJUSTE SALARIAL EM LOTE (revisao geral anual — CF art. 37, X) a um percentual linear
/// sobre o vencimento-base dos cargos ATIVOS do tenant, opcionalmente restrito a um tipo de cargo.
/// Cada alteracao reusa <see cref="Cargo.AlterarVencimento"/> (que emite <c>VencimentoAlterado</c>,
/// auditado pelo interceptor) — sem dominio anemico, sem desviar da trilha. Idempotencia transacional
/// pelo pipeline; um unico <c>SaveChanges</c> persiste o lote.
/// </summary>
/// <param name="Percentual">Percentual de reajuste (ex.: 5.5 = +5,5%); em (0, 100].</param>
/// <param name="Tipo">Restringe o reajuste a um tipo de cargo (opcional; nulo = todos os ativos).</param>
public sealed record AplicarReajusteEmLoteCommand(decimal Percentual, TipoCargo? Tipo) : ICommand<ReajusteEmLoteResultado>;

/// <summary>Resultado consolidado de um reajuste em lote.</summary>
/// <param name="CargosReajustados">Quantidade de cargos efetivamente reajustados.</param>
/// <param name="Percentual">Percentual aplicado.</param>
public sealed record ReajusteEmLoteResultado(int CargosReajustados, decimal Percentual);

/// <summary>Regras de validacao do reajuste em lote.</summary>
public sealed class AplicarReajusteEmLoteValidator : AbstractValidator<AplicarReajusteEmLoteCommand>
{
    /// <summary>Define as regras.</summary>
    public AplicarReajusteEmLoteValidator()
    {
        RuleFor(comando => comando.Percentual)
            .GreaterThan(ReajusteVencimento.PercentualMinimo)
            .LessThanOrEqualTo(ReajusteVencimento.PercentualMaximo)
            .WithMessage($"Percentual de reajuste deve estar em ({ReajusteVencimento.PercentualMinimo}, {ReajusteVencimento.PercentualMaximo}].");

        RuleFor(comando => comando.Tipo)
            .IsInEnum()
            .When(comando => comando.Tipo is not null)
            .WithMessage("Tipo de cargo invalido.");
    }
}

/// <summary>Handler do reajuste salarial em lote.</summary>
public sealed class AplicarReajusteEmLoteHandler(ICargoRepository cargos, IUnitOfWork unitOfWork)
    : ICommandHandler<AplicarReajusteEmLoteCommand, ReajusteEmLoteResultado>
{
    /// <inheritdoc />
    public async Task<ReajusteEmLoteResultado> Handle(AplicarReajusteEmLoteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alvos = await cargos.ListarAtivosAsync(request.Tipo, cancellationToken).ConfigureAwait(false);

        var reajustados = 0;
        foreach (var cargo in alvos)
        {
            var novoVencimento = ReajusteVencimento.Aplicar(cargo.Vencimento, request.Percentual);
            cargo.AlterarVencimento(novoVencimento);
            reajustados++;
        }

        if (reajustados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new ReajusteEmLoteResultado(reajustados, request.Percentual);
    }
}
