using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Comissoes;

/// <summary>Cria uma comissao (permanente/temporaria) — C-1.</summary>
/// <param name="Nome">Nome da comissao.</param>
/// <param name="Tipo">Natureza (<see cref="TipoComissao"/>).</param>
public sealed record CriarComissaoCommand(string Nome, int Tipo) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de comissao.</summary>
public sealed class CriarComissaoValidator : AbstractValidator<CriarComissaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarComissaoValidator()
    {
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(Comissao.NomeMaximo);
        RuleFor(comando => comando.Tipo).Must(valor => Enum.IsDefined(typeof(TipoComissao), valor))
            .WithMessage("Tipo de comissao invalido.");
    }
}

/// <summary>Handler da criacao de comissao.</summary>
public sealed class CriarComissaoHandler(
    IComissaoRepository comissoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<CriarComissaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarComissaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comissao = Comissao.Criar(tenant.TenantId, request.Nome, (TipoComissao)request.Tipo);
        comissoes.Adicionar(comissao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return comissao.Id.Value;
    }
}

/// <summary>Designa (ou atualiza) um membro da comissao — C-2/C-3.</summary>
/// <param name="ComissaoId">Comissao alvo.</param>
/// <param name="VereadorId">Vereador.</param>
/// <param name="Papel">Papel (<see cref="PapelMembro"/>).</param>
/// <param name="Cargo">Cargo de direcao (<see cref="CargoComissao"/>).</param>
public sealed record DesignarMembroCommand(Guid ComissaoId, Guid VereadorId, int Papel, int Cargo) : ICommand<Guid>;

/// <summary>Regras de validacao da designacao de membro.</summary>
public sealed class DesignarMembroValidator : AbstractValidator<DesignarMembroCommand>
{
    /// <summary>Define as regras.</summary>
    public DesignarMembroValidator()
    {
        RuleFor(comando => comando.ComissaoId).NotEmpty();
        RuleFor(comando => comando.VereadorId).NotEmpty();
        RuleFor(comando => comando.Papel).Must(valor => Enum.IsDefined(typeof(PapelMembro), valor))
            .WithMessage("Papel de membro invalido.");
        RuleFor(comando => comando.Cargo).Must(valor => Enum.IsDefined(typeof(CargoComissao), valor))
            .WithMessage("Cargo de comissao invalido.");
    }
}

/// <summary>Handler da designacao de membro.</summary>
public sealed class DesignarMembroHandler(IComissaoRepository comissoes, IUnitOfWork unitOfWork)
    : ICommandHandler<DesignarMembroCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(DesignarMembroCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comissao = await comissoes.ObterPorIdAsync(new ComissaoId(request.ComissaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Comissao nao encontrada.");

        var membro = comissao.DesignarMembro(
            new VereadorId(request.VereadorId),
            (PapelMembro)request.Papel,
            (CargoComissao)request.Cargo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return membro.Id.Value;
    }
}

/// <summary>Remove um membro da comissao — C-4.</summary>
/// <param name="ComissaoId">Comissao alvo.</param>
/// <param name="VereadorId">Vereador a remover.</param>
public sealed record RemoverMembroCommand(Guid ComissaoId, Guid VereadorId) : ICommand;

/// <summary>Regras de validacao da remocao de membro.</summary>
public sealed class RemoverMembroValidator : AbstractValidator<RemoverMembroCommand>
{
    /// <summary>Define as regras.</summary>
    public RemoverMembroValidator()
    {
        RuleFor(comando => comando.ComissaoId).NotEmpty();
        RuleFor(comando => comando.VereadorId).NotEmpty();
    }
}

/// <summary>Handler da remocao de membro.</summary>
public sealed class RemoverMembroHandler(IComissaoRepository comissoes, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoverMembroCommand>
{
    /// <inheritdoc />
    public async Task Handle(RemoverMembroCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comissao = await comissoes.ObterPorIdAsync(new ComissaoId(request.ComissaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Comissao nao encontrada.");

        comissao.RemoverMembro(new VereadorId(request.VereadorId));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Extingue uma comissao (terminal) — C-5.</summary>
/// <param name="ComissaoId">Comissao alvo.</param>
public sealed record ExtinguirComissaoCommand(Guid ComissaoId) : ICommand;

/// <summary>Regras de validacao da extincao de comissao.</summary>
public sealed class ExtinguirComissaoValidator : AbstractValidator<ExtinguirComissaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ExtinguirComissaoValidator() => RuleFor(comando => comando.ComissaoId).NotEmpty();
}

/// <summary>Handler da extincao de comissao.</summary>
public sealed class ExtinguirComissaoHandler(IComissaoRepository comissoes, IUnitOfWork unitOfWork)
    : ICommandHandler<ExtinguirComissaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ExtinguirComissaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comissao = await comissoes.ObterPorIdAsync(new ComissaoId(request.ComissaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Comissao nao encontrada.");

        comissao.Extinguir();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
