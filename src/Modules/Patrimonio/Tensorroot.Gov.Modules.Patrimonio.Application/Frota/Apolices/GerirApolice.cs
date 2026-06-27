using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota.Apolices;

/// <summary>Contrata (registra) uma apólice de seguro para um veículo.</summary>
/// <param name="VeiculoId">Veículo segurado.</param>
/// <param name="Categoria">Categoria do seguro.</param>
/// <param name="Seguradora">Seguradora.</param>
/// <param name="NumeroApolice">Número da apólice.</param>
/// <param name="InicioVigencia">Início da vigência.</param>
/// <param name="FimVigencia">Fim da vigência.</param>
/// <param name="Premio">Prêmio pago.</param>
/// <param name="ImportanciaSegurada">Importância segurada.</param>
/// <param name="Cobertura">Resumo da cobertura (opcional).</param>
public sealed record ContratarApoliceCommand(
    Guid VeiculoId,
    CategoriaSeguro Categoria,
    string Seguradora,
    string NumeroApolice,
    DateOnly InicioVigencia,
    DateOnly FimVigencia,
    decimal Premio,
    decimal ImportanciaSegurada,
    string? Cobertura) : ICommand<Guid>;

/// <summary>Regras de validação da contratação de apólice.</summary>
public sealed class ContratarApoliceValidator : AbstractValidator<ContratarApoliceCommand>
{
    /// <summary>Define as regras.</summary>
    public ContratarApoliceValidator()
    {
        RuleFor(comando => comando.VeiculoId).NotEmpty();
        RuleFor(comando => comando.Categoria).IsInEnum();
        RuleFor(comando => comando.Seguradora).NotEmpty().MaximumLength(150);
        RuleFor(comando => comando.NumeroApolice).NotEmpty().MaximumLength(50);
        RuleFor(comando => comando.FimVigencia)
            .GreaterThan(comando => comando.InicioVigencia)
            .WithMessage("Fim da vigência deve ser posterior ao início.");
        RuleFor(comando => comando.Premio).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.ImportanciaSegurada).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.Cobertura).MaximumLength(500);
    }
}

/// <summary>Handler da contratação de apólice.</summary>
public sealed class ContratarApoliceHandler(
    IApoliceRepository apolices,
    IVeiculoRepository veiculos,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext)
    : ICommandHandler<ContratarApoliceCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ContratarApoliceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculoId = new VeiculoId(request.VeiculoId);
        // Garante que o veículo existe no tenant antes de vincular a apólice (integridade da referência).
        _ = await veiculos.ObterPorIdAsync(veiculoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        var apolice = Apolice.Contratar(
            tenantContext.TenantId,
            veiculoId,
            request.Categoria,
            request.Seguradora,
            request.NumeroApolice,
            request.InicioVigencia,
            request.FimVigencia,
            ValorMonetario.De(request.Premio),
            ValorMonetario.De(request.ImportanciaSegurada),
            request.Cobertura);

        apolices.Adicionar(apolice);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return apolice.Id.Value;
    }
}

/// <summary>Renova uma apólice, estendendo a vigência para um novo período com novo prêmio.</summary>
/// <param name="ApoliceId">Apólice a renovar.</param>
/// <param name="NovoInicioVigencia">Início do novo período.</param>
/// <param name="NovoFimVigencia">Fim do novo período.</param>
/// <param name="NovoPremio">Prêmio do novo período.</param>
public sealed record RenovarApoliceCommand(
    Guid ApoliceId,
    DateOnly NovoInicioVigencia,
    DateOnly NovoFimVigencia,
    decimal NovoPremio) : ICommand;

/// <summary>Regras de validação da renovação de apólice.</summary>
public sealed class RenovarApoliceValidator : AbstractValidator<RenovarApoliceCommand>
{
    /// <summary>Define as regras.</summary>
    public RenovarApoliceValidator()
    {
        RuleFor(comando => comando.ApoliceId).NotEmpty();
        RuleFor(comando => comando.NovoFimVigencia)
            .GreaterThan(comando => comando.NovoInicioVigencia)
            .WithMessage("Fim da nova vigência deve ser posterior ao novo início.");
        RuleFor(comando => comando.NovoPremio).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler da renovação de apólice.</summary>
public sealed class RenovarApoliceHandler(IApoliceRepository apolices, IUnitOfWork unitOfWork)
    : ICommandHandler<RenovarApoliceCommand>
{
    /// <inheritdoc />
    public async Task Handle(RenovarApoliceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apolice = await apolices.ObterPorIdAsync(new ApoliceId(request.ApoliceId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Apólice não encontrada.");

        apolice.Renovar(request.NovoInicioVigencia, request.NovoFimVigencia, ValorMonetario.De(request.NovoPremio));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Cancela uma apólice (endosso de cancelamento); estado terminal.</summary>
/// <param name="ApoliceId">Apólice a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record CancelarApoliceCommand(Guid ApoliceId, string Motivo) : ICommand;

/// <summary>Regras de validação do cancelamento de apólice.</summary>
public sealed class CancelarApoliceValidator : AbstractValidator<CancelarApoliceCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarApoliceValidator()
    {
        RuleFor(comando => comando.ApoliceId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(200);
    }
}

/// <summary>Handler do cancelamento de apólice.</summary>
public sealed class CancelarApoliceHandler(IApoliceRepository apolices, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarApoliceCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarApoliceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apolice = await apolices.ObterPorIdAsync(new ApoliceId(request.ApoliceId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Apólice não encontrada.");

        apolice.Cancelar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
