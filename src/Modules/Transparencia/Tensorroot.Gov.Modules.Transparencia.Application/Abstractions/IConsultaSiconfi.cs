namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Critério de consulta à API de Dados Abertos do SICONFI (STN). Mapeia os parâmetros confirmados no
/// Swagger (verificação SICONFI). É uma consulta de RECONCILIAÇÃO — NÃO há envio.
/// </summary>
/// <param name="IdEnte">Código IBGE do ente (id_ente).</param>
/// <param name="Exercicio">Ano de exercício (an_exercicio).</param>
/// <param name="TipoDemonstrativo">Demonstrativo a consultar (RREO/RGF/DCA/MSC).</param>
/// <param name="Periodo">Período do demonstrativo (bimestre/quadrimestre/competência), quando aplicável.</param>
/// <param name="CoPoder">Poder (E/L/J/M/D), quando aplicável.</param>
public sealed record ConsultaSiconfiCriterio(
    string IdEnte,
    int Exercicio,
    DemonstrativoSiconfi TipoDemonstrativo,
    int? Periodo = null,
    string? CoPoder = null);

/// <summary>Espécie de demonstrativo consultável na API de Dados Abertos do SICONFI.</summary>
public enum DemonstrativoSiconfi
{
    /// <summary>Relatório Resumido da Execução Orçamentária.</summary>
    Rreo = 1,

    /// <summary>Relatório de Gestão Fiscal.</summary>
    Rgf = 2,

    /// <summary>Declaração de Contas Anuais.</summary>
    Dca = 3,

    /// <summary>Matriz de Saldos Contábeis.</summary>
    Msc = 4,
}

/// <summary>
/// Um valor publicado pelo SICONFI para uma rubrica/conta, usado na reconciliação contra o valor calculado
/// localmente.
/// </summary>
/// <param name="Conta">Identificador da conta/rubrica (ex.: cód. PCASP ou rótulo do demonstrativo).</param>
/// <param name="Coluna">Coluna/atributo do demonstrativo (ex.: "Até o Bimestre").</param>
/// <param name="Valor">Valor publicado pelo SICONFI.</param>
public sealed record ValorPublicadoSiconfi(string Conta, string Coluna, decimal Valor);

/// <summary>
/// Status de uma entrega no extrato do SICONFI (<c>/extrato_entregas</c>), usado para auditoria do que já
/// foi homologado no portal (ato humano com e-CPF A3).
/// </summary>
/// <param name="TipoRelatorio">Tipo do relatório (RREO/RGF/DCA/MSC).</param>
/// <param name="Periodo">Período da entrega.</param>
/// <param name="Status">Status textual retornado pela API.</param>
/// <param name="DataStatus">Data do status, quando disponível.</param>
public sealed record EntregaSiconfi(string TipoRelatorio, string Periodo, string Status, DateOnly? DataStatus);

/// <summary>
/// Anti-Corruption Layer da API de Dados Abertos do SICONFI (somente CONSULTA — não há upload/envio).
/// <c>https://apidatalake.tesouro.gov.br/ords/siconfi/tt/</c> — JSON, sem auth, rate limit ~1 req/s,
/// paginação 5.000. Atrás de Polly (timeout/retry/circuit breaker) na Infrastructure. Usada para
/// RECONCILIAR os valores calculados localmente com o publicado e AUDITAR o status de entrega.
/// </summary>
public interface IConsultaSiconfi
{
    /// <summary>Consulta os valores publicados de um demonstrativo para reconciliação.</summary>
    /// <param name="criterio">Critério da consulta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Valores publicados (vazio se não houver).</returns>
    Task<IReadOnlyList<ValorPublicadoSiconfi>> ConsultarValoresAsync(
        ConsultaSiconfiCriterio criterio,
        CancellationToken cancellationToken);

    /// <summary>Consulta o extrato de entregas do ente (auditoria do status de homologação).</summary>
    /// <param name="idEnte">Código IBGE do ente.</param>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Entregas do exercício (vazio se não houver).</returns>
    Task<IReadOnlyList<EntregaSiconfi>> ConsultarEntregasAsync(
        string idEnte,
        int exercicio,
        CancellationToken cancellationToken);
}
