using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Contracts;

/// <summary>
/// Resultado da apuração de UM mínimo constitucional setorial num exercício (snapshot para consumo
/// cross-module). Strings/decimais para não vazar os enums do domínio fiscal da Transparencia.
/// </summary>
/// <param name="Setor">Setor apurado (<c>Saude</c>/<c>Educacao</c>).</param>
/// <param name="ReceitaBase">Receita-base (impostos + transferências constitucionais).</param>
/// <param name="Aplicado">Valor aplicado computável no setor.</param>
/// <param name="PercentualAplicado">Percentual aplicado (0..1).</param>
/// <param name="PercentualMinimo">Percentual mínimo (limite) vigente (0..1).</param>
/// <param name="Situacao">Situação apurada (<c>Atingido</c>/<c>NaoAtingido</c>).</param>
public sealed record MinimoSetorialApuradoDto(
    string Setor,
    decimal ReceitaBase,
    decimal Aplicado,
    decimal PercentualAplicado,
    decimal PercentualMinimo,
    string Situacao);

/// <summary>
/// Evento de integração público: a Transparencia apurou os <b>mínimos constitucionais</b> de um exercício
/// (Saúde 15% ASPS — LC 141/2012; Educação 25% MDE — CF art. 212) e publica o resultado para que o
/// Painel do Gestor (BI) exiba a situação (semáforo) sem reapurar nem acessar o interno da Transparencia.
/// Idempotente por <c>(TenantId, Exercicio)</c> no consumidor — a apuração mais recente do exercício
/// substitui a anterior. Reprodutível (a apuração de origem não usa relógio — ancora no exercício).
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="Exercicio">Exercício (ano) apurado.</param>
/// <param name="Setores">Resultado por setor (Saúde/Educação).</param>
public sealed record MinimoConstitucionalApuradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    int Exercicio,
    IReadOnlyList<MinimoSetorialApuradoDto> Setores) : IntegrationEvent(EventId, OccurredOnUtc);
