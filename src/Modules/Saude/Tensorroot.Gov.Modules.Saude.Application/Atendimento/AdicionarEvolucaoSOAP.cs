using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Inclui uma nota SOAP no atendimento em andamento.</summary>
/// <param name="AtendimentoId">Atendimento alvo.</param>
/// <param name="Subjetivo">Componente Subjetivo.</param>
/// <param name="Objetivo">Componente Objetivo.</param>
/// <param name="Avaliacao">Componente Avaliacao.</param>
/// <param name="Plano">Componente Plano.</param>
/// <param name="Cid">Diagnostico CID-10 (opcional).</param>
/// <param name="Ciap">Diagnostico CIAP-2 (opcional).</param>
public sealed record AdicionarEvolucaoSOAPCommand(
    Guid AtendimentoId,
    string Subjetivo,
    string Objetivo,
    string Avaliacao,
    string Plano,
    string? Cid,
    string? Ciap) : ICommand;

/// <summary>Regras de validacao da inclusao de evolucao SOAP.</summary>
public sealed class AdicionarEvolucaoSOAPValidator : AbstractValidator<AdicionarEvolucaoSOAPCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarEvolucaoSOAPValidator()
    {
        RuleFor(comando => comando.AtendimentoId).NotEmpty().WithMessage("Atendimento e obrigatorio.");
        RuleFor(comando => comando)
            .Must(AoMenosUmCampoPreenchido)
            .WithMessage("Informe ao menos um campo SOAP.");
        RuleFor(comando => comando.Cid)
            .Must(cid => Domain.Atendimento.Cid.EhValido(cid))
            .When(comando => !string.IsNullOrWhiteSpace(comando.Cid))
            .WithMessage("CID-10 invalido.");
        RuleFor(comando => comando.Ciap)
            .Must(ciap => Domain.Atendimento.Ciap.EhValido(ciap))
            .When(comando => !string.IsNullOrWhiteSpace(comando.Ciap))
            .WithMessage("CIAP-2 invalido.");
    }

    private static bool AoMenosUmCampoPreenchido(AdicionarEvolucaoSOAPCommand comando)
        => !string.IsNullOrWhiteSpace(comando.Subjetivo)
            || !string.IsNullOrWhiteSpace(comando.Objetivo)
            || !string.IsNullOrWhiteSpace(comando.Avaliacao)
            || !string.IsNullOrWhiteSpace(comando.Plano);
}

/// <summary>Handler da inclusao de evolucao SOAP.</summary>
public sealed class AdicionarEvolucaoSOAPHandler(
    IAtendimentoRepository atendimentos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AdicionarEvolucaoSOAPCommand>
{
    /// <inheritdoc />
    public async Task Handle(AdicionarEvolucaoSOAPCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atendimento = await atendimentos
            .ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Atendimento nao encontrado.");

        var cid = string.IsNullOrWhiteSpace(request.Cid) ? (Cid?)null : Cid.De(request.Cid);
        var ciap = string.IsNullOrWhiteSpace(request.Ciap) ? (Ciap?)null : Ciap.De(request.Ciap);

        atendimento.AdicionarEvolucaoSOAP(
            request.Subjetivo,
            request.Objetivo,
            request.Avaliacao,
            request.Plano,
            timeProvider.GetUtcNow(),
            cid,
            ciap);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
