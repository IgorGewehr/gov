namespace Tensorroot.Gov.Modules.Convenios.Application.Abstractions;

/// <summary>
/// Dados abertos do convenio no Transferegov.br (DTPAR): situacao/cadastro consultados na plataforma federal.
/// </summary>
/// <param name="NumeroConvenio">Numero do convenio no Transferegov.</param>
/// <param name="SituacaoTransferegov">Situacao cadastral na plataforma (texto).</param>
/// <param name="ConcedenteCnpj">CNPJ do concedente.</param>
/// <param name="ValorGlobal">Valor global registrado.</param>
public sealed record DadosConvenioTransferegov(
    string NumeroConvenio,
    string SituacaoTransferegov,
    string ConcedenteCnpj,
    decimal ValorGlobal);

/// <summary>
/// Anti-Corruption Layer (porta) do Transferegov.br (fluxo A). No M9 expoe a LEITURA de dados abertos DTPAR
/// (cadastro/situacao), atras de Polly. A TRANSMISSAO REAL da PC oficial (submissao a plataforma) e restrita
/// a orgaos integrados (creds/cert) e fica DEFERIDA ao M10 — ver <c>// TODO(M10)</c> na implementacao.
/// </summary>
public interface ITransferegovGateway
{
    /// <summary>
    /// Consulta os dados abertos (DTPAR) de um convenio pelo numero do Transferegov. Leitura idempotente,
    /// resiliente (Polly). No M9, a implementacao real e simulada/WireMock; producao defere M10.
    /// </summary>
    /// <param name="numeroConvenio">Numero do convenio no Transferegov.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados do convenio, ou nulo se nao encontrado.</returns>
    Task<DadosConvenioTransferegov?> ConsultarConvenioAsync(string numeroConvenio, CancellationToken cancellationToken);

    /// <summary>
    /// Transmite a prestacao de contas oficial ao Transferegov.br. // TODO(M10): integracao real (creds/cert
    /// + base de producao). No M9 a implementacao registra a intencao e devolve um protocolo simulado.
    /// </summary>
    /// <param name="numeroConvenio">Numero do convenio no Transferegov.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Protocolo (simulado) da transmissao.</returns>
    Task<string> TransmitirPrestacaoContasAsync(string numeroConvenio, CancellationToken cancellationToken);
}
