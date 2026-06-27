using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Credenciamentos;

/// <summary>Abre um edital de credenciamento (rascunho — art. 78, I e art. 79, Lei 14.133/2021).</summary>
/// <param name="Objeto">Descricao do objeto.</param>
/// <param name="Hipotese">Hipotese autorizadora (art. 79, I a III).</param>
/// <param name="VigenciaInicio">Inicio da vigencia do edital.</param>
/// <param name="VigenciaFim">Fim da vigencia do edital.</param>
/// <param name="FundamentacaoLegal">Amparo legal (ex.: "Art. 74, IV c/c art. 79, I").</param>
/// <param name="EtpId">Referencia ao ETP (opcional).</param>
/// <param name="TermoReferenciaId">Referencia ao TR (opcional).</param>
public sealed record AbrirCredenciamentoCommand(
    string Objeto,
    HipoteseCredenciamento Hipotese,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    string FundamentacaoLegal,
    Guid? EtpId,
    Guid? TermoReferenciaId) : ICommand<Guid>;

/// <summary>Adiciona um item credenciavel (preco fixado) ao edital em elaboracao.</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="ItemCatalogoId">Referencia ao catalogo (opcional).</param>
/// <param name="Descricao">Descricao do objeto credenciavel.</param>
/// <param name="UnidadeMedida">Unidade de prestacao/medida.</param>
/// <param name="PrecoFixado">Preco unitario fixado pela Administracao.</param>
public sealed record AdicionarItemCredenciamentoCommand(
    Guid CredenciamentoId,
    Guid? ItemCatalogoId,
    string Descricao,
    string UnidadeMedida,
    decimal PrecoFixado) : ICommand<Guid>;

/// <summary>Remove um item do edital em elaboracao.</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="ItemId">Item a remover.</param>
public sealed record RemoverItemCredenciamentoCommand(Guid CredenciamentoId, Guid ItemId) : ICommand;

/// <summary>Publica o chamamento publico do credenciamento (abre inscricoes permanentes — art. 79, par. unico).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="NumeroEdital">Numero/identificador do edital de chamamento.</param>
public sealed record PublicarChamamentoCommand(Guid CredenciamentoId, string NumeroEdital) : ICommand;

/// <summary>Suspende/reabre o recebimento de inscricoes (ato motivado).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="Suspender"><c>true</c> para suspender; <c>false</c> para reabrir.</param>
/// <param name="Motivo">Motivacao da suspensao (exigida ao suspender).</param>
public sealed record AlterarChamamentoCommand(Guid CredenciamentoId, bool Suspender, string? Motivo) : ICommand;

/// <summary>Encerra/anula/revoga o edital de credenciamento (terminal).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="Situacao">Situacao terminal alvo (Encerrado, Anulado ou Revogado).</param>
/// <param name="Motivo">Motivacao do ato administrativo.</param>
public sealed record EncerrarCredenciamentoCommand(Guid CredenciamentoId, SituacaoCredenciamento Situacao, string Motivo) : ICommand;

/// <summary>Validacao da abertura do credenciamento.</summary>
public sealed class AbrirCredenciamentoValidator : AbstractValidator<AbrirCredenciamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirCredenciamentoValidator()
    {
        RuleFor(c => c.Objeto).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Hipotese).IsInEnum();
        RuleFor(c => c.FundamentacaoLegal).NotEmpty().MaximumLength(500);
        RuleFor(c => c.VigenciaFim).GreaterThanOrEqualTo(c => c.VigenciaInicio);
    }
}

/// <summary>Validacao da inclusao de item.</summary>
public sealed class AdicionarItemCredenciamentoValidator : AbstractValidator<AdicionarItemCredenciamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarItemCredenciamentoValidator()
    {
        RuleFor(c => c.CredenciamentoId).NotEmpty();
        RuleFor(c => c.Descricao).NotEmpty().MaximumLength(1000);
        RuleFor(c => c.UnidadeMedida).NotEmpty().MaximumLength(50);
        RuleFor(c => c.PrecoFixado).GreaterThan(0);
    }
}

/// <summary>Validacao do encerramento.</summary>
public sealed class EncerrarCredenciamentoValidator : AbstractValidator<EncerrarCredenciamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarCredenciamentoValidator()
    {
        RuleFor(c => c.CredenciamentoId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(1000);
        RuleFor(c => c.Situacao)
            .Must(s => s is SituacaoCredenciamento.Encerrado or SituacaoCredenciamento.Anulado or SituacaoCredenciamento.Revogado)
            .WithMessage("Situacao terminal deve ser Encerrado, Anulado ou Revogado.");
    }
}

/// <summary>Handler da abertura do credenciamento.</summary>
public sealed class AbrirCredenciamentoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<AbrirCredenciamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirCredenciamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = Credenciamento.Abrir(
            tenant.TenantId,
            request.Objeto,
            request.Hipotese,
            request.VigenciaInicio,
            request.VigenciaFim,
            request.FundamentacaoLegal,
            request.EtpId,
            request.TermoReferenciaId);
        credenciamentos.Adicionar(credenciamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return credenciamento.Id.Value;
    }
}

/// <summary>Handler da inclusao de item credenciavel.</summary>
public sealed class AdicionarItemCredenciamentoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarItemCredenciamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarItemCredenciamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        var itemId = credenciamento.AdicionarItem(
            request.ItemCatalogoId,
            request.Descricao,
            request.UnidadeMedida,
            ValorMonetario.De(request.PrecoFixado));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return itemId.Value;
    }
}

/// <summary>Handler da remocao de item.</summary>
public sealed class RemoverItemCredenciamentoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoverItemCredenciamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RemoverItemCredenciamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        credenciamento.RemoverItem(new ItemCredenciamentoId(request.ItemId));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da publicacao do chamamento.</summary>
public sealed class PublicarChamamentoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<PublicarChamamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarChamamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        credenciamento.PublicarChamamento(request.NumeroEdital);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da suspensao/reabertura do chamamento.</summary>
public sealed class AlterarChamamentoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<AlterarChamamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AlterarChamamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        if (request.Suspender)
        {
            credenciamento.SuspenderChamamento(request.Motivo ?? throw new InvalidOperationException("Motivo da suspensao e obrigatorio."));
        }
        else
        {
            credenciamento.ReabrirChamamento();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do encerramento/anulacao/revogacao do edital.</summary>
public sealed class EncerrarCredenciamentoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarCredenciamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarCredenciamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        switch (request.Situacao)
        {
            case SituacaoCredenciamento.Encerrado:
                credenciamento.Encerrar(request.Motivo);
                break;
            case SituacaoCredenciamento.Anulado:
                credenciamento.Anular(request.Motivo);
                break;
            case SituacaoCredenciamento.Revogado:
                credenciamento.Revogar(request.Motivo);
                break;
            default:
                throw new InvalidOperationException("Situacao terminal invalida para encerramento.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
