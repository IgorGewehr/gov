using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;

/// <summary>A-1: cria a unidade gestora do Fundo Municipal de Assistencia Social (FMAS).</summary>
/// <param name="Nome">Nome da unidade gestora.</param>
/// <param name="Cnpj">CNPJ da unidade gestora.</param>
public sealed record AbrirFundoMunicipalAssistenciaCommand(string Nome, string Cnpj) : ICommand<Guid>;

/// <summary>Validacao da abertura do FMAS.</summary>
public sealed class AbrirFundoMunicipalAssistenciaValidator : AbstractValidator<AbrirFundoMunicipalAssistenciaCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirFundoMunicipalAssistenciaValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().WithMessage("Nome da unidade gestora e obrigatorio.");
        RuleFor(c => c.Cnpj).NotEmpty().WithMessage("CNPJ da unidade gestora e obrigatorio.");
    }
}

/// <summary>Handler da abertura do FMAS.</summary>
public sealed class AbrirFundoMunicipalAssistenciaHandler(
    IFundoMunicipalAssistenciaRepository fundos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirFundoMunicipalAssistenciaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirFundoMunicipalAssistenciaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = FundoMunicipalAssistencia.Criar(tenant.TenantId, request.Nome, request.Cnpj);
        await fundos.AdicionarAsync(fundo, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return fundo.Id.Value;
    }
}

/// <summary>A-1: registra o recebimento de uma parcela do FNAS num bloco/piso/fonte do FMAS.</summary>
/// <param name="FundoId">Identificador do fundo.</param>
/// <param name="Bloco">Bloco de cofinanciamento (PSB/PSE-MC/PSE-AC/Gestao-IGD).</param>
/// <param name="Piso">Piso de cofinanciamento (servico tipificado).</param>
/// <param name="FonteRecurso">Fonte/destinacao de recurso (PCASP) vinculada.</param>
/// <param name="Valor">Valor recebido (&gt; 0).</param>
public sealed record ReceberParcelaFnasCommand(
    Guid FundoId,
    BlocoFinanciamentoAssistencia Bloco,
    PisoAssistencia Piso,
    string FonteRecurso,
    decimal Valor) : ICommand;

/// <summary>Validacao do recebimento de parcela.</summary>
public sealed class ReceberParcelaFnasValidator : AbstractValidator<ReceberParcelaFnasCommand>
{
    /// <summary>Define as regras.</summary>
    public ReceberParcelaFnasValidator()
    {
        RuleFor(c => c.FundoId).NotEmpty();
        RuleFor(c => c.Bloco).IsInEnum();
        RuleFor(c => c.Piso).IsInEnum();
        RuleFor(c => c.FonteRecurso).NotEmpty().WithMessage("Fonte de recurso e obrigatoria (segregacao por bloco/piso).");
        RuleFor(c => c.Valor).GreaterThan(0m).WithMessage("Valor da parcela deve ser positivo.");
    }
}

/// <summary>Handler do recebimento de parcela.</summary>
public sealed class ReceberParcelaFnasHandler(
    IFundoMunicipalAssistenciaRepository fundos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReceberParcelaFnasCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReceberParcelaFnasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = await fundos.ObterPorIdAsync(new FundoMunicipalAssistenciaId(request.FundoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fundo Municipal de Assistencia Social inexistente no tenant.");

        fundo.ReceberParcela(request.Bloco, request.Piso, request.FonteRecurso, request.Valor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>A-1: registra a execucao de despesa num bloco/piso/fonte do FMAS (respeita o saldo segregado).</summary>
/// <param name="FundoId">Identificador do fundo.</param>
/// <param name="Bloco">Bloco de cofinanciamento.</param>
/// <param name="Piso">Piso de cofinanciamento.</param>
/// <param name="FonteRecurso">Fonte/destinacao de recurso (PCASP) vinculada.</param>
/// <param name="Valor">Valor executado (&gt; 0).</param>
public sealed record ExecutarDespesaSuasCommand(
    Guid FundoId,
    BlocoFinanciamentoAssistencia Bloco,
    PisoAssistencia Piso,
    string FonteRecurso,
    decimal Valor) : ICommand;

/// <summary>Validacao da execucao de despesa.</summary>
public sealed class ExecutarDespesaSuasValidator : AbstractValidator<ExecutarDespesaSuasCommand>
{
    /// <summary>Define as regras.</summary>
    public ExecutarDespesaSuasValidator()
    {
        RuleFor(c => c.FundoId).NotEmpty();
        RuleFor(c => c.Bloco).IsInEnum();
        RuleFor(c => c.Piso).IsInEnum();
        RuleFor(c => c.FonteRecurso).NotEmpty();
        RuleFor(c => c.Valor).GreaterThan(0m).WithMessage("Valor executado deve ser positivo.");
    }
}

/// <summary>Handler da execucao de despesa.</summary>
public sealed class ExecutarDespesaSuasHandler(
    IFundoMunicipalAssistenciaRepository fundos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ExecutarDespesaSuasCommand>
{
    /// <inheritdoc />
    public async Task Handle(ExecutarDespesaSuasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = await fundos.ObterPorIdAsync(new FundoMunicipalAssistenciaId(request.FundoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fundo Municipal de Assistencia Social inexistente no tenant.");

        // O invariante de transposicao vedada entre blocos/pisos vive no agregado (lanca se exceder o saldo).
        fundo.ExecutarDespesa(request.Bloco, request.Piso, request.FonteRecurso, request.Valor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
