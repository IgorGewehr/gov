using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application.RegistroPrecos;

/// <summary>Registra um item (preco + quantidade + fornecedor) em uma ata vigente.</summary>
/// <param name="AtaId">Identificador da ata.</param>
/// <param name="ItemCatalogoId">Item de catalogo registrado.</param>
/// <param name="FornecedorBeneficiarioId">Fornecedor beneficiario do preco.</param>
/// <param name="PrecoRegistrado">Preco unitario registrado.</param>
/// <param name="QuantidadeRegistrada">Quantidade maxima registrada.</param>
public sealed record RegistrarItemAtaCommand(
    Guid AtaId,
    Guid ItemCatalogoId,
    Guid FornecedorBeneficiarioId,
    decimal PrecoRegistrado,
    decimal QuantidadeRegistrada) : ICommand<Guid>;

/// <summary>Registra uma adesao (carona) a um item da ata, debitando o saldo (art. 86).</summary>
/// <param name="AtaId">Identificador da ata.</param>
/// <param name="ItemAtaId">Item registrado objeto da adesao.</param>
/// <param name="OrgaoAderente">Orgao/entidade aderente.</param>
/// <param name="Quantidade">Quantidade aderida.</param>
public sealed record RegistrarAdesaoCommand(
    Guid AtaId,
    Guid ItemAtaId,
    string OrgaoAderente,
    decimal Quantidade) : ICommand<Guid>;

/// <summary>Consome saldo de um item por contratacao direta do proprio ente (uso da ata).</summary>
/// <param name="AtaId">Identificador da ata.</param>
/// <param name="ItemAtaId">Item registrado.</param>
/// <param name="Quantidade">Quantidade contratada.</param>
public sealed record ContratarItemAtaCommand(Guid AtaId, Guid ItemAtaId, decimal Quantidade) : ICommand;

/// <summary>Cancela uma ata por ato administrativo (art. 85/86).</summary>
/// <param name="AtaId">Identificador da ata.</param>
/// <param name="Motivo">Motivacao do ato.</param>
public sealed record CancelarAtaCommand(Guid AtaId, string Motivo) : ICommand;

/// <summary>Regras de validacao do registro de item na ata.</summary>
public sealed class RegistrarItemAtaValidator : AbstractValidator<RegistrarItemAtaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarItemAtaValidator()
    {
        RuleFor(c => c.AtaId).NotEmpty();
        RuleFor(c => c.ItemCatalogoId).NotEmpty();
        RuleFor(c => c.FornecedorBeneficiarioId).NotEmpty();
        RuleFor(c => c.PrecoRegistrado).GreaterThan(0);
        RuleFor(c => c.QuantidadeRegistrada).GreaterThan(0);
    }
}

/// <summary>Regras de validacao da adesao.</summary>
public sealed class RegistrarAdesaoValidator : AbstractValidator<RegistrarAdesaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAdesaoValidator()
    {
        RuleFor(c => c.AtaId).NotEmpty();
        RuleFor(c => c.ItemAtaId).NotEmpty();
        RuleFor(c => c.OrgaoAderente).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Quantidade).GreaterThan(0);
    }
}

/// <summary>Handler do registro de item na ata. Valida que o item de catalogo existe e esta ativo.</summary>
public sealed class RegistrarItemAtaHandler(
    IAtaRepository atas,
    ICatalogoRepository catalogo,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarItemAtaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarItemAtaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ata = await atas.ObterPorIdAsync(new AtaId(request.AtaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ata nao encontrada.");

        var item = await catalogo.ObterPorIdAsync(new ItemCatalogoId(request.ItemCatalogoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de catalogo nao encontrado.");
        if (item.Situacao != SituacaoItemCatalogo.Ativo)
        {
            throw new InvalidOperationException("Item de catalogo inativo nao pode ser registrado na ata.");
        }

        var itemAtaId = ata.RegistrarItem(
            item.Id,
            request.FornecedorBeneficiarioId,
            ValorMonetario.De(request.PrecoRegistrado),
            request.QuantidadeRegistrada);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return itemAtaId.Value;
    }
}

/// <summary>Handler da adesao (carona) a item da ata.</summary>
public sealed class RegistrarAdesaoHandler(IAtaRepository atas, IUnitOfWork unitOfWork, IDataHojeTenant dataHoje)
    : ICommandHandler<RegistrarAdesaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarAdesaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ata = await atas.ObterPorIdAsync(new AtaId(request.AtaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ata nao encontrada.");

        var adesaoId = ata.RegistrarAdesao(new ItemAtaId(request.ItemAtaId), request.OrgaoAderente, request.Quantidade, dataHoje.Hoje());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return adesaoId.Value;
    }
}

/// <summary>Handler da contratacao direta de item da ata pelo proprio ente.</summary>
public sealed class ContratarItemAtaHandler(IAtaRepository atas, IUnitOfWork unitOfWork, IDataHojeTenant dataHoje)
    : ICommandHandler<ContratarItemAtaCommand>
{
    /// <inheritdoc />
    public async Task Handle(ContratarItemAtaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ata = await atas.ObterPorIdAsync(new AtaId(request.AtaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ata nao encontrada.");
        ata.ContratarItem(new ItemAtaId(request.ItemAtaId), request.Quantidade, dataHoje.Hoje());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do cancelamento de ata.</summary>
public sealed class CancelarAtaHandler(IAtaRepository atas, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarAtaCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarAtaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ata = await atas.ObterPorIdAsync(new AtaId(request.AtaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ata nao encontrada.");
        ata.Cancelar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
