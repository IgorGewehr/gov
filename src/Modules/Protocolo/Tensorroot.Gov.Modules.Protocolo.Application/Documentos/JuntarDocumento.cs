using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Documentos;

/// <summary>
/// Junta um documento (PDF/A + hash SHA-256) a um processo, tornando-o imutavel e parte da trilha
/// documental (Lei 11.419/2006; e-ARQ Brasil).
/// </summary>
/// <param name="ProcessoId">Processo de destino.</param>
/// <param name="Hash">Resumo SHA-256 do conteudo (64 caracteres hexadecimais).</param>
/// <param name="Criticidade">Criticidade do ato (determina o nivel minimo de assinatura).</param>
/// <param name="NivelAcesso">Visibilidade do documento.</param>
/// <param name="FormatoPdfA">Conformidade com PDF/A (deve ser <c>true</c>).</param>
public sealed record JuntarDocumentoCommand(
    Guid ProcessoId,
    string Hash,
    CriticidadeAto Criticidade,
    NivelDeAcesso NivelAcesso,
    bool FormatoPdfA) : ICommand<Guid>;

/// <summary>Regras de validacao da juntada de documento.</summary>
public sealed class JuntarDocumentoValidator : AbstractValidator<JuntarDocumentoCommand>
{
    /// <summary>Define as regras.</summary>
    public JuntarDocumentoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
        RuleFor(comando => comando.Hash).NotEmpty().Length(Hash.ComprimentoSha256);
        RuleFor(comando => comando.Criticidade).IsInEnum();
        RuleFor(comando => comando.NivelAcesso).IsInEnum();
        RuleFor(comando => comando.FormatoPdfA).Equal(true);
    }
}

/// <summary>Handler da juntada de documento ao processo.</summary>
public sealed class JuntarDocumentoHandler(
    IDocumentoRepository documentos,
    IProcessoRepository processos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<JuntarDocumentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(JuntarDocumentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processoExiste = await processos.ExisteAsync(request.ProcessoId, cancellationToken).ConfigureAwait(false);
        if (!processoExiste)
        {
            throw new InvalidOperationException("Processo nao encontrado.");
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var documento = Documento.Criar(
            tenant.TenantId,
            Hash.De(request.Hash),
            request.Criticidade,
            request.NivelAcesso,
            request.FormatoPdfA);

        documento.Juntar(request.ProcessoId, hoje);

        documentos.Adicionar(documento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return documento.Id.Value;
    }
}
