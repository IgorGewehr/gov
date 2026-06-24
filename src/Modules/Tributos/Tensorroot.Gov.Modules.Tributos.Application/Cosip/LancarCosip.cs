using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Tributos.Application.Cosip;

/// <summary>Resultado do lançamento próprio da COSIP.</summary>
/// <param name="LancamentoId">Lançamento gerado.</param>
/// <param name="DamId">DAM (guia) gerado.</param>
/// <param name="ValorCosip">Valor da COSIP apurado (R$).</param>
public sealed record ResultadoLancamentoCosip(Guid LancamentoId, Guid DamId, decimal ValorCosip);

/// <summary>
/// Apura e lança a COSIP por LANÇAMENTO PRÓPRIO (caminho para consumidores NÃO faturados pela
/// distribuidora — o caminho usual é a cobrança na fatura, modelado por convênio à parte). Apura o valor
/// pela <see cref="Domain.Cosip.TabelaCosip"/> vigente, a partir da classe e do consumo (kWh), constitui
/// o <see cref="Lancamento"/> <c>TipoTributo.Cosip</c> e gera a guia (DAM). Nenhum valor é hardcoded.
/// Ver M6-DESIGN §3.4.
/// </summary>
/// <param name="ContribuinteId">Contribuinte devedor (consumidor).</param>
/// <param name="Classe">Classe de consumidor.</param>
/// <param name="ConsumoKwh">Consumo medido (kWh).</param>
/// <param name="Ano">Ano da competência.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="Vencimento">Vencimento da guia.</param>
/// <param name="ImovelId">Imóvel vinculado (opcional).</param>
public sealed record LancarCosipCommand(
    Guid ContribuinteId,
    ClasseConsumidorCosip Classe,
    decimal ConsumoKwh,
    int Ano,
    int Mes,
    DateOnly Vencimento,
    Guid? ImovelId = null) : ICommand<ResultadoLancamentoCosip>;

/// <summary>Regras de validação do lançamento próprio da COSIP.</summary>
public sealed class LancarCosipValidator : AbstractValidator<LancarCosipCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarCosipValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.Classe).IsInEnum();
        RuleFor(c => c.ConsumoKwh).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.Ano).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
    }
}

/// <summary>Handler do lançamento próprio da COSIP.</summary>
public sealed class LancarCosipHandler(
    ITabelaCosipRepository tabelas,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<LancarCosipCommand, ResultadoLancamentoCosip>
{
    /// <inheritdoc />
    public async Task<ResultadoLancamentoCosip> Handle(LancarCosipCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = await tabelas.ObterVigenteAsync(request.Ano, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há tabela de COSIP vigente para o exercício {request.Ano}.");

        var valorCosip = tabela.Apurar(request.Classe, request.ConsumoKwh);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var imovelId = request.ImovelId is null ? (ImovelId?)null : new ImovelId(request.ImovelId.Value);

        // Fato gerador na competência informada; data da constituição = "hoje" administrativo (sem
        // relógio no domínio — CLAUDE.md §16). A decadência (CTN art. 173, I) é aferida no agregado.
        var dataFatoGerador = new DateOnly(request.Ano, request.Mes, DateTime.DaysInMonth(request.Ano, request.Mes));
        var hoje = dataHoje.Hoje();

        var lancamento = Lancamento.LancarComImovel(
            tenant.TenantId,
            contribuinteId,
            TipoTributo.Cosip,
            Competencia.De(request.Ano, request.Mes),
            valorCosip,
            request.Vencimento,
            dataFatoGerador,
            hoje,
            imovelId);

        var dam = Dam.Gerar(tenant.TenantId, lancamento.Id, contribuinteId, valorCosip, numeroParcelas: 1, request.Vencimento);

        lancamentos.Adicionar(lancamento);
        dams.Adicionar(dam);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoLancamentoCosip(lancamento.Id.Value, dam.Id.Value, valorCosip.Valor);
    }
}
