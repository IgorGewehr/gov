using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

namespace Tensorroot.Gov.Modules.Educacao.Application.Transporte;

/// <summary>Ativa a rota para operacao (Planejada -&gt; Ativa). Exige ao menos um aluno ativo (I-R5).</summary>
/// <param name="RotaId">Rota a ativar.</param>
public sealed record AtivarRotaCommand(Guid RotaId) : ICommand;

/// <summary>Handler da ativacao de rota.</summary>
public sealed class AtivarRotaHandler(IRotaTransporteRepository rotas, IUnitOfWork unitOfWork)
    : ICommandHandler<AtivarRotaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtivarRotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rota = await rotas.ObterPorIdAsync(new RotaTransporteId(request.RotaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rota nao encontrada.");
        rota.Ativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Encerra a rota (estado terminal).</summary>
/// <param name="RotaId">Rota a encerrar.</param>
public sealed record EncerrarRotaCommand(Guid RotaId) : ICommand;

/// <summary>Handler do encerramento de rota.</summary>
public sealed class EncerrarRotaHandler(IRotaTransporteRepository rotas, IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarRotaCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarRotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rota = await rotas.ObterPorIdAsync(new RotaTransporteId(request.RotaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rota nao encontrada.");
        rota.Encerrar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Desliga (logicamente) um aluno da rota.</summary>
/// <param name="RotaId">Rota.</param>
/// <param name="AlunoTransportadoId">Vinculo a desligar.</param>
public sealed record DesligarAlunoRotaCommand(Guid RotaId, Guid AlunoTransportadoId) : ICommand;

/// <summary>Handler do desligamento de aluno da rota.</summary>
public sealed class DesligarAlunoRotaHandler(IRotaTransporteRepository rotas, IUnitOfWork unitOfWork)
    : ICommandHandler<DesligarAlunoRotaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DesligarAlunoRotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rota = await rotas.ObterPorIdAsync(new RotaTransporteId(request.RotaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Rota nao encontrada.");
        rota.DesligarAluno(new AlunoTransportadoId(request.AlunoTransportadoId));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
