using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>Designa o fiscal/gestor do contrato de obra (Lei 14.133/2021, art. 117 — I-10).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="FiscalId">Servidor designado.</param>
/// <param name="Desde">Data de início da vigência.</param>
/// <param name="AtoDesignacao">Ato de designação (portaria/ofício).</param>
public sealed record DesignarFiscalCommand(Guid ObraId, Guid FiscalId, DateOnly Desde, string AtoDesignacao) : ICommand;

/// <summary>Regras de validação da designação de fiscal.</summary>
public sealed class DesignarFiscalValidator : AbstractValidator<DesignarFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public DesignarFiscalValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.FiscalId).NotEmpty();
        RuleFor(comando => comando.AtoDesignacao).NotEmpty().MaximumLength(200);
    }
}

/// <summary>Handler da designação de fiscal.</summary>
public sealed class DesignarFiscalHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<DesignarFiscalCommand>
{
    /// <inheritdoc />
    public async Task Handle(DesignarFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        obra.DesignarFiscal(request.FiscalId, request.Desde, request.AtoDesignacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Registra uma ocorrência de fiscalização (notificação/advertência/registro técnico — art. 117).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="Data">Data da ocorrência.</param>
/// <param name="Tipo">Tipo (1=Notificacao, 2=Advertencia, 3=RegistroTecnico).</param>
/// <param name="Descricao">Descrição do fato.</param>
/// <param name="RegistradaPorId">Servidor que registra.</param>
public sealed record RegistrarOcorrenciaCommand(
    Guid ObraId,
    DateOnly Data,
    int Tipo,
    string Descricao,
    Guid RegistradaPorId) : ICommand<Guid>;

/// <summary>Regras de validação do registro de ocorrência.</summary>
public sealed class RegistrarOcorrenciaValidator : AbstractValidator<RegistrarOcorrenciaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarOcorrenciaValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.Tipo)
            .Must(tipo => Enum.IsDefined(typeof(TipoOcorrenciaFiscalizacao), tipo))
            .WithMessage("Tipo de ocorrência inválido (1=Notificacao, 2=Advertencia, 3=RegistroTecnico).");
        RuleFor(comando => comando.Descricao).NotEmpty().MaximumLength(1000);
        RuleFor(comando => comando.RegistradaPorId).NotEmpty();
    }
}

/// <summary>Handler do registro de ocorrência.</summary>
public sealed class RegistrarOcorrenciaHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarOcorrenciaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarOcorrenciaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        var ocorrenciaId = obra.RegistrarOcorrencia(
            request.Data,
            (TipoOcorrenciaFiscalizacao)request.Tipo,
            request.Descricao,
            request.RegistradaPorId);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ocorrenciaId.Value;
    }
}

/// <summary>Paralisa a obra (suspende novas medições/RDOs — I-15).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="Motivo">Motivo da paralisação (1..5).</param>
/// <param name="Data">Data da paralisação.</param>
public sealed record ParalisarObraCommand(Guid ObraId, int Motivo, DateOnly Data) : ICommand;

/// <summary>Regras de validação da paralisação.</summary>
public sealed class ParalisarObraValidator : AbstractValidator<ParalisarObraCommand>
{
    /// <summary>Define as regras.</summary>
    public ParalisarObraValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.Motivo)
            .Must(motivo => Enum.IsDefined(typeof(MotivoParalisacao), motivo))
            .WithMessage("Motivo de paralisação inválido (1..5).");
    }
}

/// <summary>Handler da paralisação.</summary>
public sealed class ParalisarObraHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<ParalisarObraCommand>
{
    /// <inheritdoc />
    public async Task Handle(ParalisarObraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        obra.Paralisar((MotivoParalisacao)request.Motivo, request.Data);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Reinicia a obra paralisada (Paralisada → EmExecucao).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="Data">Data do reinício.</param>
public sealed record ReiniciarObraCommand(Guid ObraId, DateOnly Data) : ICommand;

/// <summary>Handler do reinício.</summary>
public sealed class ReiniciarObraHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<ReiniciarObraCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReiniciarObraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        obra.Reiniciar(request.Data);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
