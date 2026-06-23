using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Taxas;

/// <summary>Resultado do lançamento de uma taxa.</summary>
/// <param name="LancamentoId">Lançamento gerado.</param>
/// <param name="DamId">DAM (guia) gerado.</param>
/// <param name="ValorTaxa">Valor da taxa apurado (R$).</param>
public sealed record ResultadoLancamentoTaxa(Guid LancamentoId, Guid DamId, decimal ValorTaxa);

/// <summary>
/// Lança uma taxa (poder de polícia ou serviço) a um contribuinte, com vínculo OPCIONAL a um imóvel:
/// calcula o valor pela <see cref="Domain.Taxas.TabelaTaxa"/> vigente (código + exercício) a partir da
/// quantidade-base, constitui o <see cref="Lancamento"/> <c>TipoTributo.Taxa</c> e gera a guia (DAM).
/// Nenhum valor é hardcoded. Ver M6-DESIGN §3.2.
/// </summary>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
/// <param name="CodigoTaxa">Código da taxa no CTM.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
/// <param name="QuantidadeBase">Quantidade-base (m², unidades…); ignorada no modo ValorFixo.</param>
/// <param name="Vencimento">Vencimento da guia.</param>
/// <param name="ImovelId">Imóvel vinculado (opcional).</param>
/// <param name="NumeroParcelas">Número de parcelas (1 = cota única).</param>
public sealed record LancarTaxaCommand(
    Guid ContribuinteId,
    string CodigoTaxa,
    int Exercicio,
    decimal QuantidadeBase,
    DateOnly Vencimento,
    Guid? ImovelId = null,
    int NumeroParcelas = 1) : ICommand<ResultadoLancamentoTaxa>;

/// <summary>Regras de validação do lançamento de taxa.</summary>
public sealed class LancarTaxaValidator : AbstractValidator<LancarTaxaCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarTaxaValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.CodigoTaxa).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.QuantidadeBase).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.NumeroParcelas).GreaterThanOrEqualTo(1);
    }
}

/// <summary>Handler do lançamento de taxa.</summary>
public sealed class LancarTaxaHandler(
    ITabelaTaxaRepository tabelas,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<LancarTaxaCommand, ResultadoLancamentoTaxa>
{
    /// <inheritdoc />
    public async Task<ResultadoLancamentoTaxa> Handle(LancarTaxaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = await tabelas.ObterVigentePorCodigoAsync(request.CodigoTaxa, request.Exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há tabela de taxa vigente para o código {request.CodigoTaxa} no exercício {request.Exercicio}.");

        var valorTaxa = tabela.Calcular(request.QuantidadeBase);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var imovelId = request.ImovelId is null ? (ImovelId?)null : new ImovelId(request.ImovelId.Value);

        var lancamento = Lancamento.LancarComImovel(
            tenant.TenantId,
            contribuinteId,
            TipoTributo.Taxa,
            Competencia.De(request.Exercicio, request.Vencimento.Month),
            valorTaxa,
            request.Vencimento,
            imovelId);

        var dam = Dam.Gerar(tenant.TenantId, lancamento.Id, contribuinteId, valorTaxa, request.NumeroParcelas, request.Vencimento);

        lancamentos.Adicionar(lancamento);
        dams.Adicionar(dam);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoLancamentoTaxa(lancamento.Id.Value, dam.Id.Value, valorTaxa.Valor);
    }
}
