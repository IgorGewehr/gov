using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Alvaras;

/// <summary>Resultado da renovação de um alvará com a TLL de renovação.</summary>
/// <param name="AlvaraId">Alvará renovado.</param>
/// <param name="LancamentoTllId">Lançamento da TLL de renovação gerado.</param>
/// <param name="DamId">DAM (guia) da TLL gerado.</param>
/// <param name="ValorTll">Valor da TLL de renovação (R$).</param>
public sealed record ResultadoRenovacaoAlvara(Guid AlvaraId, Guid LancamentoTllId, Guid DamId, decimal ValorTll);

/// <summary>
/// Renova um alvará por novo período e lança a Taxa de Licença (TLL) de renovação anual a partir da
/// <see cref="Domain.Taxas.TabelaTaxa"/> vigente. Renovação típica no RS (M6-DESIGN §3.3). Nenhum valor
/// é hardcoded.
/// </summary>
/// <param name="AlvaraId">Alvará a renovar.</param>
/// <param name="NovoInicioVigencia">Início da nova vigência.</param>
/// <param name="NovoFimVigencia">Fim da nova vigência.</param>
/// <param name="CodigoTaxaTll">Código da taxa de licença (TLL) no CTM.</param>
/// <param name="Exercicio">Exercício fiscal da TLL.</param>
/// <param name="QuantidadeBaseTll">Quantidade-base da TLL; ignorada no modo ValorFixo.</param>
/// <param name="VencimentoTll">Vencimento da guia da TLL.</param>
public sealed record RenovarAlvaraCommand(
    Guid AlvaraId,
    DateOnly NovoInicioVigencia,
    DateOnly NovoFimVigencia,
    string CodigoTaxaTll,
    int Exercicio,
    decimal QuantidadeBaseTll,
    DateOnly VencimentoTll) : ICommand<ResultadoRenovacaoAlvara>;

/// <summary>Regras de validação da renovação de alvará + TLL.</summary>
public sealed class RenovarAlvaraValidator : AbstractValidator<RenovarAlvaraCommand>
{
    /// <summary>Define as regras.</summary>
    public RenovarAlvaraValidator()
    {
        RuleFor(c => c.AlvaraId).NotEmpty();
        RuleFor(c => c.NovoFimVigencia).GreaterThanOrEqualTo(c => c.NovoInicioVigencia);
        RuleFor(c => c.CodigoTaxaTll).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.QuantidadeBaseTll).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler da renovação de alvará + TLL.</summary>
public sealed class RenovarAlvaraHandler(
    IAlvaraRepository alvaras,
    ITabelaTaxaRepository tabelas,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<RenovarAlvaraCommand, ResultadoRenovacaoAlvara>
{
    /// <inheritdoc />
    public async Task<ResultadoRenovacaoAlvara> Handle(RenovarAlvaraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var alvara = await alvaras.ObterPorIdAsync(new AlvaraId(request.AlvaraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Alvará não encontrado.");

        var tabela = await tabelas.ObterVigentePorCodigoAsync(request.CodigoTaxaTll, request.Exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há tabela de TLL vigente para o código {request.CodigoTaxaTll} no exercício {request.Exercicio}.");

        alvara.Renovar(request.NovoInicioVigencia, request.NovoFimVigencia);

        var valorTll = tabela.Calcular(request.QuantidadeBaseTll);

        // Fato gerador da TLL no exercício informado; data da constituição = "hoje" administrativo
        // (sem relógio no domínio — CLAUDE.md §16). A decadência (CTN art. 173, I) é aferida no agregado.
        var dataFatoGerador = new DateOnly(request.Exercicio, request.VencimentoTll.Month, 1);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var lancamento = Lancamento.LancarComImovel(
            tenant.TenantId,
            alvara.ContribuinteId,
            TipoTributo.Taxa,
            Competencia.De(request.Exercicio, request.VencimentoTll.Month),
            valorTll,
            request.VencimentoTll,
            dataFatoGerador,
            hoje,
            alvara.ImovelId);

        var dam = Dam.Gerar(tenant.TenantId, lancamento.Id, alvara.ContribuinteId, valorTll, numeroParcelas: 1, request.VencimentoTll);

        lancamentos.Adicionar(lancamento);
        dams.Adicionar(dam);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoRenovacaoAlvara(alvara.Id.Value, lancamento.Id.Value, dam.Id.Value, valorTll.Valor);
    }
}
