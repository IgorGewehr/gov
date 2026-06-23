using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

/// <summary>
/// Evento de integração público (ponte RH -&gt; Educação): expõe o TOTAL da <b>remuneração paga aos
/// profissionais da educação básica</b> num exercício, custeada com recursos do FUNDEB, para que a
/// Educação afira o <b>piso de 70%</b> (EC 108/2020; Lei 14.113/2020 art. 26) sem acessar o interno do RH.
/// Espelha o padrão da MSC (Finanças -&gt; Transparência) e da remessa de folha (RH -&gt; Transparência): o
/// consumidor (Educação) é CONSUMIDOR e recebe apenas este snapshot via Contracts. Idempotente por
/// <c>EventId</c>/exercício no consumidor.
/// <para>
/// O <b>rol de "profissionais da educação básica"</b> que compõe o numerador (divergência interpretativa
/// TCE/CNM) é resolvido no RH ao classificar a folha do magistério; o município pode, alternativamente,
/// informar o total como parâmetro até o cruzamento automático estar disponível.
/// // TODO(validar-oficial): rol exato dos profissionais (Lei 14.113/2020 + instrumento do CACS-FUNDEB).
/// </para>
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="Exercicio">Exercício de referência (ano) da apuração.</param>
/// <param name="RemuneracaoProfissionaisEducacao">
/// Total pago a profissionais da educação básica no exercício, custeado com FUNDEB (numerador dos 70%).
/// </param>
public sealed record RemuneracaoMagisterioApuradaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    int Exercicio,
    decimal RemuneracaoProfissionaisEducacao) : IntegrationEvent(EventId, OccurredOnUtc);
