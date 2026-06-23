using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Educacao.Application.Fiscal;

/// <summary>E-3: abre a distribuição do FUNDEB de um exercício.</summary>
/// <param name="Exercicio">Exercício de referência.</param>
public sealed record AbrirDistribuicaoFundebCommand(int Exercicio) : ICommand<Guid>;

/// <summary>Validação da abertura da distribuição.</summary>
public sealed class AbrirDistribuicaoFundebValidator : AbstractValidator<AbrirDistribuicaoFundebCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirDistribuicaoFundebValidator() => RuleFor(c => c.Exercicio).GreaterThan(0);
}

/// <summary>Handler da abertura da distribuição do FUNDEB.</summary>
public sealed class AbrirDistribuicaoFundebHandler(
    IDistribuicaoFundebRepository distribuicoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirDistribuicaoFundebCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirDistribuicaoFundebCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var distribuicao = DistribuicaoFundeb.Criar(tenant.TenantId, request.Exercicio);
        await distribuicoes.AdicionarAsync(distribuicao, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return distribuicao.Id.Value;
    }
}

/// <summary>E-3: define o valor esperado (FNDE/Estado) de uma origem do FUNDEB.</summary>
/// <param name="DistribuicaoId">Identificador da distribuição.</param>
/// <param name="Origem">Origem do recurso (cota-parte / VAAF / VAAT / VAAR).</param>
/// <param name="ValorEsperado">Valor esperado divulgado (&gt;= 0).</param>
public sealed record DefinirEsperadoFundebCommand(
    Guid DistribuicaoId,
    OrigemRecursoFundeb Origem,
    decimal ValorEsperado) : ICommand;

/// <summary>Validação da definição de esperado.</summary>
public sealed class DefinirEsperadoFundebValidator : AbstractValidator<DefinirEsperadoFundebCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirEsperadoFundebValidator()
    {
        RuleFor(c => c.DistribuicaoId).NotEmpty();
        RuleFor(c => c.Origem).IsInEnum();
        RuleFor(c => c.ValorEsperado).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler da definição de esperado.</summary>
public sealed class DefinirEsperadoFundebHandler(
    IDistribuicaoFundebRepository distribuicoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirEsperadoFundebCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirEsperadoFundebCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var distribuicao = await distribuicoes.ObterPorIdAsync(new DistribuicaoFundebId(request.DistribuicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Distribuicao do FUNDEB inexistente no tenant.");

        distribuicao.DefinirEsperado(request.Origem, request.ValorEsperado);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>E-3: registra o recebimento de uma parcela do FUNDEB numa origem.</summary>
/// <param name="DistribuicaoId">Identificador da distribuição.</param>
/// <param name="Origem">Origem do recurso (cota-parte / VAAF / VAAT / VAAR).</param>
/// <param name="Valor">Valor recebido (&gt; 0).</param>
public sealed record ReceberParcelaFundebCommand(
    Guid DistribuicaoId,
    OrigemRecursoFundeb Origem,
    decimal Valor) : ICommand;

/// <summary>Validação do recebimento de parcela.</summary>
public sealed class ReceberParcelaFundebValidator : AbstractValidator<ReceberParcelaFundebCommand>
{
    /// <summary>Define as regras.</summary>
    public ReceberParcelaFundebValidator()
    {
        RuleFor(c => c.DistribuicaoId).NotEmpty();
        RuleFor(c => c.Origem).IsInEnum();
        RuleFor(c => c.Valor).GreaterThan(0m).WithMessage("Valor da parcela deve ser positivo.");
    }
}

/// <summary>Handler do recebimento de parcela do FUNDEB.</summary>
public sealed class ReceberParcelaFundebHandler(
    IDistribuicaoFundebRepository distribuicoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReceberParcelaFundebCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReceberParcelaFundebCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var distribuicao = await distribuicoes.ObterPorIdAsync(new DistribuicaoFundebId(request.DistribuicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Distribuicao do FUNDEB inexistente no tenant.");

        distribuicao.ReceberParcela(request.Origem, request.Valor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
