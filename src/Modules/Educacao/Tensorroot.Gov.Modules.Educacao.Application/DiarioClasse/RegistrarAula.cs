using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Registra uma aula/dia (conta para os 200 dias letivos quando dia letivo — I-9) em diario aberto (I-3).</summary>
/// <param name="DiarioClasseId">Diario a registrar.</param>
/// <param name="Data">Data da aula.</param>
/// <param name="Conteudo">Conteudo ministrado.</param>
/// <param name="DiaLetivo">Indica se conta como dia letivo.</param>
public sealed record RegistrarAulaCommand(
    Guid DiarioClasseId,
    DateOnly Data,
    string Conteudo,
    bool DiaLetivo) : ICommand;

/// <summary>Regras de validacao do registro de aula (validacao minima; demais protecoes por invariantes de dominio).</summary>
public sealed class RegistrarAulaValidator : AbstractValidator<RegistrarAulaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAulaValidator()
    {
        RuleFor(comando => comando.DiarioClasseId).NotEmpty().WithMessage("Identificador do diario obrigatorio.");
        RuleFor(comando => comando.Data).NotEmpty().WithMessage("Data da aula obrigatoria.");
    }
}

/// <summary>Handler do registro de aula.</summary>
public sealed class RegistrarAulaHandler(IDiarioClasseRepository diarios, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarAulaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAulaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diario = await diarios.ObterPorIdAsync(new DiarioClasseId(request.DiarioClasseId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Diario nao encontrado.");

        diario.RegistrarAula(request.Data, request.Conteudo, request.DiaLetivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
