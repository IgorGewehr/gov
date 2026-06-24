namespace Tensorroot.Gov.SharedKernel.Tempo;

/// <summary>
/// Porta de BORDA que devolve a DATA de "hoje" no FUSO HORARIO do tenant (default
/// <c>America/Sao_Paulo</c>, UTC-3, parametrizavel por tenant). Resolve o WARN sistemico de fuso: o
/// "hoje" calculado direto do <c>TimeProvider.GetUtcNow().UtcDateTime</c> vira o dia SEGUINTE perto da
/// meia-noite num municipio BR (UTC-3), errando competencia de folha, prazos legais (decadencia/
/// prescricao/PNCP/convenios) e data de lancamento.
/// </summary>
/// <remarks>
/// <para>
/// CONTRATO (CLAUDE.md S7/S16 — reprodutibilidade): o DOMINIO nao le relogio. O "hoje" entra pela
/// BORDA/Application (handlers, workers, varreduras de prazo) e e' PASSADO para as transicoes do
/// agregado e calculos de prazo como <see cref="DateOnly"/>. Esta porta e' o UNICO ponto autorizado a
/// derivar a data civil do tenant a partir do <c>TimeProvider</c>.
/// </para>
/// <para>
/// ESCOPO: apenas o "hoje" de DOMINIO (competencia, prazo legal, data de lancamento). Timestamps de
/// AUDITORIA, hash-chain (trilha imutavel) e Outbox seguem UTC (instante absoluto, ordenavel
/// globalmente) e NAO passam por aqui — sao do <c>TimeProvider.GetUtcNow()</c> diretamente.
/// </para>
/// <para>
/// Evolucao natural (sem alterar o contrato): a config do fuso por tenant migra para a tabela de
/// parametros do tenant. O tenant e' resolvido na Infra (mesma mecanica de <c>IFeriadosTenantProvider</c>).
/// </para>
/// </remarks>
public interface IDataHojeTenant
{
    /// <summary>
    /// Data civil de HOJE no fuso do tenant (default <c>America/Sao_Paulo</c>), derivada do instante
    /// UTC corrente do <c>TimeProvider</c>. Use SEMPRE este metodo no lugar de
    /// <c>DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)</c> para datas de DOMINIO.
    /// </summary>
    /// <returns>A data de hoje no fuso do tenant.</returns>
    DateOnly Hoje();

    /// <summary>
    /// Instante "agora" no fuso do tenant como <see cref="DateTimeOffset"/> (offset do tenant aplicado),
    /// para os raros casos que precisam da HORA local do tenant (ex.: janela de coleta de ponto). Para
    /// timestamp de auditoria/Outbox use o UTC do <c>TimeProvider</c>, NAO este metodo.
    /// </summary>
    /// <returns>O instante corrente no fuso do tenant.</returns>
    DateTimeOffset Agora();
}
