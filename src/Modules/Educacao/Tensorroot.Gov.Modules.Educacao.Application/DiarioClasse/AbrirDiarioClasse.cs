using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>Abre um diario de classe para uma matricula ativa (vinculo 1-1 — I-7).</summary>
/// <param name="MatriculaId">Matricula a vincular.</param>
/// <param name="CargaHorariaTotal">Carga horaria anual de referencia (800h/1.000h).</param>
public sealed record AbrirDiarioClasseCommand(Guid MatriculaId, int CargaHorariaTotal) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de diario.</summary>
public sealed class AbrirDiarioClasseValidator : AbstractValidator<AbrirDiarioClasseCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirDiarioClasseValidator()
    {
        RuleFor(comando => comando.MatriculaId).NotEmpty().WithMessage("Matricula obrigatoria.");
        RuleFor(comando => comando.CargaHorariaTotal).GreaterThan(0).WithMessage("Carga horaria total deve ser maior que zero.");
    }
}

/// <summary>Handler da abertura de diario.</summary>
public sealed class AbrirDiarioClasseHandler(
    IDiarioClasseRepository diarios,
    IMatriculaRepository matriculas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirDiarioClasseCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirDiarioClasseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var matriculaId = new MatriculaId(request.MatriculaId);

        var matricula = await matriculas.ObterPorIdAsync(matriculaId, cancellationToken).ConfigureAwait(false);
        if (matricula is null || matricula.Situacao != SituacaoMatricula.Ativa)
        {
            throw new InvalidOperationException("Matricula invalida para abertura de diario.");
        }

        if (await diarios.ExisteParaMatriculaAsync(matriculaId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Diario ja existe para a matricula.");
        }

        var diario = DiarioClasseAggregate.Abrir(tenant.TenantId, matriculaId, request.CargaHorariaTotal);

        diarios.Adicionar(diario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return diario.Id.Value;
    }
}
