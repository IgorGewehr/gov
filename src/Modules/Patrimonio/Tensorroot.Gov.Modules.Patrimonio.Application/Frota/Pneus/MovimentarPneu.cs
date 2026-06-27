using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota.Pneus;

/// <summary>Instala um pneu (em estoque) numa posição (eixo/lado) de um veículo.</summary>
/// <param name="PneuId">Pneu a instalar.</param>
/// <param name="VeiculoId">Veículo de destino.</param>
/// <param name="Eixo">Eixo de montagem.</param>
/// <param name="Lado">Lado/posição de montagem.</param>
/// <param name="OdometroVeiculo">Odômetro atual do veículo (km).</param>
public sealed record InstalarPneuCommand(
    Guid PneuId,
    Guid VeiculoId,
    Eixo Eixo,
    LadoMontagem Lado,
    int OdometroVeiculo) : ICommand;

/// <summary>Regras de validação da instalação de pneu.</summary>
public sealed class InstalarPneuValidator : AbstractValidator<InstalarPneuCommand>
{
    /// <summary>Define as regras.</summary>
    public InstalarPneuValidator()
    {
        RuleFor(comando => comando.PneuId).NotEmpty();
        RuleFor(comando => comando.VeiculoId).NotEmpty();
        RuleFor(comando => comando.Eixo).IsInEnum();
        RuleFor(comando => comando.Lado).IsInEnum();
        RuleFor(comando => comando.OdometroVeiculo).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler da instalação de pneu.</summary>
public sealed class InstalarPneuHandler(IPneuRepository pneus, IUnitOfWork unitOfWork)
    : ICommandHandler<InstalarPneuCommand>
{
    /// <inheritdoc />
    public async Task Handle(InstalarPneuCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pneu = await pneus.ObterPorIdAsync(new PneuId(request.PneuId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pneu não encontrado.");

        var veiculoId = new VeiculoId(request.VeiculoId);
        var posicao = PosicaoPneu.De(request.Eixo, request.Lado);

        if (await pneus.PosicaoOcupadaAsync(veiculoId, posicao, pneu.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"A posição {posicao.Codigo} do veículo já está ocupada por outro pneu; remova-o antes.");
        }

        pneu.Instalar(veiculoId, posicao, request.OdometroVeiculo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Remove um pneu instalado do veículo (rodízio/reposicionamento/manutenção), aferindo o sulco.</summary>
/// <param name="PneuId">Pneu a remover.</param>
/// <param name="OdometroVeiculo">Odômetro do veículo na remoção (km).</param>
/// <param name="SulcoAferidoMilimetros">Sulco aferido na remoção (mm).</param>
/// <param name="RetornarAoEstoque">Se verdadeiro, devolve o pneu ao estoque após a remoção (pronto a reinstalar).</param>
public sealed record RemoverPneuCommand(
    Guid PneuId,
    int OdometroVeiculo,
    decimal SulcoAferidoMilimetros,
    bool RetornarAoEstoque) : ICommand;

/// <summary>Regras de validação da remoção de pneu.</summary>
public sealed class RemoverPneuValidator : AbstractValidator<RemoverPneuCommand>
{
    /// <summary>Define as regras.</summary>
    public RemoverPneuValidator()
    {
        RuleFor(comando => comando.PneuId).NotEmpty();
        RuleFor(comando => comando.OdometroVeiculo).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.SulcoAferidoMilimetros).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler da remoção de pneu.</summary>
public sealed class RemoverPneuHandler(IPneuRepository pneus, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoverPneuCommand>
{
    /// <inheritdoc />
    public async Task Handle(RemoverPneuCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pneu = await pneus.ObterPorIdAsync(new PneuId(request.PneuId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pneu não encontrado.");

        pneu.Remover(request.OdometroVeiculo, request.SulcoAferidoMilimetros);
        if (request.RetornarAoEstoque)
        {
            pneu.RetornarAoEstoque();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
