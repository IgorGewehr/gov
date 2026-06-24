using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Arquivistica;

/// <summary>Item (classe documental) de um plano de classificacao a cadastrar.</summary>
/// <param name="Codigo">Codigo de classificacao (ex.: "040", "040.1").</param>
/// <param name="Assunto">Assunto/descricao da classe.</param>
/// <param name="AtividadeFim">Verdadeiro se atividade-fim; falso se atividade-meio.</param>
/// <param name="CodigoPai">Codigo da classe-pai (arvore), quando houver.</param>
public sealed record ItemPlanoClassificacao(string Codigo, string Assunto, bool AtividadeFim, string? CodigoPai);

/// <summary>
/// Cadastra o Plano de Classificacao documental do tenant (e-ARQ v2 / CONARQ — Peca 2 / W9.4).
/// </summary>
/// <param name="Nome">Nome do plano.</param>
/// <param name="Classes">Classes documentais (codigo x assunto).</param>
public sealed record CadastrarPlanoClassificacaoCommand(
    string Nome,
    IReadOnlyList<ItemPlanoClassificacao> Classes) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de plano de classificacao.</summary>
public sealed class CadastrarPlanoClassificacaoValidator : AbstractValidator<CadastrarPlanoClassificacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarPlanoClassificacaoValidator()
    {
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.Classes).NotEmpty();
        RuleForEach(comando => comando.Classes).ChildRules(item =>
        {
            item.RuleFor(classe => classe.Codigo).NotEmpty().MaximumLength(ClasseDocumental.ComprimentoMaximoCodigo);
            item.RuleFor(classe => classe.Assunto).NotEmpty().MaximumLength(ClasseDocumental.ComprimentoMaximoAssunto);
        });
    }
}

/// <summary>Handler do cadastro de plano de classificacao.</summary>
public sealed class CadastrarPlanoClassificacaoHandler(
    IPlanoDeClassificacaoRepository planos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarPlanoClassificacaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarPlanoClassificacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plano = PlanoDeClassificacao.Criar(tenant.TenantId, request.Nome);
        foreach (var item in request.Classes)
        {
            var atividade = item.AtividadeFim ? AtividadeMeioOuFim.Fim : AtividadeMeioOuFim.Meio;
            plano.AdicionarClasse(item.Codigo, item.Assunto, atividade, item.CodigoPai);
        }

        planos.Adicionar(plano);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plano.Id.Value;
    }
}
