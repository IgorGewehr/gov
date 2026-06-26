using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;

namespace Tensorroot.Gov.Modules.Tributos.Application.Domicilio;

/// <summary>
/// Adere um contribuinte ao Domicílio Eletrônico do Contribuinte (DEC): cria a caixa postal fiscal com
/// efeito legal de intimação a partir da adesão. Idempotente por contribuinte (um domicílio ativo por
/// contribuinte). Paridade com o incumbente SAPI.
/// </summary>
/// <param name="ContribuinteId">Contribuinte titular.</param>
/// <param name="DataAdesao">Data de adesão (data do fato).</param>
/// <param name="DiasCienciaTacita">Prazo (dias) para a ciência tácita (parametrizável); padrão 15.</param>
public sealed record AderirDomicilioEletronicoCommand(
    Guid ContribuinteId,
    DateOnly DataAdesao,
    int DiasCienciaTacita = DomicilioEletronicoContribuinte.DiasCienciaTacitaPadrao) : ICommand<Guid>;

/// <summary>Regras de validação da adesão ao Domicílio Eletrônico.</summary>
public sealed class AderirDomicilioEletronicoValidator : AbstractValidator<AderirDomicilioEletronicoCommand>
{
    /// <summary>Define as regras.</summary>
    public AderirDomicilioEletronicoValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.DiasCienciaTacita).GreaterThanOrEqualTo(1);
    }
}

/// <summary>Handler da adesão ao Domicílio Eletrônico.</summary>
public sealed class AderirDomicilioEletronicoHandler(
    IContribuinteRepository contribuintes,
    IDomicilioEletronicoContribuinteRepository domicilios,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AderirDomicilioEletronicoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AderirDomicilioEletronicoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        _ = await contribuintes.ObterPorIdAsync(contribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte não encontrado.");

        var existente = await domicilios.ObterAtivoPorContribuinteAsync(contribuinteId, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            throw new InvalidOperationException("O contribuinte já possui domicílio eletrônico ativo.");
        }

        var domicilio = DomicilioEletronicoContribuinte.Aderir(
            tenant.TenantId,
            contribuinteId,
            request.DataAdesao,
            request.DiasCienciaTacita);
        domicilios.Adicionar(domicilio);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return domicilio.Id.Value;
    }
}

/// <summary>Resultado da disponibilização de uma mensagem fiscal.</summary>
/// <param name="MensagemId">Mensagem disponibilizada.</param>
/// <param name="DataLimiteCienciaTacita">Data-limite para a ciência tácita (decurso de prazo).</param>
public sealed record ResultadoMensagemFiscal(Guid MensagemId, DateOnly DataLimiteCienciaTacita);

/// <summary>
/// Disponibiliza (envia) uma mensagem fiscal ao Domicílio Eletrônico de um contribuinte: a comunicação
/// passa a contar prazo para a ciência tácita e, a partir da ciência, o prazo de manifestação/pagamento.
/// </summary>
/// <param name="ContribuinteId">Contribuinte destinatário (titular do domicílio).</param>
/// <param name="Tipo">Tipo da comunicação (intimação/notificação/aviso).</param>
/// <param name="Assunto">Assunto.</param>
/// <param name="Corpo">Corpo da comunicação.</param>
/// <param name="DataDisponibilizacao">Data de disponibilização (data do fato).</param>
/// <param name="DiasPrazoManifestacao">Prazo (dias) de manifestação contado da ciência; 0 se sem prazo.</param>
/// <param name="ReferenciaExterna">Referência ao ato de origem (ex.: nº do lançamento/CDA), opcional.</param>
public sealed record DisponibilizarMensagemFiscalCommand(
    Guid ContribuinteId,
    TipoMensagemFiscal Tipo,
    string Assunto,
    string Corpo,
    DateOnly DataDisponibilizacao,
    int DiasPrazoManifestacao,
    string? ReferenciaExterna) : ICommand<ResultadoMensagemFiscal>;

/// <summary>Regras de validação da disponibilização de mensagem fiscal.</summary>
public sealed class DisponibilizarMensagemFiscalValidator : AbstractValidator<DisponibilizarMensagemFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public DisponibilizarMensagemFiscalValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.Assunto).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Corpo).NotEmpty().MaximumLength(8000);
        RuleFor(c => c.DiasPrazoManifestacao).GreaterThanOrEqualTo(0);
        RuleFor(c => c.ReferenciaExterna).MaximumLength(60);
    }
}

/// <summary>Handler da disponibilização de mensagem fiscal.</summary>
public sealed class DisponibilizarMensagemFiscalHandler(
    IDomicilioEletronicoContribuinteRepository domicilios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DisponibilizarMensagemFiscalCommand, ResultadoMensagemFiscal>
{
    /// <inheritdoc />
    public async Task<ResultadoMensagemFiscal> Handle(DisponibilizarMensagemFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var domicilio = await domicilios.ObterAtivoPorContribuinteAsync(contribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("O contribuinte não possui domicílio eletrônico ativo.");

        var mensagem = domicilio.Disponibilizar(
            request.Tipo,
            request.Assunto,
            request.Corpo,
            request.DataDisponibilizacao,
            request.DiasPrazoManifestacao,
            request.ReferenciaExterna);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ResultadoMensagemFiscal(mensagem.Id.Value, mensagem.DataLimiteCienciaTacita);
    }
}

/// <summary>Cancela a adesão de um contribuinte ao Domicílio Eletrônico (desativa).</summary>
/// <param name="ContribuinteId">Contribuinte titular.</param>
public sealed record CancelarDomicilioEletronicoCommand(Guid ContribuinteId) : ICommand;

/// <summary>Regras de validação do cancelamento do Domicílio Eletrônico.</summary>
public sealed class CancelarDomicilioEletronicoValidator : AbstractValidator<CancelarDomicilioEletronicoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarDomicilioEletronicoValidator() => RuleFor(c => c.ContribuinteId).NotEmpty();
}

/// <summary>Handler do cancelamento do Domicílio Eletrônico.</summary>
public sealed class CancelarDomicilioEletronicoHandler(
    IDomicilioEletronicoContribuinteRepository domicilios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarDomicilioEletronicoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarDomicilioEletronicoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var domicilio = await domicilios.ObterAtivoPorContribuinteAsync(contribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("O contribuinte não possui domicílio eletrônico ativo.");

        domicilio.CancelarAdesao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
