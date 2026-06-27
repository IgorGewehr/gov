using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;

/// <summary>
/// Gera a MSC de Encerramento (anual, mês 13) — insumo do rascunho da DCA (LC 101/2000 art. 51).
/// Espelha <see cref="GerarMscCommand"/>: lê o balancete de encerramento (mês 13, com classes 3/4 e
/// 5/6 de execução já zeradas), monta a matriz validando ΣD=ΣC e publica o evento de integração via
/// Outbox. Idempotente por (Tenant, Exercicio, Mes=13, Tipo=Encerramento). DESIGN §6.
/// </summary>
/// <param name="Exercicio">Exercício encerrado.</param>
/// <param name="PoderOrgao">
/// Código Poder/Órgão (PO, 5 dígitos) do ente. Omitido ⇒ PO do Executivo (único emissor da MSC).
/// </param>
public sealed record GerarMscEncerramentoCommand(int Exercicio, string? PoderOrgao = null)
    : ICommand<GerarMscResultado>;

/// <summary>Validação da geração da MSC de encerramento.</summary>
public sealed class GerarMscEncerramentoValidator : AbstractValidator<GerarMscEncerramentoCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarMscEncerramentoValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.PoderOrgao)
            .Must(po => po is null || InformacoesComplementaresMsc.PoderOrgaoValido(po))
            .WithMessage("Poder/Orgao (PO) deve ter 5 digitos (2 poder + 3 orgao) — Regras Gerais MSC 2026, IC nº1.");
    }
}

/// <summary>
/// Handler da MSC de encerramento: agrega o balancete do mês 13 (apuração) sobre o saldo final de
/// dezembro. SICONFI valida saldo_inicial + movimento = saldo_final, o que recai no invariante
/// <c>MatrizSaldosContabeis.EstaBalanceada</c> já provado.
/// </summary>
public sealed class GerarMscEncerramentoHandler(
    IBalanceteProjection balancete,
    IMscGeradaStore mscStore,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider) : ICommandHandler<GerarMscEncerramentoCommand, GerarMscResultado>
{
    /// <summary>Mês lógico do encerramento (apuração patrimonial/orçamentária — DESIGN §2).</summary>
    public const int MesEncerramento = 13;

    private const int TipoMatrizEncerramento = (int)TipoMatrizMsc.Encerramento;

    /// <inheritdoc />
    public async Task<GerarMscResultado> Handle(GerarMscEncerramentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existente = await mscStore
            .ObterAsync(request.Exercicio, MesEncerramento, TipoMatrizEncerramento, cancellationToken)
            .ConfigureAwait(false);
        if (existente is not null)
        {
            return new GerarMscResultado(existente.EventId, existente.QuantidadeLinhas, JaExistia: true);
        }

        // Balancete de encerramento: o estado final após as apurações vive como saldo do mês 13 das
        // contas movimentadas na apuração e, para as contas não tocadas em 13, no saldo do mês 12.
        // Mesclamos por conta (mês 13 prevalece) para refletir o ending_balance pós-apuração.
        var dezembro = await balancete.ListarPorPeriodoAsync(request.Exercicio, 12, cancellationToken).ConfigureAwait(false);
        var apuracao = await balancete.ListarPorPeriodoAsync(request.Exercicio, MesEncerramento, cancellationToken).ConfigureAwait(false);

        var consolidado = MesclarBalancete(dezembro, apuracao);
        var linhasMsc = DerivadorMsc.Derivar(consolidado, request.PoderOrgao);

        var matriz = MatrizSaldosContabeis.Montar(
            tenant.TenantId,
            request.Exercicio,
            MesEncerramento,
            TipoMatrizMsc.Encerramento,
            linhasMsc);

        var eventId = Guid.NewGuid();
        var evento = new MSCGeradaIntegrationEvent(
            eventId,
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            matriz.Exercicio,
            matriz.Mes,
            (int)matriz.TipoMatriz,
            matriz.Linhas.Select(MscDtoMapper.Mapear).ToList());

        integrationEvents.Enfileirar(evento);

        mscStore.Adicionar(new MscGeradaRegistro
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Exercicio = matriz.Exercicio,
            Mes = matriz.Mes,
            TipoMatriz = (int)matriz.TipoMatriz,
            EventId = eventId,
            QuantidadeLinhas = matriz.Linhas.Count,
            GeradaEmUtc = timeProvider.GetUtcNow().UtcDateTime,
        });

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new GerarMscResultado(eventId, matriz.Linhas.Count, JaExistia: false);
    }

    /// <summary>
    /// Consolida o balancete de encerramento: para cada conta, a linha do mês 13 (apuração) prevalece
    /// sobre a de dezembro; contas não tocadas pela apuração mantêm o saldo de dezembro. O movimento
    /// (period_change) do encerramento corresponde aos débitos/créditos do mês 13.
    /// </summary>
    private static IReadOnlyList<LinhaBalancete> MesclarBalancete(
        IReadOnlyList<LinhaBalancete> dezembro,
        IReadOnlyList<LinhaBalancete> apuracao)
    {
        var porConta = new Dictionary<Guid, LinhaBalancete>();
        foreach (var linha in dezembro)
        {
            // Saldo inicial da MSC de encerramento = saldo final de dezembro; sem movimento por default.
            porConta[linha.ContaId] = new LinhaBalancete
            {
                TenantId = linha.TenantId,
                ContaId = linha.ContaId,
                CodigoConta = linha.CodigoConta,
                Titulo = linha.Titulo,
                NaturezaSaldo = linha.NaturezaSaldo,
                NaturezaInformacao = linha.NaturezaInformacao,
                Nivel = linha.Nivel,
                Exercicio = linha.Exercicio,
                PeriodoMes = MesEncerramento,
                SaldoAnterior = linha.SaldoAtual,
                TotalDebitos = 0m,
                TotalCreditos = 0m,
                SaldoAtual = linha.SaldoAtual,
            };
        }

        foreach (var linha in apuracao)
        {
            // A linha do mês 13 já tem SaldoAnterior = saldo de dezembro e movimento da apuração.
            porConta[linha.ContaId] = linha;
        }

        return [.. porConta.Values];
    }
}
