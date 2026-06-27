using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>
/// Adiciona uma retenção/consignação a uma liquidação. Para IRRF/PJ, calcula automaticamente pela tabela
/// vigente (IN RFB 1.234/2012) a partir do enquadramento e da base; para as demais naturezas, aceita o
/// valor informado. A base default é o valor liquidado (bruto).
/// </summary>
/// <param name="LiquidacaoId">Liquidação onerada.</param>
/// <param name="Natureza">Natureza da retenção.</param>
/// <param name="EnquadramentoIrrf">Código do enquadramento IRRF (Anexo I), quando IRRF/PJ por tabela.</param>
/// <param name="BaseCalculo">Base de cálculo (default = valor liquidado).</param>
/// <param name="ValorInformado">Valor retido informado (naturezas sem cálculo por tabela).</param>
/// <param name="AliquotaInformada">Alíquota informada (fração), quando aplicável.</param>
/// <param name="CodigoReceita">Código de receita (DARF/guia), quando aplicável.</param>
/// <param name="FavorecidoDocumento">Documento do favorecido/recolhedor.</param>
/// <param name="Descricao">Descrição/histórico.</param>
public sealed record AdicionarRetencaoCommand(
    Guid LiquidacaoId,
    NaturezaRetencao Natureza,
    string? EnquadramentoIrrf,
    decimal? BaseCalculo,
    decimal? ValorInformado,
    decimal? AliquotaInformada,
    string? CodigoReceita,
    string? FavorecidoDocumento,
    string? Descricao) : ICommand<Guid>;

/// <summary>Validação da adição de retenção.</summary>
public sealed class AdicionarRetencaoValidator : AbstractValidator<AdicionarRetencaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarRetencaoValidator()
    {
        RuleFor(c => c.LiquidacaoId).NotEmpty();
        RuleFor(c => c.Natureza).IsInEnum();
        RuleFor(c => c.EnquadramentoIrrf)
            .NotEmpty()
            .When(c => c.Natureza == NaturezaRetencao.IrrfPessoaJuridica && c.ValorInformado is null)
            .WithMessage("Informe o enquadramento IRRF ou o valor a reter.");
        RuleFor(c => c.ValorInformado)
            .GreaterThan(0m)
            .When(c => c.ValorInformado.HasValue);
        RuleFor(c => c.BaseCalculo)
            .GreaterThan(0m)
            .When(c => c.BaseCalculo.HasValue);
    }
}

/// <summary>Handler da adição de retenção.</summary>
public sealed class AdicionarRetencaoHandler(
    ILiquidacaoRepository liquidacoes,
    ITabelaIrrfServicosRepository tabelas,
    IUnitOfWork unitOfWork) : ICommandHandler<AdicionarRetencaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarRetencaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var liquidacao = await liquidacoes.ObterPorIdAsync(new LiquidacaoId(request.LiquidacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Liquidacao nao encontrada.");

        var baseCalculo = request.BaseCalculo is { } b ? ValorMonetario.De(b) : liquidacao.Valor;

        Retencao retencao;
        if (request.Natureza == NaturezaRetencao.IrrfPessoaJuridica && request.ValorInformado is null)
        {
            // Cálculo automático pela tabela vigente (IN RFB 1.234/2012).
            var tabela = await tabelas.ObterVigenteAsync(liquidacao.DataLiquidacao, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Tabela IRRF vigente nao encontrada (rodar seed da tabela de IRRF de servicos).");

            var apuracao = tabela.Apurar(request.EnquadramentoIrrf!, baseCalculo);
            if (apuracao.DispensadoPorMinimo || !apuracao.ValorRetido.EhPositivo())
            {
                // Dispensa por valor mínimo (art. 3º, §6º): nada a reter.
                throw new InvalidOperationException(
                    $"IRRF dispensado por valor minimo (abaixo de {tabela.ValorMinimoRetencao:0.00}); retencao nao registrada.");
            }

            retencao = Retencao.Criar(
                NaturezaRetencao.IrrfPessoaJuridica,
                apuracao.ValorRetido,
                baseCalculo,
                request.Descricao ?? $"IRRF/PJ {apuracao.CodigoFaixa} (IN RFB 1234/2012)",
                codigoReceita: request.CodigoReceita ?? apuracao.CodigoReceitaDarf,
                aliquota: apuracao.Aliquota,
                favorecidoDocumento: request.FavorecidoDocumento);
        }
        else
        {
            var valor = ValorMonetario.De(request.ValorInformado!.Value);
            retencao = Retencao.Criar(
                request.Natureza,
                valor,
                baseCalculo,
                request.Descricao ?? request.Natureza.ToString(),
                codigoReceita: request.CodigoReceita,
                aliquota: request.AliquotaInformada,
                favorecidoDocumento: request.FavorecidoDocumento);
        }

        liquidacao.AdicionarRetencao(retencao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return retencao.Id.Value;
    }
}
