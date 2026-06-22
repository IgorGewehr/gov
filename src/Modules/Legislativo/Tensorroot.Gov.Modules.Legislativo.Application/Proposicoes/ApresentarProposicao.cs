using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Apresenta (protocola) uma nova proposicao legislativa.</summary>
/// <param name="Tipo">Especie da materia (<see cref="TipoProposicao"/>).</param>
/// <param name="Ementa">Resumo do objeto.</param>
/// <param name="Autoria">Autoria (iniciativa).</param>
/// <param name="Regime">Regime de tramitacao (<see cref="RegimeTramitacao"/>).</param>
public sealed record ApresentarProposicaoCommand(int Tipo, string Ementa, string Autoria, int Regime) : ICommand<Guid>;

/// <summary>Regras de validacao da apresentacao de proposicao.</summary>
public sealed class ApresentarProposicaoValidator : AbstractValidator<ApresentarProposicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ApresentarProposicaoValidator()
    {
        // Tipo/Regime trafegam como int; validar contra os enums de dominio (IsInEnum() so vale
        // para propriedade ja tipada como enum — aqui sao Int32 e reprovariam qualquer valor).
        RuleFor(comando => comando.Tipo)
            .Must(tipo => Enum.IsDefined(typeof(Domain.Proposicoes.TipoProposicao), tipo))
            .WithMessage("Tipo de proposicao invalido.");
        RuleFor(comando => comando.Regime)
            .Must(regime => Enum.IsDefined(typeof(Domain.Proposicoes.RegimeTramitacao), regime))
            .WithMessage("Regime de tramitacao invalido.");
        RuleFor(comando => comando.Ementa).NotEmpty().MaximumLength(Domain.Proposicoes.Ementa.ComprimentoMaximo);
        RuleFor(comando => comando.Autoria).NotEmpty().MaximumLength(Domain.Proposicoes.Autoria.ComprimentoMaximo);
    }
}

/// <summary>Handler da apresentacao de proposicao.</summary>
public sealed class ApresentarProposicaoHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ApresentarProposicaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ApresentarProposicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var proposicao = Proposicao.Apresentar(
            tenant.TenantId,
            (TipoProposicao)request.Tipo,
            Ementa.De(request.Ementa),
            Autoria.De(request.Autoria),
            (RegimeTramitacao)request.Regime,
            hoje);

        proposicoes.Adicionar(proposicao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return proposicao.Id.Value;
    }
}
