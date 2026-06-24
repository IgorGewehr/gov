using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Arquivistica;

/// <summary>
/// Avalia a aptidao das fichas de destinacao em <see cref="EstadoDestinacao.AguardandoPrazo"/>,
/// promovendo para <see cref="EstadoDestinacao.AptoEliminar"/> as cujo prazo de guarda ja decorreu
/// (varredura — reusa o padrao de drenagem do Outbox; nao e scheduler ad-hoc). Peca 2 / W9.4.
/// </summary>
public sealed record AvaliarAptidaoDestinacoesCommand : ICommand<int>;

/// <summary>Handler da varredura de aptidao a eliminacao.</summary>
public sealed class AvaliarAptidaoDestinacoesHandler(
    IDestinacaoProcessoRepository destinacoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AvaliarAptidaoDestinacoesCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(AvaliarAptidaoDestinacoesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var pendentes = await destinacoes.ListarAguardandoPrazoAsync(cancellationToken).ConfigureAwait(false);

        var promovidas = 0;
        foreach (var ficha in pendentes)
        {
            if (ficha.AvaliarAptidao(hoje))
            {
                promovidas++;
            }
        }

        if (promovidas > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return promovidas;
    }
}

/// <summary>
/// Autoriza a eliminacao de um processo arquivado por ato humano (RBAC). I-T1: rejeita antes do prazo;
/// I-T2: guarda permanente sempre rejeita. Peca 2 / W9.4.
/// </summary>
/// <param name="DestinacaoProcessoId">Ficha de destinacao a autorizar.</param>
/// <param name="AutorizadoPor">Sujeito autorizador (RBAC).</param>
public sealed record AutorizarEliminacaoCommand(Guid DestinacaoProcessoId, Guid AutorizadoPor) : ICommand;

/// <summary>Regras de validacao da autorizacao de eliminacao.</summary>
public sealed class AutorizarEliminacaoValidator : AbstractValidator<AutorizarEliminacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AutorizarEliminacaoValidator()
    {
        RuleFor(comando => comando.DestinacaoProcessoId).NotEmpty();
        RuleFor(comando => comando.AutorizadoPor).NotEmpty();
    }
}

/// <summary>Handler da autorizacao de eliminacao.</summary>
public sealed class AutorizarEliminacaoHandler(
    IDestinacaoProcessoRepository destinacoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AutorizarEliminacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AutorizarEliminacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ficha = await destinacoes.ObterPorIdAsync(new DestinacaoProcessoId(request.DestinacaoProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ficha de destinacao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        ficha.AutorizarEliminacao(request.AutorizadoPor, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Registra a eliminacao efetiva com o termo assinado/carimbado (hash) + edital sob WORM (I-T4 — prova
/// oponivel ao TCE; Res. CONARQ 40/2014). Exige autorizacao previa. Peca 2 / W9.4.
/// </summary>
/// <param name="DestinacaoProcessoId">Ficha de destinacao autorizada.</param>
/// <param name="TermoEliminacaoHash">Hash SHA-256 do termo de eliminacao (assinado/carimbado).</param>
/// <param name="EditalEliminacaoRef">Referencia do edital de eliminacao.</param>
public sealed record RegistrarEliminacaoCommand(
    Guid DestinacaoProcessoId,
    string TermoEliminacaoHash,
    string EditalEliminacaoRef) : ICommand;

/// <summary>Regras de validacao do registro de eliminacao.</summary>
public sealed class RegistrarEliminacaoValidator : AbstractValidator<RegistrarEliminacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarEliminacaoValidator()
    {
        RuleFor(comando => comando.DestinacaoProcessoId).NotEmpty();
        RuleFor(comando => comando.TermoEliminacaoHash).NotEmpty().Length(Hash.ComprimentoSha256);
        RuleFor(comando => comando.EditalEliminacaoRef).NotEmpty().MaximumLength(200);
    }
}

/// <summary>Handler do registro de eliminacao.</summary>
public sealed class RegistrarEliminacaoHandler(
    IDestinacaoProcessoRepository destinacoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarEliminacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarEliminacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ficha = await destinacoes.ObterPorIdAsync(new DestinacaoProcessoId(request.DestinacaoProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ficha de destinacao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        ficha.MarcarEliminado(Hash.De(request.TermoEliminacaoHash), request.EditalEliminacaoRef, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
