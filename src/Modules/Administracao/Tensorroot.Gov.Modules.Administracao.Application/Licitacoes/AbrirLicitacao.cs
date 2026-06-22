using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Abre uma licitacao: publica o edital e inicia o certame (nasce <c>Aberta</c>).</summary>
/// <param name="Objeto">Descricao do objeto licitado.</param>
/// <param name="Modalidade">Modalidade do certame (art. 28/74/75).</param>
/// <param name="CriterioJulgamento">Criterio de julgamento (art. 33).</param>
/// <param name="ValorEstimado">Valor estimado/orcado da contratacao.</param>
/// <param name="EtpId">Referencia ao Estudo Tecnico Preliminar (opcional).</param>
/// <param name="TermoReferenciaId">Referencia ao Termo de Referencia (opcional).</param>
public sealed record AbrirLicitacaoCommand(
    string Objeto,
    ModalidadeLicitacao Modalidade,
    CriterioJulgamento CriterioJulgamento,
    decimal ValorEstimado,
    Guid? EtpId,
    Guid? TermoReferenciaId) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de licitacao.</summary>
public sealed class AbrirLicitacaoValidator : AbstractValidator<AbrirLicitacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirLicitacaoValidator()
    {
        RuleFor(comando => comando.Objeto)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Objeto e obrigatorio (max. 500 caracteres).");
        RuleFor(comando => comando.Modalidade).IsInEnum().WithMessage("Modalidade invalida.");
        RuleFor(comando => comando.CriterioJulgamento).IsInEnum().WithMessage("Criterio de julgamento invalido.");
        RuleFor(comando => comando.ValorEstimado).GreaterThan(0).WithMessage("Valor estimado deve ser positivo.");
        RuleFor(comando => comando)
            .Must(PregaoExigeMenorPrecoOuDesconto)
            .WithMessage("Pregao admite apenas menor preco ou maior desconto.");
    }

    private static bool PregaoExigeMenorPrecoOuDesconto(AbrirLicitacaoCommand comando)
        => comando.Modalidade != ModalidadeLicitacao.Pregao
        || comando.CriterioJulgamento is CriterioJulgamento.MenorPreco or CriterioJulgamento.MaiorDesconto;
}

/// <summary>Handler da abertura de licitacao.</summary>
public sealed class AbrirLicitacaoHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirLicitacaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirLicitacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = Licitacao.Abrir(
            tenant.TenantId,
            request.Objeto,
            request.Modalidade,
            request.CriterioJulgamento,
            ValorMonetario.De(request.ValorEstimado),
            request.EtpId,
            request.TermoReferenciaId);

        licitacoes.Adicionar(licitacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return licitacao.Id.Value;
    }
}
