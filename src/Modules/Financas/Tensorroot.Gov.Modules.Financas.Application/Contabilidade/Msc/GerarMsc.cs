using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;

/// <summary>Resultado da geração da MSC de uma competência.</summary>
/// <param name="EventId">Identificador do evento publicado (ou do registro existente, se idempotente).</param>
/// <param name="QuantidadeLinhas">Quantidade de linhas da MSC.</param>
/// <param name="JaExistia">Indica se a MSC já havia sido gerada (idempotência).</param>
/// <param name="ContasRestosAPagarAusentes">
/// Na MSC de dezembro: prefixos das contas de Restos a Pagar (Regras Gerais MSC 2026 p.13) ausentes —
/// alerta de conformidade não bloqueante. Vazia fora de dezembro ou se todas presentes.
/// </param>
public sealed record GerarMscResultado(
    Guid EventId,
    int QuantidadeLinhas,
    bool JaExistia,
    IReadOnlyList<string>? ContasRestosAPagarAusentes = null);

/// <summary>
/// Gera a Matriz de Saldos Contábeis (Agregada) de uma competência a partir do balancete e publica o
/// <see cref="MSCGeradaIntegrationEvent"/> via Outbox. Idempotente por competência (RBAC
/// <c>financas.gerenciar</c>).
/// </summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês da competência (1-12).</param>
/// <param name="PoderOrgao">
/// Código Poder/Órgão (PO, 5 dígitos = 2 poder + 3 órgão) do ente. A MSC é enviada SOMENTE pelo Executivo
/// (Regras Gerais MSC 2026): quando omitido, assume o PO do Executivo (<see cref="PoderOrgaoMsc.Executivo"/>).
/// </param>
public sealed record GerarMscCommand(int Exercicio, int Mes, string? PoderOrgao = null)
    : ICommand<GerarMscResultado>;

/// <summary>Validação da geração da MSC.</summary>
public sealed class GerarMscValidator : AbstractValidator<GerarMscCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarMscValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
        RuleFor(c => c.PoderOrgao)
            .Must(po => po is null || InformacoesComplementaresMsc.PoderOrgaoValido(po))
            .WithMessage("Poder/Orgao (PO) deve ter 5 digitos (2 poder + 3 orgao) — Regras Gerais MSC 2026, IC nº1.");
    }
}

/// <summary>
/// Handler da geração da MSC: lê o balancete da competência, deriva as linhas, monta a matriz (validando
/// o fechamento Σ saldo final D = Σ C), enfileira o evento de integração no Outbox e grava o registro de
/// idempotência — tudo confirmado numa única unidade de trabalho.
/// </summary>
public sealed class GerarMscHandler(
    IBalanceteProjection balancete,
    IMscGeradaStore mscStore,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider) : ICommandHandler<GerarMscCommand, GerarMscResultado>
{
    private const int TipoMatrizAgregada = (int)TipoMatrizMsc.Agregada;

    /// <inheritdoc />
    public async Task<GerarMscResultado> Handle(GerarMscCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Idempotência por (TenantId, Exercicio, Mes, TipoMatriz): não republica.
        var existente = await mscStore
            .ObterAsync(request.Exercicio, request.Mes, TipoMatrizAgregada, cancellationToken)
            .ConfigureAwait(false);
        if (existente is not null)
        {
            return new GerarMscResultado(existente.EventId, existente.QuantidadeLinhas, JaExistia: true);
        }

        var linhasBalancete = await balancete
            .ListarPorPeriodoAsync(request.Exercicio, request.Mes, cancellationToken)
            .ConfigureAwait(false);

        var linhasMsc = DerivadorMsc.Derivar(linhasBalancete, request.PoderOrgao);

        // Monta a matriz — valida o fechamento (lança MatrizSaldosDesbalanceadaException se quebrado).
        var matriz = MatrizSaldosContabeis.Montar(
            tenant.TenantId,
            request.Exercicio,
            request.Mes,
            TipoMatrizMsc.Agregada,
            linhasMsc);

        // Alerta de conformidade (nao bloqueante): na MSC de dezembro, conferir a presenca das contas de
        // inscricao de Restos a Pagar (Regras Gerais MSC 2026 p.13).
        var restosAusentes = matriz.ContasRestosAPagarAusentes();

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

        return new GerarMscResultado(eventId, matriz.Linhas.Count, JaExistia: false, restosAusentes);
    }
}

/// <summary>Mapeia a linha de domínio da MSC para o DTO do contrato de integração.</summary>
internal static class MscDtoMapper
{
    public static LinhaMscDto Mapear(LinhaMsc linha) => new(
        linha.ContaPcasp,
        (int)linha.NaturezaSaldo,
        (int)linha.TipoValor,
        linha.Valor,
        linha.Complementares.ParaTexto(),
        new InformacoesComplementaresMscDto(
            linha.Complementares.PoderOrgao,
            linha.Complementares.AtributoSuperavitFinanceiro,
            linha.Complementares.DividaConsolidada,
            linha.Complementares.FonteRecurso,
            linha.Complementares.CodigoAcompanhamento,
            linha.Complementares.NaturezaReceita,
            linha.Complementares.NaturezaDespesa,
            linha.Complementares.FuncaoSubfuncao,
            linha.Complementares.AnoInscricaoRp));
}
