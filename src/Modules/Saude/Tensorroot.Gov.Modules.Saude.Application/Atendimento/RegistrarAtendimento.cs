using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Abre o registro clinico de um encontro assistencial (PEP/e-SUS APS).</summary>
/// <param name="PacienteId">Paciente atendido.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES) do atendimento.</param>
/// <param name="ProfissionalId">Profissional responsavel (CBO ativo na competencia).</param>
/// <param name="DataHora">Data/hora do atendimento.</param>
/// <param name="Modalidade">Modalidade (1 = Presencial, 2 = Teleconsulta).</param>
public sealed record RegistrarAtendimentoCommand(
    Guid PacienteId,
    Guid EstabelecimentoId,
    Guid ProfissionalId,
    DateTimeOffset DataHora,
    ModalidadeAtendimento Modalidade) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de atendimento.</summary>
public sealed class RegistrarAtendimentoValidator : AbstractValidator<RegistrarAtendimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAtendimentoValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.EstabelecimentoId).NotEmpty().WithMessage("Estabelecimento (CNES) e obrigatorio.");
        RuleFor(comando => comando.ProfissionalId).NotEmpty().WithMessage("Profissional e obrigatorio.");
        RuleFor(comando => comando.DataHora).NotEmpty().WithMessage("Data/hora do atendimento e obrigatoria.");
        RuleFor(comando => comando.Modalidade).IsInEnum().WithMessage("Modalidade invalida.");
    }
}

/// <summary>Handler do registro de atendimento.</summary>
public sealed class RegistrarAtendimentoHandler(
    IPacienteRepository pacientes,
    IEstabelecimentoRepository estabelecimentos,
    IAtendimentoRepository atendimentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<RegistrarAtendimentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarAtendimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // I-1: paciente deve existir com CNS valido/confirmado (CADSUS).
        var paciente = await pacientes
            .ObterPorIdAsync(new DomainPacienteId(request.PacienteId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente sem CNS valido/confirmado.");

        if (!paciente.CnsConfirmado)
        {
            throw new InvalidOperationException("Paciente sem CNS valido/confirmado.");
        }

        var competencia = Competencia.De(request.DataHora);
        var estabelecimentoId = new EstabelecimentoId(request.EstabelecimentoId);
        var profissionalId = new ProfissionalId(request.ProfissionalId);

        // I-1: CNES e profissional/CBO ativos na competencia.
        var estabelecimentoAtivo = await estabelecimentos
            .EstabelecimentoAtivoAsync(estabelecimentoId, competencia, cancellationToken)
            .ConfigureAwait(false);
        if (!estabelecimentoAtivo)
        {
            throw new InvalidOperationException("Estabelecimento (CNES) inativo na competencia.");
        }

        var profissionalAtivo = await estabelecimentos
            .ProfissionalAtivoAsync(profissionalId, estabelecimentoId, competencia, cancellationToken)
            .ConfigureAwait(false);
        if (!profissionalAtivo)
        {
            throw new InvalidOperationException("Profissional/CBO inativo na competencia.");
        }

        // I-9: teleconsulta exige profissional com CRM ativo (Lei 14.510/2022).
        if (request.Modalidade == ModalidadeAtendimento.Teleconsulta)
        {
            var crmAtivo = await estabelecimentos
                .ProfissionalComCrmAtivoAsync(profissionalId, cancellationToken)
                .ConfigureAwait(false);
            if (!crmAtivo)
            {
                throw new InvalidOperationException("Teleconsulta exige profissional com CRM ativo.");
            }
        }

        var atendimento = Domain.Atendimento.Atendimento.Registrar(
            tenant.TenantId,
            new PacienteId(request.PacienteId),
            estabelecimentoId,
            profissionalId,
            request.DataHora,
            competencia,
            request.Modalidade);

        atendimentos.Adicionar(atendimento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return atendimento.Id.Value;
    }
}
