using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Tributos.Application.Itbi;

/// <summary>Resultado do lançamento do ITBI (guia avulsa por transmissão).</summary>
/// <param name="TransmissaoId">Transmissão imobiliária registrada.</param>
/// <param name="LancamentoId">Lançamento do ITBI gerado.</param>
/// <param name="DamId">DAM (guia avulsa) gerado.</param>
/// <param name="BaseCalculo">Base adotada — valor declarado (R$).</param>
/// <param name="Origem">Origem da base (Declarada no lançamento inicial).</param>
/// <param name="HaDivergenciaReferencia">Verdadeiro se a triagem sinalizou divergência relevante (não altera a base).</param>
/// <param name="ImpostoDevido">ITBI devido (R$).</param>
public sealed record ResultadoLancamentoItbi(
    Guid TransmissaoId,
    Guid LancamentoId,
    Guid DamId,
    decimal BaseCalculo,
    OrigemBaseCalculoItbi Origem,
    bool HaDivergenciaReferencia,
    decimal ImpostoDevido);

/// <summary>
/// Registra uma transmissão imobiliária e lança o ITBI: base = VALOR DECLARADO (Tema 1.113/STJ —
/// presunção de veracidade), constitui o <see cref="Lancamento"/> de ITBI contra o adquirente e gera a
/// guia avulsa (DAM, 1 parcela). O valor venal de referência só dispara a triagem (não eleva a base).
/// Vincula imóvel + transmitente + adquirente. Ver M6-DESIGN §3.1.
/// </summary>
/// <param name="ImovelId">Imóvel transmitido.</param>
/// <param name="TransmitenteId">Contribuinte transmitente.</param>
/// <param name="AdquirenteId">Contribuinte adquirente (sujeito passivo usual).</param>
/// <param name="Exercicio">Exercício do fato gerador.</param>
/// <param name="ValorDeclarado">Valor declarado da transação (R$).</param>
/// <param name="Vencimento">Vencimento da guia.</param>
/// <param name="PercentualIsencao">Percentual de isenção (lei municipal); padrão 0.</param>
/// <param name="UsarAliquotaSfh">Usa a alíquota reduzida do SFH; padrão falso.</param>
/// <param name="MargemDivergenciaPercentual">Margem de tolerância da triagem em % (parametrizável por tenant); padrão 0.</param>
public sealed record LancarItbiCommand(
    Guid ImovelId,
    Guid TransmitenteId,
    Guid AdquirenteId,
    int Exercicio,
    decimal ValorDeclarado,
    DateOnly Vencimento,
    decimal PercentualIsencao = 0m,
    bool UsarAliquotaSfh = false,
    decimal MargemDivergenciaPercentual = 0m) : ICommand<ResultadoLancamentoItbi>;

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
        // IT-B4 — O ITBI incide sobre transmissao ONEROSA (CTN art. 35). Valor declarado zero nao e
        // transmissao onerosa: e doacao (campo do ITCMD estadual) ou indicio de fraude. Exigir > 0
        // impede a emissao de guia de ITBI R$ 0,00.
        RuleFor(c => c.ValorDeclarado).GreaterThan(0m)
            .WithMessage("O valor declarado deve ser maior que zero: o ITBI incide sobre transmissao onerosa (CTN art. 35).");
        RuleFor(c => c.PercentualIsencao).InclusiveBetween(0m, 100m);
        RuleFor(c => c.MargemDivergenciaPercentual).InclusiveBetween(0m, 100m);
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
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
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
            request.MargemDivergenciaPercentual,
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

        // Triagem (Tema 1.113/STJ): a guia segue SEMPRE pela base declarada. Se houver divergência
        // relevante, apenas sinalizamos a fila de revisão fiscal — NUNCA elevamos a base de ofício.
        if (memoria.HaDivergenciaReferencia)
        {
            transmissao.SinalizarDivergenciaTriagem(memoria.ValorVenalReferencia);
        }

        // Fato gerador do ITBI: a transmissão onerosa no exercício informado (CTN art. 35). Data da
        // constituição = "hoje" administrativo (sem relógio no domínio — CLAUDE.md §16). A decadência
        // (CTN art. 173, I) é aferida no agregado a partir do exercício do fato gerador.
        var dataFatoGerador = new DateOnly(request.Exercicio, request.Vencimento.Month, 1);
        var hoje = dataHoje.Hoje();

        // Contribuinte do ITBI = adquirente (CTN art. 42; usual). Lançamento avulso por transação.
        var lancamento = Lancamento.Lancar(
            tenant.TenantId,
            adquirenteId,
            TipoTributo.Itbi,
            Competencia.De(request.Exercicio, request.Vencimento.Month),
            memoria.ImpostoDevido,
            request.Vencimento,
            dataFatoGerador,
            hoje);

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
            memoria.Origem,
            memoria.HaDivergenciaReferencia,
            memoria.ImpostoDevido.Valor);
    }
}
