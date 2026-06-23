using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Application.Estabelecimentos;

/// <summary>Inativa um estabelecimento (encerramento/suspensao) — bloqueia novos atendimentos.</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
public sealed record InativarEstabelecimentoCommand(Guid EstabelecimentoId) : ICommand;

/// <summary>Reativa um estabelecimento inativado.</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
public sealed record ReativarEstabelecimentoCommand(Guid EstabelecimentoId) : ICommand;

/// <summary>Handler da inativacao de estabelecimento.</summary>
public sealed class InativarEstabelecimentoHandler(
    IEstabelecimentoCadastroRepository estabelecimentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<InativarEstabelecimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarEstabelecimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento nao encontrado.");

        estabelecimento.Inativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da reativacao de estabelecimento.</summary>
public sealed class ReativarEstabelecimentoHandler(
    IEstabelecimentoCadastroRepository estabelecimentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReativarEstabelecimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReativarEstabelecimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento nao encontrado.");

        estabelecimento.Reativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
