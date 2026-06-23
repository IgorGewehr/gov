using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Membro da comissão de inventário no comando de abertura.</summary>
/// <param name="ResponsavelId">Vínculo (Id) do servidor designado.</param>
/// <param name="Nome">Nome do membro.</param>
/// <param name="Presidente">Indica se preside a comissão.</param>
public sealed record MembroComissaoEntrada(Guid ResponsavelId, string Nome, bool Presidente);

/// <summary>
/// Abre um inventário patrimonial (Lei 4.320 art. 96) com a comissão designada por portaria.
/// O mínimo de membros é parametrizável por tenant (default <see cref="Inventario.MinimoMembrosComissaoPadrao"/>).
/// </summary>
/// <param name="Exercicio">Exercício (ano-base) do levantamento.</param>
/// <param name="Tipo">Tipo (1 = Anual, 2 = PorSetor, 3 = Eventual, 4 = Transferencia).</param>
/// <param name="Setor">Setor/UO escopo (nulo = geral).</param>
/// <param name="Portaria">Portaria de designação da comissão.</param>
/// <param name="DataAbertura">Data de abertura.</param>
/// <param name="Membros">Membros da comissão.</param>
/// <param name="MinimoMembrosComissao">Mínimo de membros exigido (parametrizável por tenant; nulo usa o padrão).</param>
public sealed record AbrirInventarioCommand(
    int Exercicio,
    int Tipo,
    string? Setor,
    string Portaria,
    DateOnly DataAbertura,
    IReadOnlyList<MembroComissaoEntrada> Membros,
    int? MinimoMembrosComissao) : ICommand<Guid>;

/// <summary>Regras de validação da abertura de inventário.</summary>
public sealed class AbrirInventarioValidator : AbstractValidator<AbrirInventarioCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirInventarioValidator()
    {
        RuleFor(comando => comando.Exercicio).GreaterThan(0);
        RuleFor(comando => comando.Tipo).Must(tipo => Enum.IsDefined((TipoInventario)tipo))
            .WithMessage("Tipo de inventário inválido.");
        RuleFor(comando => comando.Portaria).NotEmpty().MaximumLength(100);
        RuleFor(comando => comando.Setor).MaximumLength(200);
        RuleFor(comando => comando.Membros).NotEmpty();
        RuleForEach(comando => comando.Membros).ChildRules(membro =>
        {
            membro.RuleFor(m => m.ResponsavelId).NotEmpty();
            membro.RuleFor(m => m.Nome).NotEmpty().MaximumLength(200);
        });
        RuleFor(comando => comando.MinimoMembrosComissao!.Value).GreaterThan(0)
            .When(comando => comando.MinimoMembrosComissao.HasValue);
    }
}

/// <summary>Handler da abertura de inventário.</summary>
public sealed class AbrirInventarioHandler(
    IInventarioRepository inventarios,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirInventarioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirInventarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var membros = request.Membros
            .Select(m => MembroComissao.Designar(m.ResponsavelId, m.Nome, m.Presidente))
            .ToList();

        var inventario = Inventario.Abrir(
            tenant.TenantId,
            request.Exercicio,
            (TipoInventario)request.Tipo,
            request.Setor,
            request.Portaria,
            membros,
            request.DataAbertura,
            request.MinimoMembrosComissao ?? Inventario.MinimoMembrosComissaoPadrao);

        inventarios.Adicionar(inventario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return inventario.Id.Value;
    }
}
