using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>
/// Abre uma grade de disponibilidade (em rascunho) para um profissional num estabelecimento. As
/// pre-condicoes de profissional/estabelecimento ATIVOS sao verificadas aqui (reuso da Onda 1).
/// </summary>
/// <param name="ProfissionalId">Profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="Tipo">Natureza (consulta/exame).</param>
/// <param name="Data">Data da grade.</param>
/// <param name="HoraInicio">Inicio do expediente.</param>
/// <param name="HoraFim">Fim do expediente.</param>
/// <param name="DuracaoSlotMinutos">Duracao de cada slot, em minutos.</param>
/// <param name="CapacidadeVagas">Vagas por slot.</param>
public sealed record AbrirAgendaCommand(
    Guid ProfissionalId,
    Guid EstabelecimentoId,
    TipoAtendimentoAgenda Tipo,
    DateOnly Data,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    int DuracaoSlotMinutos,
    int CapacidadeVagas) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de agenda.</summary>
public sealed class AbrirAgendaValidator : AbstractValidator<AbrirAgendaCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirAgendaValidator()
    {
        RuleFor(comando => comando.ProfissionalId).NotEmpty().WithMessage("Profissional e obrigatorio.");
        RuleFor(comando => comando.EstabelecimentoId).NotEmpty().WithMessage("Estabelecimento e obrigatorio.");
        RuleFor(comando => comando.Tipo).IsInEnum().WithMessage("Tipo de atendimento invalido.");
        RuleFor(comando => comando.HoraFim).GreaterThan(comando => comando.HoraInicio).WithMessage("Hora fim deve ser posterior a hora inicio.");
        RuleFor(comando => comando.DuracaoSlotMinutos).GreaterThanOrEqualTo(AgendaProfissional.DuracaoSlotMinima);
        RuleFor(comando => comando.CapacidadeVagas).GreaterThanOrEqualTo(AgendaProfissional.CapacidadeMinima);
    }
}

/// <summary>Handler da abertura de agenda.</summary>
public sealed class AbrirAgendaHandler(
    IAgendaProfissionalRepository agendas,
    IProfissionalCadastroRepository profissionais,
    IEstabelecimentoCadastroRepository estabelecimentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirAgendaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirAgendaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profissionalId = new ProfissionalId(request.ProfissionalId);
        var estabelecimentoId = new EstabelecimentoId(request.EstabelecimentoId);

        var profissional = await profissionais.ObterPorIdAsync(profissionalId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Profissional nao encontrado.");
        if (profissional.Situacao is not Domain.Profissionais.SituacaoProfissional.Ativo)
        {
            throw new InvalidOperationException("Profissional inativo: nao admite agenda.");
        }

        var estabelecimento = await estabelecimentos.ObterPorIdAsync(estabelecimentoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento nao encontrado.");
        if (!estabelecimento.EstaAtivo)
        {
            throw new InvalidOperationException("Estabelecimento inativo: nao admite agenda.");
        }

        var agenda = AgendaProfissional.Abrir(
            tenant.TenantId,
            profissionalId,
            estabelecimentoId,
            request.Tipo,
            request.Data,
            request.HoraInicio,
            request.HoraFim,
            request.DuracaoSlotMinutos,
            request.CapacidadeVagas);

        agendas.Adicionar(agenda);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return agenda.Id.Value;
    }
}
