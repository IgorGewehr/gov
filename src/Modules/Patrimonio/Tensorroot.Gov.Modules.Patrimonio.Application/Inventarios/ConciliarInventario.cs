using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Projeção de uma divergência apurada (leitura).</summary>
/// <param name="Id">Identificador da divergência.</param>
/// <param name="Tipo">Tipo (Falta/Sobra/DivergenciaLocalizacao/DivergenciaEstado/DivergenciaValor).</param>
/// <param name="BemPatrimonialId">Bem relacionado (nulo para sobra).</param>
/// <param name="Descricao">Descrição do achado.</param>
/// <param name="Recomendacao">Recomendação de efetivação.</param>
public sealed record DivergenciaInventarioDto(
    Guid Id,
    string Tipo,
    Guid? BemPatrimonialId,
    string Descricao,
    string Recomendacao);

/// <summary>Concilia físico × contábil do inventário e retorna as divergências apuradas.</summary>
/// <param name="InventarioId">Inventário a conciliar.</param>
public sealed record ConciliarInventarioCommand(Guid InventarioId) : ICommand<IReadOnlyList<DivergenciaInventarioDto>>;

/// <summary>Regras de validação da conciliação.</summary>
public sealed class ConciliarInventarioValidator : AbstractValidator<ConciliarInventarioCommand>
{
    /// <summary>Define as regras.</summary>
    public ConciliarInventarioValidator() => RuleFor(comando => comando.InventarioId).NotEmpty();
}

/// <summary>Handler da conciliação de inventário.</summary>
public sealed class ConciliarInventarioHandler(
    IInventarioRepository inventarios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConciliarInventarioCommand, IReadOnlyList<DivergenciaInventarioDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DivergenciaInventarioDto>> Handle(
        ConciliarInventarioCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        var divergencias = inventario.Conciliar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return divergencias
            .Select(d => new DivergenciaInventarioDto(
                d.Id.Value,
                d.Tipo.ToString(),
                d.BemPatrimonialId?.Value,
                d.Descricao,
                d.Recomendacao.ToString()))
            .ToList();
    }
}
