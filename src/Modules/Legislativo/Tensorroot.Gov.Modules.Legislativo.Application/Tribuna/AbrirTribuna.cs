using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Tribuna;

/// <summary>Abre a tribuna de uma sessao (uma por sessao) — T-1.</summary>
/// <param name="SessaoId">Sessao alvo.</param>
/// <param name="TempoPadraoSegundos">Tempo padrao por orador, em segundos (opcional; parametrizavel por tenant).</param>
public sealed record AbrirTribunaCommand(Guid SessaoId, int? TempoPadraoSegundos) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de tribuna.</summary>
public sealed class AbrirTribunaValidator : AbstractValidator<AbrirTribunaCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirTribunaValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
        RuleFor(comando => comando.TempoPadraoSegundos!.Value)
            .GreaterThan(0)
            .When(comando => comando.TempoPadraoSegundos.HasValue);
    }
}

/// <summary>Handler da abertura de tribuna (impede duplicidade por sessao).</summary>
public sealed class AbrirTribunaHandler(
    ITribunaSessaoRepository tribunas,
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<AbrirTribunaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirTribunaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessaoId = new SessaoId(request.SessaoId);
        var existente = await tribunas.ObterPorSessaoAsync(sessaoId, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            return existente.Id.Value; // idempotente: uma tribuna por sessao.
        }

        var sessao = await sessoes.ObterPorIdAsync(sessaoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        var tempo = request.TempoPadraoSegundos is { } segundos ? TimeSpan.FromSeconds(segundos) : (TimeSpan?)null;
        var tribuna = TribunaSessao.Abrir(tenant.TenantId, sessao, tempo);

        tribunas.Adicionar(tribuna);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tribuna.Id.Value;
    }
}
