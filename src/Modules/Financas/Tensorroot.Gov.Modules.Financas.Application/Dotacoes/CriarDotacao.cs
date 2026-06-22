using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Dotacoes;

/// <summary>Cria uma dotação orçamentária (crédito da LOA).</summary>
/// <param name="Exercicio">Exercício orçamentário.</param>
/// <param name="Orgao">Código do órgão.</param>
/// <param name="Unidade">Código da unidade orçamentária.</param>
/// <param name="FuncionalProgramatica">Funcional-programática.</param>
/// <param name="CategoriaEconomica">Categoria econômica.</param>
/// <param name="FonteRecurso">Fonte de recurso.</param>
/// <param name="ValorDotado">Valor dotado inicial.</param>
public sealed record CriarDotacaoCommand(
    int Exercicio,
    string Orgao,
    string Unidade,
    string FuncionalProgramatica,
    CategoriaEconomica CategoriaEconomica,
    string FonteRecurso,
    decimal ValorDotado) : ICommand<Guid>;

/// <summary>Regras de validação da criação de dotação.</summary>
public sealed class CriarDotacaoValidator : AbstractValidator<CriarDotacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarDotacaoValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(DotacaoOrcamentaria.ExercicioMinimo);
        RuleFor(c => c.Orgao).NotEmpty().MaximumLength(10);
        RuleFor(c => c.Unidade).NotEmpty().MaximumLength(20);
        RuleFor(c => c.FuncionalProgramatica).NotEmpty().MaximumLength(50);
        RuleFor(c => c.CategoriaEconomica).IsInEnum();
        RuleFor(c => c.FonteRecurso).NotEmpty().MaximumLength(20);
        RuleFor(c => c.ValorDotado).GreaterThan(0m);
    }
}

/// <summary>Handler da criação de dotação.</summary>
public sealed class CriarDotacaoHandler(IDotacaoOrcamentariaRepository dotacoes, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<CriarDotacaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarDotacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var classificacao = ClassificacaoOrcamentaria.De(
            request.Orgao,
            request.Unidade,
            request.FuncionalProgramatica,
            request.CategoriaEconomica,
            request.FonteRecurso);

        var dotacao = DotacaoOrcamentaria.Criar(
            tenant.TenantId,
            request.Exercicio,
            classificacao,
            ValorMonetario.De(request.ValorDotado));

        dotacoes.Adicionar(dotacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return dotacao.Id.Value;
    }
}
