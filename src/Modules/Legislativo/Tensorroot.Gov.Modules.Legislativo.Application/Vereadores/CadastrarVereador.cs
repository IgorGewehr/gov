using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Vereadores;

/// <summary>Cadastra um novo vereador (parlamentar) no tenant, em exercicio.</summary>
/// <param name="NomeCivil">Nome civil completo.</param>
/// <param name="NomeParlamentar">Nome parlamentar (exibido no painel/ata; default = nome civil).</param>
/// <param name="Partido">Sigla partidaria.</param>
/// <param name="LegislaturaInicio">Ano de inicio da legislatura.</param>
/// <param name="LegislaturaFim">Ano de fim da legislatura.</param>
/// <param name="CargoMesa">Cargo na Mesa Diretora (0 = Nenhum).</param>
public sealed record CadastrarVereadorCommand(
    string NomeCivil,
    string NomeParlamentar,
    string Partido,
    int LegislaturaInicio,
    int LegislaturaFim,
    int CargoMesa) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de vereador.</summary>
public sealed class CadastrarVereadorValidator : AbstractValidator<CadastrarVereadorCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarVereadorValidator()
    {
        RuleFor(comando => comando.NomeCivil).NotEmpty().MaximumLength(Vereador.NomeMaximo);
        RuleFor(comando => comando.Partido).NotEmpty().MaximumLength(Vereador.PartidoMaximo);
        RuleFor(comando => comando.LegislaturaInicio).InclusiveBetween(1900, 2100);
        RuleFor(comando => comando.LegislaturaFim).GreaterThan(comando => comando.LegislaturaInicio);
        RuleFor(comando => comando.CargoMesa).Must(valor => Enum.IsDefined((CargoMesa)valor));
    }
}

/// <summary>Handler do cadastro de vereador.</summary>
public sealed class CadastrarVereadorHandler(
    IVereadorRepository vereadores,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<CadastrarVereadorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarVereadorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vereador = Vereador.Cadastrar(
            tenant.TenantId,
            request.NomeCivil,
            request.NomeParlamentar,
            request.Partido,
            request.LegislaturaInicio,
            request.LegislaturaFim,
            (CargoMesa)request.CargoMesa);

        vereadores.Adicionar(vereador);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return vereador.Id.Value;
    }
}
