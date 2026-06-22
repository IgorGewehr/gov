using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Itbi;

/// <summary>Resultado do lançamento do ITBI (guia avulsa por transmissão).</summary>
/// <param name="TransmissaoId">Transmissão imobiliária registrada.</param>
/// <param name="LancamentoId">Lançamento do ITBI gerado.</param>
/// <param name="DamId">DAM (guia avulsa) gerado.</param>
/// <param name="BaseCalculo">Base adotada (R$).</param>
/// <param name="BaseFoiValorVenal">Verdadeiro se a base foi o valor venal.</param>
/// <param name="ImpostoDevido">ITBI devido (R$).</param>
public sealed record ResultadoLancamentoItbi(
    Guid TransmissaoId,
    Guid LancamentoId,
    Guid DamId,
    decimal BaseCalculo,
    bool BaseFoiValorVenal,
    decimal ImpostoDevido);

/// <summary>
/// Registra uma transmissão imobiliária e lança o ITBI: apura a base (maior entre valor venal de
/// referência e valor declarado), constitui o <see cref="Lancamento"/> de ITBI contra o adquirente e
/// gera a guia avulsa (DAM, 1 parcela). Vincula imóvel + transmitente + adquirente. Ver M6-DESIGN §3.1.
/// </summary>
/// <param name="ImovelId">Imóvel transmitido.</param>
/// <param name="TransmitenteId">Contribuinte transmitente.</param>
/// <param name="AdquirenteId">Contribuinte adquirente (sujeito passivo usual).</param>
/// <param name="Exercicio">Exercício do fato gerador.</param>
/// <param name="ValorDeclarado">Valor declarado da transação (R$).</param>
/// <param name="Vencimento">Vencimento da guia.</param>
/// <param name="PercentualIsencao">Percentual de isenção (lei municipal); padrão 0.</param>
/// <param name="UsarAliquotaSfh">Usa a alíquota reduzida do SFH; padrão falso.</param>
public sealed record LancarItbiCommand(
    Guid ImovelId,
    Guid TransmitenteId,
    Guid AdquirenteId,
    int Exercicio,
    decimal ValorDeclarado,
    DateOnly Vencimento,
    decimal PercentualIsencao = 0m,
    bool UsarAliquotaSfh = false) : ICommand<ResultadoLancamentoItbi>;

/// <summary>Regras de validação do lançamento do ITBI.</summary>
public sealed class LancarItbiValidator : AbstractValidator<LancarItbiCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarItbiValidator()
    {
        RuleFor(c => c.ImovelId).NotEmpty();
        RuleFor(c => c.TransmitenteId).NotEmpty();
        RuleFor(c => c.AdquirenteId).NotEmpty();
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.ValorDeclarado).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.PercentualIsencao).InclusiveBetween(0m, 100m);
        RuleFor(c => c.AdquirenteId).NotEqual(c => c.TransmitenteId)
            .WithMessage("O transmitente e o adquirente não podem ser o mesmo contribuinte.");
    }
}

/// <summary>Handler do lançamento do ITBI.</summary>
public sealed class LancarItbiHandler(
    IImovelRepository imoveis,
    IPlantaValoresRepository plantas,
    IAliquotaItbiRepository aliquotas,
    ITransmissaoImobiliariaRepository transmissoes,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<LancarItbiCommand, ResultadoLancamentoItbi>
{
    /// <inheritdoc />
    public async Task<ResultadoLancamentoItbi> Handle(LancarItbiCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var imovelId = new ImovelId(request.ImovelId);
        var transmitenteId = new ContribuinteId(request.TransmitenteId);
        var adquirenteId = new ContribuinteId(request.AdquirenteId);
        var valorDeclarado = ValorMonetario.De(request.ValorDeclarado);

        var memoria = await ApuradorItbi.ApurarAsync(
            imovelId,
            request.Exercicio,
            valorDeclarado,
            new ParametrosItbi(request.PercentualIsencao, request.UsarAliquotaSfh),
            imoveis,
            plantas,
            aliquotas,
            cancellationToken).ConfigureAwait(false);

        var transmissao = TransmissaoImobiliaria.Registrar(
            tenant.TenantId,
            imovelId,
            transmitenteId,
            adquirenteId,
            request.Exercicio,
            valorDeclarado,
            memoria);

        // Contribuinte do ITBI = adquirente (CTN art. 42; usual). Lançamento avulso por transação.
        var lancamento = Lancamento.Lancar(
            tenant.TenantId,
            adquirenteId,
            TipoTributo.Itbi,
            Competencia.De(request.Exercicio, request.Vencimento.Month),
            memoria.ImpostoDevido,
            request.Vencimento);

        // Guia avulsa: cota única (1 parcela).
        var dam = Dam.Gerar(tenant.TenantId, lancamento.Id, adquirenteId, memoria.ImpostoDevido, numeroParcelas: 1, request.Vencimento);

        transmissoes.Adicionar(transmissao);
        lancamentos.Adicionar(lancamento);
        dams.Adicionar(dam);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoLancamentoItbi(
            transmissao.Id.Value,
            lancamento.Id.Value,
            dam.Id.Value,
            memoria.BaseCalculo.Valor,
            memoria.BaseFoiValorVenal,
            memoria.ImpostoDevido.Valor);
    }
}
