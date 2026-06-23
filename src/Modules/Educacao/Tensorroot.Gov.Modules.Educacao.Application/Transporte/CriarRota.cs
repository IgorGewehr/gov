using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Transporte;

/// <summary>
/// Cria uma rota de transporte (PNATE), situacao inicial Planejada. O veiculo (modalidade Proprio) e o
/// agregado Veiculo da Frota de Patrimonio, referenciado por Id (FK logica cross-module).
/// </summary>
/// <param name="EscolaId">Escola atendida.</param>
/// <param name="Nome">Nome/identificacao da rota.</param>
/// <param name="Turno">Turno de atendimento (reusa o Turno de Turmas).</param>
/// <param name="Modalidade">Modalidade de execucao (proprio/terceirizado).</param>
/// <param name="VeiculoId">Veiculo da Frota (Patrimonio) por Id — obrigatorio se Proprio.</param>
/// <param name="Quilometragem">Quilometragem do itinerario (&gt;= 0).</param>
public sealed record CriarRotaCommand(
    Guid EscolaId,
    string Nome,
    Turno Turno,
    ModalidadeTransporte Modalidade,
    Guid? VeiculoId,
    decimal Quilometragem) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de rota.</summary>
public sealed class CriarRotaValidator : AbstractValidator<CriarRotaCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarRotaValidator()
    {
        RuleFor(comando => comando.EscolaId).NotEmpty().WithMessage("Escola obrigatoria.");
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(RotaTransporte.ComprimentoNome).WithMessage("Nome obrigatorio.");
        RuleFor(comando => comando.Turno).IsInEnum().WithMessage("Turno invalido.");
        RuleFor(comando => comando.Modalidade).IsInEnum().WithMessage("Modalidade invalida.");
        RuleFor(comando => comando.Quilometragem).GreaterThanOrEqualTo(0m).WithMessage("Quilometragem nao pode ser negativa.");
    }
}

/// <summary>Handler da criacao de rota: valida a escola e cria a rota (situacao Planejada).</summary>
public sealed class CriarRotaHandler(
    IRotaTransporteRepository rotas,
    IEscolaRepository escolas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CriarRotaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarRotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var escolaId = new EscolaId(request.EscolaId);
        var escola = await escolas.ObterPorIdAsync(escolaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Escola nao encontrada.");
        if (escola.Encerrada)
        {
            throw new InvalidOperationException("Escola desativada nao admite novas rotas.");
        }

        var rota = RotaTransporte.Criar(
            tenant.TenantId,
            escolaId,
            request.Nome,
            request.Turno,
            request.Modalidade,
            request.VeiculoId,
            request.Quilometragem);

        rotas.Adicionar(rota);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return rota.Id.Value;
    }
}
