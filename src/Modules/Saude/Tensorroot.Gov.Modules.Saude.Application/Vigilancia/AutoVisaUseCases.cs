using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Application.Vigilancia;

/// <summary>
/// Lavra um auto (intimacao/infracao/penalidade) a partir de uma inspecao CONCLUIDA do estabelecimento.
/// Exige inspecao concluida cujo estabelecimento bate com o informado e numero de auto unico. Auto de
/// intimacao nao tem multa; infracao/penalidade exige valor (>= 0).
/// </summary>
/// <param name="EstabelecimentoId">Estabelecimento autuado.</param>
/// <param name="InspecaoId">Inspecao fundante (concluida).</param>
/// <param name="Tipo">Tipo do auto.</param>
/// <param name="Numero">Numero/controle do auto.</param>
/// <param name="Fundamentacao">Fundamentacao legal/pendencias.</param>
/// <param name="PrazoFinal">Prazo final (defesa/regularizacao).</param>
/// <param name="ValorMulta">Valor da multa (nulo para intimacao).</param>
public sealed record LavrarAutoCommand(
    Guid EstabelecimentoId,
    Guid InspecaoId,
    TipoAutoVisa Tipo,
    string Numero,
    string Fundamentacao,
    DateOnly PrazoFinal,
    decimal? ValorMulta) : ICommand<Guid>;

/// <summary>Regras de validacao da lavratura de auto.</summary>
public sealed class LavrarAutoValidator : AbstractValidator<LavrarAutoCommand>
{
    /// <summary>Define as regras.</summary>
    public LavrarAutoValidator()
    {
        RuleFor(c => c.EstabelecimentoId).NotEmpty();
        RuleFor(c => c.InspecaoId).NotEmpty();
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.Numero).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Fundamentacao).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.ValorMulta).GreaterThanOrEqualTo(0m).When(c => c.ValorMulta is not null);
    }
}

/// <summary>Handler da lavratura de auto.</summary>
public sealed class LavrarAutoHandler(
    IInspecaoRepository inspecoes,
    IAutoVisaRepository autos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<LavrarAutoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(LavrarAutoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(request.InspecaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inspecao fundante nao encontrada.");

        if (inspecao.Situacao != SituacaoInspecao.Concluida)
        {
            throw new InvalidOperationException("So e possivel lavrar auto a partir de inspecao concluida.");
        }

        if (inspecao.EstabelecimentoFiscalizavelId.Value != request.EstabelecimentoId)
        {
            throw new InvalidOperationException("Inspecao nao pertence ao estabelecimento informado.");
        }

        if (await autos.ExisteNumeroAsync(request.Numero.Trim(), cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe auto com este numero.");
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var auto = AutoVisa.Lavrar(
            tenant.TenantId,
            inspecao.EstabelecimentoFiscalizavelId,
            inspecao.Id,
            request.Tipo,
            request.Numero,
            request.Fundamentacao,
            hoje,
            request.PrazoFinal,
            request.ValorMulta);

        autos.Adicionar(auto);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return auto.Id.Value;
    }
}

/// <summary>Registra a defesa do autuado num auto de infracao/penalidade (dentro do prazo).</summary>
/// <param name="AutoId">Auto alvo.</param>
/// <param name="Texto">Razoes da defesa.</param>
public sealed record ApresentarDefesaAutoCommand(Guid AutoId, string Texto) : ICommand;

/// <summary>Regras de validacao da defesa.</summary>
public sealed class ApresentarDefesaAutoValidator : AbstractValidator<ApresentarDefesaAutoCommand>
{
    /// <summary>Define as regras.</summary>
    public ApresentarDefesaAutoValidator()
    {
        RuleFor(c => c.AutoId).NotEmpty();
        RuleFor(c => c.Texto).NotEmpty().MaximumLength(4000);
    }
}

/// <summary>Handler da apresentacao de defesa.</summary>
public sealed class ApresentarDefesaAutoHandler(IAutoVisaRepository autos, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<ApresentarDefesaAutoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ApresentarDefesaAutoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auto = await autos.ObterPorIdAsync(new AutoVisaId(request.AutoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Auto nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        auto.ApresentarDefesa(request.Texto, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Julga um auto de infracao/penalidade com defesa apresentada (defere ou indefere).</summary>
/// <param name="AutoId">Auto alvo.</param>
/// <param name="Deferir">Verdadeiro defere (cancela); falso indefere (mantem).</param>
public sealed record JulgarAutoCommand(Guid AutoId, bool Deferir) : ICommand;

/// <summary>Handler do julgamento de auto.</summary>
public sealed class JulgarAutoHandler(IAutoVisaRepository autos, IUnitOfWork unitOfWork)
    : ICommandHandler<JulgarAutoCommand>
{
    /// <inheritdoc />
    public async Task Handle(JulgarAutoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auto = await autos.ObterPorIdAsync(new AutoVisaId(request.AutoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Auto nao encontrado.");

        auto.Julgar(request.Deferir);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Reconhece a regularizacao das pendencias de uma intimacao (dentro do prazo).</summary>
/// <param name="AutoId">Auto de intimacao alvo.</param>
public sealed record RegularizarIntimacaoCommand(Guid AutoId) : ICommand;

/// <summary>Handler da regularizacao de intimacao.</summary>
public sealed class RegularizarIntimacaoHandler(IAutoVisaRepository autos, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<RegularizarIntimacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegularizarIntimacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auto = await autos.ObterPorIdAsync(new AutoVisaId(request.AutoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Auto nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        auto.Regularizar(hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
