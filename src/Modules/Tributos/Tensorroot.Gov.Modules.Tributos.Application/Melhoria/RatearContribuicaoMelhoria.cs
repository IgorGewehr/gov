using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Melhoria;

/// <summary>Lançamento gerado por imóvel no rateio da Contribuição de Melhoria.</summary>
/// <param name="ImovelId">Imóvel beneficiado.</param>
/// <param name="LancamentoId">Lançamento gerado.</param>
/// <param name="DamId">DAM (guia) gerado.</param>
/// <param name="ContribuicaoRateada">Parcela rateada para o imóvel (R$).</param>
public sealed record LancamentoMelhoriaPorImovel(Guid ImovelId, Guid LancamentoId, Guid DamId, decimal ContribuicaoRateada);

/// <summary>Resultado do rateio da Contribuição de Melhoria.</summary>
/// <param name="ObraId">Obra rateada.</param>
/// <param name="TotalRateado">Valor total efetivamente rateado (R$).</param>
/// <param name="Lancamentos">Lançamentos gerados por imóvel.</param>
public sealed record ResultadoRateioMelhoria(Guid ObraId, decimal TotalRateado, IReadOnlyList<LancamentoMelhoriaPorImovel> Lancamentos);

/// <summary>
/// Rateia a Contribuição de Melhoria (CTN arts. 81–82) entre os imóveis beneficiados — proporcional à
/// valorização individual, respeitando os dois limites (total ≤ custo financiável; individual ≤
/// valorização) — e gera UM <see cref="Lancamento"/> <c>TipoTributo.ContribuicaoMelhoria</c> + guia (DAM)
/// por imóvel com parcela positiva. Só após o encerramento do prazo de impugnação. Nenhum valor é
/// hardcoded. Ver M6-DESIGN §3.5.
/// </summary>
/// <param name="ObraId">Obra de Contribuição de Melhoria.</param>
/// <param name="Vencimento">Vencimento das guias.</param>
/// <param name="NumeroParcelas">Número de parcelas por imóvel (1 = cota única).</param>
public sealed record RatearContribuicaoMelhoriaCommand(
    Guid ObraId,
    DateOnly Vencimento,
    int NumeroParcelas = 1) : ICommand<ResultadoRateioMelhoria>;

/// <summary>Regras de validação do rateio da Contribuição de Melhoria.</summary>
public sealed class RatearContribuicaoMelhoriaValidator : AbstractValidator<RatearContribuicaoMelhoriaCommand>
{
    /// <summary>Define as regras.</summary>
    public RatearContribuicaoMelhoriaValidator()
    {
        RuleFor(c => c.ObraId).NotEmpty();
        RuleFor(c => c.NumeroParcelas).GreaterThanOrEqualTo(1);
    }
}

/// <summary>Handler do rateio da Contribuição de Melhoria.</summary>
public sealed class RatearContribuicaoMelhoriaHandler(
    IObraContribuicaoMelhoriaRepository obras,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<RatearContribuicaoMelhoriaCommand, ResultadoRateioMelhoria>
{
    /// <inheritdoc />
    public async Task<ResultadoRateioMelhoria> Handle(RatearContribuicaoMelhoriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraContribuicaoMelhoriaId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra de Contribuição de Melhoria não encontrada.");

        var totalRateado = obra.Ratear();

        // Competência usa o mês 1 do ano do vencimento (lançamento por obra, similar ao IPTU anual).
        var competencia = Competencia.De(request.Vencimento.Year, 1);

        // Fato gerador da Contribuição de Melhoria: valorização decorrente da obra, no exercício do
        // lançamento. Data da constituição = "hoje" administrativo (sem relógio no domínio —
        // CLAUDE.md §16). A decadência (CTN art. 173, I) é aferida no agregado.
        var dataFatoGerador = new DateOnly(competencia.Ano, 1, 1);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var lancamentosGerados = new List<LancamentoMelhoriaPorImovel>();
        foreach (var imovel in obra.Imoveis)
        {
            // Imóveis com parcela zero (valorização absorvida por outros / limite) não geram cobrança.
            if (imovel.ContribuicaoRateada.Valor <= 0m)
            {
                continue;
            }

            var lancamento = Lancamento.LancarComImovel(
                tenant.TenantId,
                imovel.ProprietarioId,
                TipoTributo.ContribuicaoMelhoria,
                competencia,
                imovel.ContribuicaoRateada,
                request.Vencimento,
                dataFatoGerador,
                hoje,
                imovel.ImovelId);

            var dam = Dam.Gerar(
                tenant.TenantId,
                lancamento.Id,
                imovel.ProprietarioId,
                imovel.ContribuicaoRateada,
                request.NumeroParcelas,
                request.Vencimento);

            lancamentos.Adicionar(lancamento);
            dams.Adicionar(dam);
            lancamentosGerados.Add(new LancamentoMelhoriaPorImovel(
                imovel.ImovelId.Value,
                lancamento.Id.Value,
                dam.Id.Value,
                imovel.ContribuicaoRateada.Valor));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoRateioMelhoria(obra.Id.Value, totalRateado.Valor, lancamentosGerados);
    }
}
