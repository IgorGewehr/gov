using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>Registra um atendimento (PAIF/PAEFI/SCFV) num prontuario aberto (I-3, I-4, I-5).</summary>
/// <param name="ProntuarioId">Prontuario a registrar o atendimento.</param>
/// <param name="Servico">Servico socioassistencial (1=PAIF, 2=PAEFI, 3=SCFV).</param>
/// <param name="DataAtendimento">Data do atendimento.</param>
/// <param name="Descricao">Descricao sigilosa do atendimento.</param>
/// <param name="ProfissionalId">Profissional responsavel pelo registro.</param>
public sealed record RegistrarAtendimentoCommand(
    Guid ProntuarioId,
    TipoServico Servico,
    DateOnly DataAtendimento,
    string Descricao,
    Guid ProfissionalId) : ICommand;

/// <summary>Regras de validacao do registro de atendimento.</summary>
public sealed class RegistrarAtendimentoValidator : AbstractValidator<RegistrarAtendimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAtendimentoValidator()
    {
        RuleFor(comando => comando.ProntuarioId).NotEmpty().WithMessage("Identificador do prontuario e obrigatorio.");
        RuleFor(comando => comando.Servico).IsInEnum().WithMessage("Servico (PAIF/PAEFI/SCFV) invalido.");
        RuleFor(comando => comando.DataAtendimento).NotEmpty().WithMessage("Data do atendimento e obrigatoria.");
        RuleFor(comando => comando.Descricao).NotEmpty().MaximumLength(4000).WithMessage("Descricao do atendimento e obrigatoria.");
        RuleFor(comando => comando.ProfissionalId).NotEmpty().WithMessage("Profissional responsavel e obrigatorio.");
    }
}

/// <summary>
/// Handler do registro de atendimento. Valida a compatibilidade servico↔unidade (I-4), muta o
/// agregado e publica <see cref="AtendimentoRegistradoIntegrationEvent"/> (so metadados — I-9).
/// </summary>
public sealed class RegistrarAtendimentoHandler(
    IProntuarioSuasRepository prontuarios,
    IUnidadeAtendimentoTipoLookup unidades,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarAtendimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAtendimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prontuario = await prontuarios.ObterPorIdAsync(new ProntuarioSuasId(request.ProntuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Prontuario nao encontrado.");

        var tipoUnidade = await unidades.ObterTipoAsync(prontuario.UnidadeAtendimentoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade de atendimento nao encontrada.");

        prontuario.RegistrarAtendimento(request.Servico, request.DataAtendimento, request.Descricao, request.ProfissionalId, tipoUnidade);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AtendimentoRegistradoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            prontuario.Id.Value,
            prontuario.UnidadeAtendimentoId,
            request.Servico.ToString(),
            request.DataAtendimento);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
