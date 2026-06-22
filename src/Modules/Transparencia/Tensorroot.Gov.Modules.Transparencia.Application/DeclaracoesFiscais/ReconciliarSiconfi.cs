using FluentValidation;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Uma divergência de reconciliação entre o valor calculado localmente e o publicado no SICONFI.</summary>
/// <param name="Conta">Conta/rubrica conferida.</param>
/// <param name="ValorLocal">Valor calculado localmente (matriz de saldos).</param>
/// <param name="ValorPublicado">Valor publicado pelo SICONFI (ou <c>null</c> se ausente).</param>
/// <param name="Diferenca">Diferença (local − publicado).</param>
public sealed record DivergenciaReconciliacao(string Conta, decimal ValorLocal, decimal? ValorPublicado, decimal Diferenca);

/// <summary>Resultado da reconciliação de uma declaração contra os dados publicados no SICONFI.</summary>
/// <param name="DeclaracaoFiscalId">Declaração reconciliada.</param>
/// <param name="Conciliado">Indica se não há divergências (false quando indisponível ou com divergências).</param>
/// <param name="Divergencias">Divergências apontadas (vazia ⇒ conciliado ou indisponível).</param>
/// <param name="Indisponivel">
/// Verdadeiro quando a API de Dados Abertos do SICONFI não respondeu (após Polly): a reconciliação
/// DEGRADA GRACIOSAMENTE — não é erro do ente nem do sistema; o operador deve reexecutar mais tarde.
/// </param>
/// <param name="Observacao">Mensagem de degradação (nula em execução normal).</param>
public sealed record ResultadoReconciliacao(
    Guid DeclaracaoFiscalId,
    bool Conciliado,
    IReadOnlyList<DivergenciaReconciliacao> Divergencias,
    bool Indisponivel = false,
    string? Observacao = null);

/// <summary>
/// Reconcilia os valores calculados localmente (MatrizSaldos da declaração) com o que o SICONFI publicou,
/// via a API de Dados Abertos (somente CONSULTA). NÃO é envio nem homologação.
/// </summary>
/// <param name="DeclaracaoFiscalId">Declaração a reconciliar.</param>
/// <param name="IdEnte">Código IBGE do ente para a consulta.</param>
public sealed record ReconciliarSiconfiCommand(Guid DeclaracaoFiscalId, string IdEnte) : ICommand<ResultadoReconciliacao>;

/// <summary>Regras de validação da reconciliação.</summary>
public sealed class ReconciliarSiconfiValidator : AbstractValidator<ReconciliarSiconfiCommand>
{
    /// <summary>Define as regras.</summary>
    public ReconciliarSiconfiValidator()
    {
        RuleFor(comando => comando.DeclaracaoFiscalId).NotEmpty();
        RuleFor(comando => comando.IdEnte).NotEmpty().MaximumLength(10);
    }
}

/// <summary>Handler da reconciliação contra o SICONFI (Dados Abertos).</summary>
public sealed class ReconciliarSiconfiHandler(
    IDeclaracaoFiscalRepository declaracoes,
    IConsultaSiconfi consulta,
    ILogger<ReconciliarSiconfiHandler> logger)
    : ICommandHandler<ReconciliarSiconfiCommand, ResultadoReconciliacao>
{
    /// <inheritdoc />
    public async Task<ResultadoReconciliacao> Handle(
        ReconciliarSiconfiCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var declaracao = await declaracoes
            .ObterPorIdAsync(new DeclaracaoFiscalId(request.DeclaracaoFiscalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Declaração fiscal não encontrada.");

        var criterio = new ConsultaSiconfiCriterio(
            request.IdEnte,
            declaracao.Exercicio,
            MapearDemonstrativo(declaracao.TipoDeclaracao),
            DerivarPeriodo(declaracao));

        // ACL + Polly: consulta de RECONCILIAÇÃO (Dados Abertos, 1 req/s) — nunca envio.
        // DEGRADAÇÃO GRACIOSA: se a API externa não responder mesmo após o Polly (timeout/retry/circuit
        // breaker exauridos), a reconciliação não é um erro do ente — retorna "indisponível" auditado,
        // sem vazar exceção/HTTP 500. O operador reexecuta mais tarde. [§4.3 — resiliência externa]
        IReadOnlyList<ValorPublicadoSiconfi> publicados;
        try
        {
            publicados = await consulta.ConsultarValoresAsync(criterio, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception excecao) when (excecao is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            logger.LogWarning(
                excecao,
                "Reconciliação SICONFI indisponível para a declaração {DeclaracaoId} (ente {IdEnte}); "
                + "degradando graciosamente — reexecutar quando a API de Dados Abertos responder.",
                declaracao.Id.Value,
                request.IdEnte);

            return new ResultadoReconciliacao(
                declaracao.Id.Value,
                Conciliado: false,
                Divergencias: [],
                Indisponivel: true,
                Observacao: "API de Dados Abertos do SICONFI indisponível; reconciliação não pôde ser executada agora.");
        }

        // Agrega o publicado por conta (somando colunas) e compara com o saldo local da conta.
        var publicadoPorConta = publicados
            .GroupBy(valor => valor.Conta, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(valor => valor.Valor), StringComparer.OrdinalIgnoreCase);

        var divergencias = new List<DivergenciaReconciliacao>();
        foreach (var linha in declaracao.Matriz.Linhas)
        {
            var valorLocal = linha.Valor.Valor;
            if (publicadoPorConta.TryGetValue(linha.ContaPcasp, out var valorPublicado))
            {
                if (valorLocal != valorPublicado)
                {
                    divergencias.Add(new DivergenciaReconciliacao(
                        linha.ContaPcasp, valorLocal, valorPublicado, valorLocal - valorPublicado));
                }
            }
            else
            {
                // Conta presente localmente, ausente no publicado ⇒ divergência (publicado nulo).
                divergencias.Add(new DivergenciaReconciliacao(linha.ContaPcasp, valorLocal, null, valorLocal));
            }
        }

        return new ResultadoReconciliacao(declaracao.Id.Value, divergencias.Count == 0, divergencias);
    }

    private static DemonstrativoSiconfi MapearDemonstrativo(TipoDeclaracaoFiscal tipo)
        => tipo switch
        {
            TipoDeclaracaoFiscal.Msc => DemonstrativoSiconfi.Msc,
            TipoDeclaracaoFiscal.Rreo => DemonstrativoSiconfi.Rreo,
            TipoDeclaracaoFiscal.Rgf => DemonstrativoSiconfi.Rgf,
            _ => DemonstrativoSiconfi.Dca,
        };

    private static int? DerivarPeriodo(DeclaracaoFiscal declaracao)
        => declaracao.TipoDeclaracao switch
        {
            TipoDeclaracaoFiscal.Msc => declaracao.Competencia?.Mes,
            TipoDeclaracaoFiscal.Rreo => declaracao.Bimestre?.Numero,
            TipoDeclaracaoFiscal.Rgf => declaracao.Quadrimestre?.Numero,
            _ => null,
        };
}
