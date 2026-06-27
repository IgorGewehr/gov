using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota.Pneus;

/// <summary>Cadastra um novo pneu, ingressando-o em estoque.</summary>
/// <param name="NumeroFogo">Número de fogo (identificação única do pneu).</param>
/// <param name="Marca">Marca do pneu.</param>
/// <param name="Modelo">Modelo/desenho do pneu.</param>
/// <param name="Medida">Medida do pneu (padrão da indústria).</param>
/// <param name="Dot">Código DOT (semana/ano de fabricação), opcional.</param>
/// <param name="SulcoNovoMilimetros">Sulco de fábrica (mm).</param>
/// <param name="VidaUtilKmEstimada">Vida útil estimada por km.</param>
/// <param name="ValorAquisicao">Valor de aquisição.</param>
/// <param name="DataAquisicao">Data de aquisição.</param>
public sealed record CadastrarPneuCommand(
    string NumeroFogo,
    string Marca,
    string Modelo,
    string Medida,
    string? Dot,
    decimal SulcoNovoMilimetros,
    int VidaUtilKmEstimada,
    decimal ValorAquisicao,
    DateOnly DataAquisicao) : ICommand<Guid>;

/// <summary>Regras de validação do cadastro de pneu.</summary>
public sealed class CadastrarPneuValidator : AbstractValidator<CadastrarPneuCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarPneuValidator()
    {
        RuleFor(comando => comando.NumeroFogo).NotEmpty().MaximumLength(40);
        RuleFor(comando => comando.Marca).NotEmpty().MaximumLength(60);
        RuleFor(comando => comando.Modelo).NotEmpty().MaximumLength(80);
        RuleFor(comando => comando.Medida).NotEmpty().MaximumLength(30);
        RuleFor(comando => comando.Dot).MaximumLength(8);
        RuleFor(comando => comando.SulcoNovoMilimetros).GreaterThan(0);
        RuleFor(comando => comando.VidaUtilKmEstimada).GreaterThan(0);
        RuleFor(comando => comando.ValorAquisicao).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler do cadastro de pneu.</summary>
public sealed class CadastrarPneuHandler(
    IPneuRepository pneus,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext)
    : ICommandHandler<CadastrarPneuCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarPneuCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await pneus.ExisteNumeroFogoAsync(request.NumeroFogo, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Já existe um pneu com o número de fogo '{request.NumeroFogo}' no tenant.");
        }

        var pneu = Pneu.Cadastrar(
            tenantContext.TenantId,
            request.NumeroFogo,
            request.Marca,
            request.Modelo,
            request.Medida,
            request.Dot,
            request.SulcoNovoMilimetros,
            request.VidaUtilKmEstimada,
            ValorMonetario.De(request.ValorAquisicao),
            request.DataAquisicao);

        pneus.Adicionar(pneu);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return pneu.Id.Value;
    }
}
