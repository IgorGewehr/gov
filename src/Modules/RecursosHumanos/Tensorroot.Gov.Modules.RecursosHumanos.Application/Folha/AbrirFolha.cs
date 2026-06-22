using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Abre uma folha de pagamento para uma competencia (uma folha por competencia por tenant — I-1).</summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
public sealed record AbrirFolhaCommand(int Ano, int Mes) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de folha.</summary>
public sealed class AbrirFolhaValidator : AbstractValidator<AbrirFolhaCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirFolhaValidator()
    {
        RuleFor(comando => comando.Ano)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Ano da competencia invalido.");
        RuleFor(comando => comando.Mes)
            .InclusiveBetween(1, 12)
            .WithMessage("Mes da competencia deve estar entre 1 e 12.");
    }
}

/// <summary>Handler da abertura de folha.</summary>
public sealed class AbrirFolhaHandler(
    IFolhaDePagamentoRepository folhas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirFolhaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.Ano, request.Mes);

        // I-1: uma folha por competencia por tenant.
        if (await folhas.ExisteParaCompetenciaAsync(competencia, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Folha ja aberta para a competencia.");
        }

        var folha = FolhaDePagamento.Abrir(tenant.TenantId, competencia);
        folhas.Adicionar(folha);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return folha.Id.Value;
    }
}
