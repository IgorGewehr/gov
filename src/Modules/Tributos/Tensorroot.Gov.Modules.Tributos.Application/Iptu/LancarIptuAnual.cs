using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;

namespace Tensorroot.Gov.Modules.Tributos.Application.Iptu;

/// <summary>Resultado do lançamento anual de IPTU.</summary>
/// <param name="LancamentoId">Lançamento gerado.</param>
/// <param name="DamId">DAM (guia/carnê) gerado.</param>
/// <param name="ImpostoDevido">IPTU devido lançado (R$).</param>
public sealed record ResultadoLancamentoIptu(Guid LancamentoId, Guid DamId, decimal ImpostoDevido);

/// <summary>
/// Lança o IPTU anual de ofício de um imóvel (CTN art. 142/149): apura o imposto pela PGV/alíquota
/// vigentes, constitui o <see cref="Lancamento"/> de IPTU vinculado ao imóvel e gera o DAM com a
/// cota única ou N parcelas. O nº de parcelas/vencimento vem do calendário fiscal municipal
/// (parametrizado no comando). Ver M6-DESIGN §1.3/§5.
/// </summary>
/// <param name="ImovelId">Imóvel a tributar.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
/// <param name="PrimeiroVencimento">Vencimento da cota única / 1ª parcela.</param>
/// <param name="NumeroParcelas">Número de parcelas (1 = cota única).</param>
/// <param name="PercentualIsencao">Percentual de isenção (lei municipal); padrão 0.</param>
/// <param name="PercentualDesconto">Percentual de desconto (lei municipal); padrão 0.</param>
public sealed record LancarIptuAnualCommand(
    Guid ImovelId,
    int Exercicio,
    DateOnly PrimeiroVencimento,
    int NumeroParcelas = 1,
    decimal PercentualIsencao = 0m,
    decimal PercentualDesconto = 0m) : ICommand<ResultadoLancamentoIptu>;

/// <summary>Regras de validação do lançamento anual de IPTU.</summary>
public sealed class LancarIptuAnualValidator : AbstractValidator<LancarIptuAnualCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarIptuAnualValidator()
    {
        RuleFor(comando => comando.ImovelId).NotEmpty();
        RuleFor(comando => comando.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(comando => comando.NumeroParcelas).GreaterThanOrEqualTo(1);
        RuleFor(comando => comando.PercentualIsencao).InclusiveBetween(0m, 100m);
        RuleFor(comando => comando.PercentualDesconto).InclusiveBetween(0m, 100m);
    }
}

/// <summary>Handler do lançamento anual de IPTU.</summary>
public sealed class LancarIptuAnualHandler(
    IImovelRepository imoveis,
    IPlantaValoresRepository plantas,
    ITabelaAliquotaIptuRepository tabelas,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<LancarIptuAnualCommand, ResultadoLancamentoIptu>
{
    /// <inheritdoc />
    public async Task<ResultadoLancamentoIptu> Handle(LancarIptuAnualCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var imovelId = new ImovelId(request.ImovelId);
        var imovel = await imoveis.ObterPorIdAsync(imovelId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        var memoria = await ApuradorIptu.ApurarAsync(
            imovelId,
            request.Exercicio,
            new ParametrosIsencaoIptu(request.PercentualIsencao, request.PercentualDesconto),
            imoveis,
            plantas,
            tabelas,
            cancellationToken).ConfigureAwait(false);

        var lancamento = Lancamento.LancarIptu(
            tenant.TenantId,
            imovel.ProprietarioId,
            imovelId,
            request.Exercicio,
            memoria.ImpostoDevido,
            request.PrimeiroVencimento);

        var dam = Dam.Gerar(
            tenant.TenantId,
            lancamento.Id,
            imovel.ProprietarioId,
            memoria.ImpostoDevido,
            request.NumeroParcelas,
            request.PrimeiroVencimento);

        lancamentos.Adicionar(lancamento);
        dams.Adicionar(dam);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoLancamentoIptu(lancamento.Id.Value, dam.Id.Value, memoria.ImpostoDevido.Valor);
    }
}
