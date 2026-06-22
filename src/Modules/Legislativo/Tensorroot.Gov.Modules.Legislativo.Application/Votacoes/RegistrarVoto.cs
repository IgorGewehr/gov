using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Registra um voto na trilha imutavel (idempotente por <paramref name="VotoId"/> — I-3).</summary>
/// <param name="VotacaoId">Votacao alvo.</param>
/// <param name="VotoId">Identificador do voto (origem do painel, chave de idempotencia).</param>
/// <param name="VereadorId">Vereador autor do voto.</param>
/// <param name="Sentido">Sentido do voto (1 = Sim, 2 = Nao, 3 = Abstencao).</param>
public sealed record RegistrarVotoCommand(
    Guid VotacaoId,
    Guid VotoId,
    Guid VereadorId,
    int Sentido) : ICommand;

/// <summary>Regras de validacao do registro de voto.</summary>
public sealed class RegistrarVotoValidator : AbstractValidator<RegistrarVotoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarVotoValidator()
    {
        RuleFor(comando => comando.VotacaoId).NotEmpty();
        RuleFor(comando => comando.VotoId).NotEmpty();
        RuleFor(comando => comando.VereadorId).NotEmpty();

        // Sentido trafega como int no contrato HTTP; validar contra os valores definidos do enum.
        // IsInEnum() so funciona quando a propriedade JA e do tipo enum — aqui e Int32, e a regra
        // reprova qualquer valor. Usar Enum.IsDefined sobre o tipo de dominio correto.
        RuleFor(comando => comando.Sentido)
            .Must(sentido => Enum.IsDefined(typeof(SentidoVoto), sentido))
            .WithMessage("Sentido de voto invalido (1 = Sim, 2 = Nao, 3 = Abstencao).");
    }
}

/// <summary>Handler do registro de voto.</summary>
public sealed class RegistrarVotoHandler(
    IVotacaoRepository votacoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarVotoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarVotoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        votacao.RegistrarVoto(
            new VotoId(request.VotoId),
            new VereadorId(request.VereadorId),
            (SentidoVoto)request.Sentido,
            timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
