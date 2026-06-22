using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

namespace Tensorroot.Gov.Modules.Legislativo.Application.DiarioOficial;

/// <summary>Abre uma nova edicao do Diario Oficial em rascunho (numero sequencial por tenant/ano) — D-1, D-5.</summary>
/// <param name="Ano">Ano da edicao.</param>
public sealed record AbrirEdicaoDiarioCommand(int Ano) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de edicao.</summary>
public sealed class AbrirEdicaoDiarioValidator : AbstractValidator<AbrirEdicaoDiarioCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirEdicaoDiarioValidator() => RuleFor(comando => comando.Ano).InclusiveBetween(1900, 2100);
}

/// <summary>Handler da abertura de edicao (gera numero sequencial no tenant/ano).</summary>
public sealed class AbrirEdicaoDiarioHandler(
    IEdicaoDiarioRepository edicoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<AbrirEdicaoDiarioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirEdicaoDiarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var numero = await edicoes.ProximoNumeroAsync(request.Ano, cancellationToken).ConfigureAwait(false);
        var edicao = EdicaoDiario.Abrir(tenant.TenantId, numero, request.Ano);

        edicoes.Adicionar(edicao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return edicao.Id.Value;
    }
}

/// <summary>Adiciona uma materia a uma edicao em rascunho — D-2.</summary>
/// <param name="EdicaoId">Edicao alvo.</param>
/// <param name="TipoMateria">Especie da materia (<see cref="Domain.DiarioOficial.TipoMateria"/>).</param>
/// <param name="Titulo">Titulo.</param>
/// <param name="Conteudo">Conteudo bruto (opcional).</param>
/// <param name="ReferenciaId">Referencia a entidade de origem (opcional).</param>
public sealed record AdicionarMateriaCommand(
    Guid EdicaoId,
    int TipoMateria,
    string Titulo,
    string? Conteudo,
    Guid? ReferenciaId) : ICommand<Guid>;

/// <summary>Regras de validacao da inclusao de materia.</summary>
public sealed class AdicionarMateriaValidator : AbstractValidator<AdicionarMateriaCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarMateriaValidator()
    {
        RuleFor(comando => comando.EdicaoId).NotEmpty();
        RuleFor(comando => comando.TipoMateria).Must(valor => Enum.IsDefined(typeof(TipoMateria), valor))
            .WithMessage("Tipo de materia invalido.");
        RuleFor(comando => comando.Titulo).NotEmpty().MaximumLength(MateriaDiario.TituloMaximo);
    }
}

/// <summary>Handler da inclusao de materia.</summary>
public sealed class AdicionarMateriaHandler(IEdicaoDiarioRepository edicoes, IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarMateriaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarMateriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var edicao = await edicoes.ObterPorIdAsync(new EdicaoDiarioId(request.EdicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Edicao do Diario nao encontrada.");

        var materiaId = edicao.AdicionarMateria(
            (TipoMateria)request.TipoMateria,
            request.Titulo,
            request.Conteudo,
            request.ReferenciaId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return materiaId.Value;
    }
}

/// <summary>Abre uma edicao de retificacao referenciando a edicao original (sem mutar a original) — D-4.</summary>
/// <param name="EdicaoOriginalId">Edicao original a retificar.</param>
public sealed record RetificarEdicaoCommand(Guid EdicaoOriginalId) : ICommand<Guid>;

/// <summary>Regras de validacao da retificacao.</summary>
public sealed class RetificarEdicaoValidator : AbstractValidator<RetificarEdicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RetificarEdicaoValidator() => RuleFor(comando => comando.EdicaoOriginalId).NotEmpty();
}

/// <summary>Handler da retificacao.</summary>
public sealed class RetificarEdicaoHandler(
    IEdicaoDiarioRepository edicoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<RetificarEdicaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RetificarEdicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var original = await edicoes.ObterPorIdAsync(new EdicaoDiarioId(request.EdicaoOriginalId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Edicao original nao encontrada.");

        var numero = await edicoes.ProximoNumeroAsync(original.Ano, cancellationToken).ConfigureAwait(false);
        var retificadora = EdicaoDiario.Retificar(tenant.TenantId, numero, original.Ano, original.Id);

        edicoes.Adicionar(retificadora);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return retificadora.Id.Value;
    }
}
