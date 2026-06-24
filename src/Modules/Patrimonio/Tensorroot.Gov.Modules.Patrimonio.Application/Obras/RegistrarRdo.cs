using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>Registra um Relatório Diário de Obra (RDO) — fiscalização contínua (I-8/I-9).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="Data">Data do RDO (única por obra — I-9).</param>
/// <param name="CondicaoTempo">Condição de tempo/clima.</param>
/// <param name="EfetivoMaoDeObra">Efetivo de mão de obra.</param>
/// <param name="EquipamentosMobilizados">Equipamentos mobilizados.</param>
/// <param name="AtividadesExecutadas">Atividades executadas.</param>
/// <param name="ResponsavelTecnicoId">Responsável técnico.</param>
/// <param name="Ocorrencias">Ocorrências do dia (opcional).</param>
public sealed record RegistrarRdoCommand(
    Guid ObraId,
    DateOnly Data,
    string CondicaoTempo,
    int EfetivoMaoDeObra,
    string EquipamentosMobilizados,
    string AtividadesExecutadas,
    Guid ResponsavelTecnicoId,
    string? Ocorrencias) : ICommand<Guid>;

/// <summary>Regras de validação do registro de RDO.</summary>
public sealed class RegistrarRdoValidator : AbstractValidator<RegistrarRdoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarRdoValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.CondicaoTempo).NotEmpty().MaximumLength(60);
        RuleFor(comando => comando.EfetivoMaoDeObra).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.EquipamentosMobilizados).NotEmpty().MaximumLength(1000);
        RuleFor(comando => comando.AtividadesExecutadas).NotEmpty().MaximumLength(2000);
        RuleFor(comando => comando.ResponsavelTecnicoId).NotEmpty();
    }
}

/// <summary>Handler do registro de RDO.</summary>
public sealed class RegistrarRdoHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarRdoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarRdoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        var rdoId = obra.RegistrarRdo(
            request.Data,
            request.CondicaoTempo,
            request.EfetivoMaoDeObra,
            request.EquipamentosMobilizados,
            request.AtividadesExecutadas,
            request.ResponsavelTecnicoId,
            request.Ocorrencias);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rdoId.Value;
    }
}
