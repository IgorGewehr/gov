using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Vereadores;

/// <summary>Edita os dados cadastrais e a situacao de mandato de um vereador.</summary>
/// <param name="VereadorId">Vereador alvo.</param>
/// <param name="NomeCivil">Novo nome civil.</param>
/// <param name="NomeParlamentar">Novo nome parlamentar.</param>
/// <param name="Partido">Nova sigla partidaria.</param>
/// <param name="CargoMesa">Novo cargo na Mesa Diretora.</param>
/// <param name="Situacao">Nova situacao de mandato.</param>
public sealed record EditarVereadorCommand(
    Guid VereadorId,
    string NomeCivil,
    string NomeParlamentar,
    string Partido,
    int CargoMesa,
    int Situacao) : ICommand;

/// <summary>Regras de validacao da edicao de vereador.</summary>
public sealed class EditarVereadorValidator : AbstractValidator<EditarVereadorCommand>
{
    /// <summary>Define as regras.</summary>
    public EditarVereadorValidator()
    {
        RuleFor(comando => comando.VereadorId).NotEmpty();
        RuleFor(comando => comando.NomeCivil).NotEmpty().MaximumLength(Vereador.NomeMaximo);
        RuleFor(comando => comando.Partido).NotEmpty().MaximumLength(Vereador.PartidoMaximo);
        RuleFor(comando => comando.CargoMesa).Must(valor => Enum.IsDefined((CargoMesa)valor));
        RuleFor(comando => comando.Situacao).Must(valor => Enum.IsDefined((SituacaoVereador)valor));
    }
}

/// <summary>Handler da edicao de vereador.</summary>
public sealed class EditarVereadorHandler(
    IVereadorRepository vereadores,
    IUnitOfWork unitOfWork) : ICommandHandler<EditarVereadorCommand>
{
    /// <inheritdoc />
    public async Task Handle(EditarVereadorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vereador = await vereadores.ObterPorIdAsync(new VereadorId(request.VereadorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Vereador nao encontrado.");

        vereador.AtualizarCadastro(request.NomeCivil, request.NomeParlamentar, request.Partido, (CargoMesa)request.CargoMesa);
        vereador.AlterarSituacao((SituacaoVereador)request.Situacao);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
