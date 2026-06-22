using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Agenda uma nova sessao plenaria (situacao inicial Agendada).</summary>
/// <param name="Tipo">Especie da sessao (1 = Ordinaria, 2 = Extraordinaria).</param>
/// <param name="DataHora">Momento agendado da sessao.</param>
/// <param name="TotalMembros">Numero de vereadores da Camara (maior que zero).</param>
public sealed record AgendarSessaoCommand(
    int Tipo,
    DateTimeOffset DataHora,
    int TotalMembros) : ICommand<Guid>;

/// <summary>Regras de validacao do agendamento de sessao.</summary>
public sealed class AgendarSessaoValidator : AbstractValidator<AgendarSessaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AgendarSessaoValidator()
    {
        // Tipo trafega como int; validar contra o enum de dominio (IsInEnum() so vale para
        // propriedade ja tipada como enum — aqui e Int32 e reprovaria qualquer valor).
        RuleFor(comando => comando.Tipo)
            .Must(tipo => Enum.IsDefined(typeof(TipoSessao), tipo))
            .WithMessage("Tipo de sessao invalido (1 = Ordinaria, 2 = Extraordinaria).");
        RuleFor(comando => comando.DataHora).NotEmpty();
        RuleFor(comando => comando.TotalMembros).GreaterThan(0);
    }
}

/// <summary>Handler do agendamento de sessao.</summary>
public sealed class AgendarSessaoHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AgendarSessaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AgendarSessaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = Sessao.Agendar(
            tenant.TenantId,
            (TipoSessao)request.Tipo,
            DataHora.De(request.DataHora),
            request.TotalMembros);

        sessoes.Adicionar(sessao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return sessao.Id.Value;
    }
}
