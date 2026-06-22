using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Acrescenta um adendo datado/reassinado a uma evolucao ja assinada (append-only).</summary>
/// <param name="AtendimentoId">Atendimento alvo.</param>
/// <param name="EvolucaoReferenciadaId">Evolucao assinada a complementar.</param>
/// <param name="Texto">Texto do adendo.</param>
/// <param name="CertificadoIcpBrasil">Identificacao do certificado ICP-Brasil (NGS2).</param>
/// <param name="Hash">Hash criptografico da assinatura do adendo.</param>
public sealed record AdicionarAdendoCommand(
    Guid AtendimentoId,
    Guid EvolucaoReferenciadaId,
    string Texto,
    string CertificadoIcpBrasil,
    string Hash) : ICommand;

/// <summary>Regras de validacao da inclusao de adendo.</summary>
public sealed class AdicionarAdendoValidator : AbstractValidator<AdicionarAdendoCommand>
{
    /// <summary>Comprimento maximo do texto do adendo.</summary>
    public const int ComprimentoMaximoTexto = 4000;

    /// <summary>Define as regras.</summary>
    public AdicionarAdendoValidator()
    {
        RuleFor(comando => comando.AtendimentoId).NotEmpty().WithMessage("Atendimento e obrigatorio.");
        RuleFor(comando => comando.EvolucaoReferenciadaId).NotEmpty().WithMessage("Evolucao referenciada e obrigatoria.");
        RuleFor(comando => comando.Texto)
            .NotEmpty()
            .MaximumLength(ComprimentoMaximoTexto)
            .WithMessage("Texto do adendo e obrigatorio (max. 4000).");
        RuleFor(comando => comando.CertificadoIcpBrasil).NotEmpty().WithMessage("Certificado ICP-Brasil (NGS2) e obrigatorio.");
        RuleFor(comando => comando.Hash).NotEmpty().WithMessage("Hash da assinatura e obrigatorio.");
    }
}

/// <summary>Handler da inclusao de adendo.</summary>
public sealed class AdicionarAdendoHandler(
    IAtendimentoRepository atendimentos,
    IAssinaturaIcpBrasilService assinaturaService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AdicionarAdendoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AdicionarAdendoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atendimento = await atendimentos
            .ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Atendimento nao encontrado.");

        var assinatura = await assinaturaService
            .AssinarAsync(request.CertificadoIcpBrasil, request.Hash, cancellationToken)
            .ConfigureAwait(false);

        atendimento.AdicionarAdendo(
            request.EvolucaoReferenciadaId,
            request.Texto,
            assinatura,
            timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
