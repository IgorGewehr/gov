using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Pbf;

/// <summary>
/// 3d.1: abre (idempotente por (familia, competencia)) o acompanhamento de condicionalidades do PBF de
/// uma familia beneficiaria. Vincula-se a familia do CadUnico por Id (valida a existencia no tenant).
/// </summary>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Competencia">Competencia (ano/mes) do periodo de acompanhamento.</param>
public sealed record AbrirAcompanhamentoCondicionalidadeCommand(Guid FamiliaId, Competencia Competencia) : ICommand<Guid>;

/// <summary>Validacao da abertura do acompanhamento.</summary>
public sealed class AbrirAcompanhamentoCondicionalidadeValidator : AbstractValidator<AbrirAcompanhamentoCondicionalidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirAcompanhamentoCondicionalidadeValidator()
    {
        RuleFor(c => c.FamiliaId).NotEmpty().WithMessage("Familia e obrigatoria.");
        RuleFor(c => c.Competencia).Must(c => c.EhValida()).WithMessage("Competencia (ano/mes) e obrigatoria e valida.");
    }
}

/// <summary>Handler da abertura do acompanhamento de condicionalidades.</summary>
public sealed class AbrirAcompanhamentoCondicionalidadeHandler(
    IAcompanhamentoCondicionalidadeRepository acompanhamentos,
    IFamiliaRepository familias,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirAcompanhamentoCondicionalidadeCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirAcompanhamentoCondicionalidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var familiaId = new FamiliaId(request.FamiliaId);

        // A familia do CadUnico deve existir no tenant (FK logica) — I-9 (base federal autoritativa).
        _ = await familias.ObterPorIdAsync(familiaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Familia inexistente no tenant.");

        var existente = await acompanhamentos.ObterPorFamiliaCompetenciaAsync(familiaId, request.Competencia, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            return existente.Id.Value; // idempotente na competencia.
        }

        var acompanhamento = AcompanhamentoCondicionalidade.Abrir(tenant.TenantId, familiaId, request.Competencia);
        await acompanhamentos.AdicionarAsync(acompanhamento, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return acompanhamento.Id.Value;
    }
}

/// <summary>
/// 3d.1: registra uma condicionalidade (educacao/saude) de um membro no acompanhamento, recalculando o
/// efeito gradativo. Pode ser manual (equipe do CRAS) e/ou alimentado por evento de Educacao/Saude
/// (frequencia/vacina) consumido via Contracts.
/// </summary>
/// <param name="AcompanhamentoId">Acompanhamento de condicionalidades.</param>
/// <param name="Tipo">Eixo da condicionalidade.</param>
/// <param name="MembroId">Membro da familia ao qual a condicionalidade se aplica.</param>
/// <param name="Status">Status do cumprimento.</param>
/// <param name="Observacao">Observacao/motivo (obrigatorio na justificativa).</param>
public sealed record RegistrarCondicionalidadeCommand(
    Guid AcompanhamentoId,
    TipoCondicionalidade Tipo,
    Guid MembroId,
    StatusCondicionalidade Status,
    string? Observacao) : ICommand<Guid>;

/// <summary>Validacao do registro de condicionalidade.</summary>
public sealed class RegistrarCondicionalidadeValidator : AbstractValidator<RegistrarCondicionalidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarCondicionalidadeValidator()
    {
        RuleFor(c => c.AcompanhamentoId).NotEmpty();
        RuleFor(c => c.MembroId).NotEmpty().WithMessage("Membro da familia e obrigatorio.");
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.Status).IsInEnum();
    }
}

/// <summary>Handler do registro de condicionalidade.</summary>
public sealed class RegistrarCondicionalidadeHandler(
    IAcompanhamentoCondicionalidadeRepository acompanhamentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarCondicionalidadeCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarCondicionalidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var acompanhamento = await acompanhamentos.ObterPorIdAsync(new AcompanhamentoCondicionalidadeId(request.AcompanhamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Acompanhamento de condicionalidades inexistente no tenant.");

        var registroId = acompanhamento.RegistrarCondicionalidade(request.Tipo, request.MembroId, request.Status, request.Observacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return registroId.Value;
    }
}

/// <summary>
/// 3d.1: justifica um descumprimento (motivo do CRAS), retirando-o da contagem de descumprimentos
/// efetivos e recalculando o efeito gradativo.
/// </summary>
/// <param name="AcompanhamentoId">Acompanhamento de condicionalidades.</param>
/// <param name="RegistroId">Registro de condicionalidade descumprida.</param>
/// <param name="Motivo">Motivo da justificativa (obrigatorio).</param>
public sealed record JustificarDescumprimentoCommand(Guid AcompanhamentoId, Guid RegistroId, string Motivo) : ICommand;

/// <summary>Validacao da justificativa de descumprimento.</summary>
public sealed class JustificarDescumprimentoValidator : AbstractValidator<JustificarDescumprimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public JustificarDescumprimentoValidator()
    {
        RuleFor(c => c.AcompanhamentoId).NotEmpty();
        RuleFor(c => c.RegistroId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().WithMessage("A justificativa exige um motivo.");
    }
}

/// <summary>Handler da justificativa de descumprimento.</summary>
public sealed class JustificarDescumprimentoHandler(
    IAcompanhamentoCondicionalidadeRepository acompanhamentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<JustificarDescumprimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(JustificarDescumprimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var acompanhamento = await acompanhamentos.ObterPorIdAsync(new AcompanhamentoCondicionalidadeId(request.AcompanhamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Acompanhamento de condicionalidades inexistente no tenant.");

        acompanhamento.JustificarDescumprimento(new RegistroCondicionalidadeId(request.RegistroId), request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
