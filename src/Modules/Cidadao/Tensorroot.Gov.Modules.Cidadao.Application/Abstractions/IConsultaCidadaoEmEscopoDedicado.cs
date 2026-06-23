namespace Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;

/// <summary>
/// PORTA que executa uma CONSULTA de leitura cidada de OUTRO modulo (Tributos/Protocolo, via Contracts)
/// num ESCOPO DE DI DEDICADO.
/// <para>
/// Motivacao (guarda H5 — <c>ScopeDbContextHolder</c>): o handler do Portal do Cidadao ja resolveu o
/// <c>CidadaoDbContext</c> no escopo da requisicao (ancora dado-proprio: o resolvedor le a
/// <c>CidadaoConta</c> pelo <c>sub</c>, e a trilha de acesso LGPD e selada nesse mesmo contexto). Chamar
/// a consulta do modulo-fonte direto no mesmo escopo faria o <c>TributosDbContext</c>/<c>ProtocoloDbContext</c>
/// ser resolvido lado-a-lado — dois <c>ModuleDbContext</c> distintos no mesmo escopo, o que a guarda H5
/// (corretamente) recusa. Esta porta isola a consulta num escopo proprio (com o tenant corrente
/// reaplicado, preservando o banco dedicado e os Global Query Filters), onde o UNICO <c>ModuleDbContext</c>
/// e o do modulo-fonte. Mesmo padrao do <c>IAssinaturaEmEscopoDedicado</c> e do
/// <c>ScopedOutboxMessageDispatcher</c>.
/// </para>
/// <para>
/// SEGURANCA: o isolamento dado-proprio NAO e afetado — a pessoa (documento) ja foi resolvida server-side
/// no escopo da requisicao e e passada como ARGUMENTO ja resolvido; a consulta no escopo dedicado apenas
/// filtra por esse documento dentro do tenant. O tenant e reaplicado do principal (JWT), nunca de entrada.
/// </para>
/// </summary>
public interface IConsultaCidadaoEmEscopoDedicado
{
    /// <summary>
    /// Abre um escopo de DI dedicado (reaplicando o tenant corrente), resolve o servico
    /// <typeparamref name="TConsulta"/> do modulo-fonte ali e executa <paramref name="consulta"/>.
    /// </summary>
    /// <typeparam name="TConsulta">Porta de consulta do modulo-fonte (ex.: <c>IConsultaTributariaCidadao</c>).</typeparam>
    /// <typeparam name="TResultado">Tipo do resultado da consulta.</typeparam>
    /// <param name="consulta">Funcao que recebe a porta resolvida no escopo dedicado e produz o resultado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resultado da consulta.</returns>
    Task<TResultado> ExecutarAsync<TConsulta, TResultado>(
        Func<TConsulta, CancellationToken, Task<TResultado>> consulta,
        CancellationToken cancellationToken)
        where TConsulta : notnull;
}
