using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Planejamento.Creditos;

/// <summary>
/// Abre um crédito adicional que ALTERA a LOA (Lei 4.320/64 arts. 40-46):
/// <list type="bullet">
/// <item>Suplementar — reforça uma dotação existente (e, se fonte por anulação, anula outra);</item>
/// <item>Especial/Extraordinário — cria uma dotação NOVA via item extraordinário da LOA.</item>
/// </list>
/// </summary>
public sealed record AbrirCreditoAdicionalCommand(
    Guid LoaId,
    EspecieCredito Especie,
    FonteRecursoCredito Fonte,
    decimal Valor,
    string AtoAutorizador,
    string AtoAbertura,
    bool PorDecreto,
    Guid? DotacaoAlvoId,
    Guid? DotacaoAnuladaId,
    Guid? AcaoPpaId,
    string? Orgao,
    string? Unidade,
    string? FuncionalProgramatica,
    CategoriaEconomica? CategoriaEconomica,
    string? FonteRecursoClassificacao,
    string? NaturezaDespesa) : ICommand<Guid>;

/// <summary>Validação da abertura de crédito adicional.</summary>
public sealed class AbrirCreditoAdicionalValidator : AbstractValidator<AbrirCreditoAdicionalCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirCreditoAdicionalValidator()
    {
        RuleFor(c => c.LoaId).NotEmpty();
        RuleFor(c => c.Especie).Must(e => Enum.IsDefined(e)).WithMessage("Especie de credito invalida.");
        RuleFor(c => c.Fonte).Must(f => Enum.IsDefined(f)).WithMessage("Fonte de recurso invalida.");
        RuleFor(c => c.Valor).GreaterThan(0m);
        RuleFor(c => c.AtoAbertura).NotEmpty().MaximumLength(100);

        // Suplementar exige dotacao alvo.
        When(c => c.Especie == EspecieCredito.Suplementar, () =>
            RuleFor(c => c.DotacaoAlvoId).NotNull().WithMessage("Suplementar exige a dotacao alvo."));

        // Especial/Extraordinario criam dotacao nova: exigem a classificacao + acao.
        When(c => c.Especie != EspecieCredito.Suplementar, () =>
        {
            RuleFor(c => c.AcaoPpaId).NotNull();
            RuleFor(c => c.Orgao).NotEmpty();
            RuleFor(c => c.Unidade).NotEmpty();
            RuleFor(c => c.FuncionalProgramatica).NotEmpty();
            RuleFor(c => c.CategoriaEconomica).NotNull();
            RuleFor(c => c.FonteRecursoClassificacao).NotEmpty();
            RuleFor(c => c.NaturezaDespesa).NotEmpty();
        });
    }
}

/// <summary>Handler da abertura de crédito adicional (aplica o efeito nas dotações).</summary>
public sealed class AbrirCreditoAdicionalHandler(
    ILoaRepository loas,
    ICreditoAdicionalRepository creditos,
    IDotacaoOrcamentariaRepository dotacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<AbrirCreditoAdicionalCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirCreditoAdicionalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LOA nao encontrada.");

        var valor = ValorMonetario.De(request.Valor);

        var credito = request.Especie switch
        {
            EspecieCredito.Suplementar => await CriarSuplementarAsync(request, loa, valor, cancellationToken).ConfigureAwait(false),
            EspecieCredito.Especial => CriarEspecial(request, loa, valor),
            EspecieCredito.Extraordinario => CreditoAdicional.Extraordinario(tenant.TenantId, loa, valor, request.AtoAbertura),
            _ => throw new InvalidOperationException("Especie de credito nao suportada."),
        };

        credito.Abrir();
        creditos.Adicionar(credito);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return credito.Id.Value;
    }

    private async Task<CreditoAdicional> CriarSuplementarAsync(
        AbrirCreditoAdicionalCommand request,
        LeiOrcamentariaAnual loa,
        ValorMonetario valor,
        CancellationToken cancellationToken)
    {
        var alvo = await dotacoes.ObterPorIdAsync(new DotacaoOrcamentariaId(request.DotacaoAlvoId!.Value), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dotacao alvo nao encontrada.");

        DotacaoOrcamentaria? anulada = null;
        if (request.Fonte == FonteRecursoCredito.AnulacaoDotacao)
        {
            anulada = await dotacoes.ObterPorIdAsync(new DotacaoOrcamentariaId(request.DotacaoAnuladaId!.Value), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Dotacao a anular nao encontrada.");
        }

        // Limite de suplementacao por decreto (art. 7º / CF 167, V): soma de suplementacoes por decreto
        // ja abertas + esta nao pode exceder o % autorizado sobre o total fixado.
        if (request.PorDecreto)
        {
            var jaSuplementado = await creditos.SomarSuplementacoesPorDecretoAsync(loa.Id, cancellationToken).ConfigureAwait(false);
            var limite = loa.TotalDespesaFixada.Valor * (loa.LimiteSuplementacaoPercentual / 100m);
            if (jaSuplementado + valor.Valor > limite)
            {
                throw new LimiteSuplementacaoExcedidoException(limite, jaSuplementado + valor.Valor);
            }
        }

        var credito = CreditoAdicional.Suplementar(
            tenant.TenantId, loa, valor, request.Fonte,
            alvo.Id, anulada?.Id, request.AtoAutorizador, request.AtoAbertura, request.PorDecreto);

        // Efeito: anula a origem (se houver) e reforca o alvo — reusa o roteiro contabil EVT-DOT.
        anulada?.AnularCredito(valor);
        alvo.Reforcar(valor);

        return credito;
    }

    private CreditoAdicional CriarEspecial(AbrirCreditoAdicionalCommand request, LeiOrcamentariaAnual loa, ValorMonetario valor)
    {
        DotacaoOrcamentariaId? anuladaId = null;
        if (request.Fonte == FonteRecursoCredito.AnulacaoDotacao && request.DotacaoAnuladaId is { } id)
        {
            anuladaId = new DotacaoOrcamentariaId(id);
        }

        var credito = CreditoAdicional.Especial(tenant.TenantId, loa, valor, request.Fonte, anuladaId, request.AtoAutorizador, request.AtoAbertura);
        CriarDotacaoNova(request, loa, valor, credito);
        return credito;
    }

    private void CriarDotacaoNova(AbrirCreditoAdicionalCommand request, LeiOrcamentariaAnual loa, ValorMonetario valor, CreditoAdicional credito)
    {
        var classificacao = ClassificacaoOrcamentaria.De(
            request.Orgao!, request.Unidade!, request.FuncionalProgramatica!, request.CategoriaEconomica!.Value, request.FonteRecursoClassificacao!);

        // Credito especial/extraordinario: cria item extraordinario na LOA (gera a dotacao via vinculo).
        // Aqui criamos a dotacao diretamente e a vinculamos como alvo (o item fica na LOA p/ rastreio).
        var itemId = loa.FixarDespesaPorCreditoEspecial(classificacao, new AcaoPpaId(request.AcaoPpaId!.Value), request.NaturezaDespesa!, valor);

        var dotacao = DotacaoOrcamentaria.CriarDeLoa(
            tenant.TenantId, loa.Exercicio, classificacao, valor, loa.Id.Value, itemId.Value, request.AcaoPpaId!.Value);
        dotacoes.Adicionar(dotacao);
        loa.RegistrarDotacaoGerada(itemId, dotacao.Id);
        credito.DefinirDotacaoAlvo(dotacao.Id);
    }
}
