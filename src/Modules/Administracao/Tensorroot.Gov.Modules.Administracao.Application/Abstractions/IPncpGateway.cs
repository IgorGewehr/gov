namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer (saida) do PNCP — Portal Nacional de Contratacoes Publicas (Lei 14.133/2021,
/// art. 174; gestao pelo Dec. 10.764/2021). Encapsula a integracao com a API do PNCP (Manual de
/// Integracao v2.x): autenticacao JWT (token de vida curta, ~1h) e o fluxo de pre-cadastro
/// orgao -&gt; unidade -&gt; compra/edital -&gt; contrato -&gt; aditivo -&gt; arquivos. A implementacao de
/// producao e resiliente (Polly: timeout + retry + circuit breaker) e IDEMPOTENTE por chave de
/// transmissao (republicar o mesmo contrato nao duplica registro). Chamado pelo HANDLER DO OUTBOX
/// (drenagem transacional), nunca direto do request.
/// <para>
/// FRONTEIRA M9/M10: no M9 a transmissao real ao PNCP de producao NAO ocorre — a impl. SIMULADA devolve
/// um numero de controle deterministico para exercitar o pipeline ponta-a-ponta contra WireMock. A
/// transmissao real (base <c>pncp.gov.br</c>, credenciais/JWT, validacao em <c>treina.pncp.gov.br</c>)
/// e M10. // TODO(M10): trocar o simulado pelo cliente HTTP real + credenciais no Key Vault.
/// </para>
/// </summary>
public interface IPncpGateway
{
    /// <summary>
    /// Divulga (publica) um contrato administrativo no PNCP, executando o pre-cadastro necessario
    /// (orgao/unidade/compra) e retornando o NUMERO DE CONTROLE PNCP do contrato. Idempotente pela
    /// <see cref="PublicacaoContratoPncpRequest.ChaveIdempotencia"/>.
    /// </summary>
    /// <param name="request">Dados do contrato a divulgar (do agregado, ja resolvidos pelo handler).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o numero de controle PNCP (ou falha mapeada).</returns>
    Task<PublicacaoContratoPncpResultado> PublicarContratoAsync(
        PublicacaoContratoPncpRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Requisicao de divulgacao de contrato no PNCP (DTO da ACL — sem tipos do dominio cruzando a fronteira).
/// Reune o minimo do pre-cadastro orgao -&gt; unidade -&gt; compra -&gt; contrato.
/// </summary>
/// <param name="ContratoId">Identificador interno do contrato (rastreabilidade/idempotencia).</param>
/// <param name="CnpjOrgao">CNPJ do orgao/entidade (8 digitos de raiz no PNCP).</param>
/// <param name="CodigoUnidade">Codigo da unidade administrativa compradora.</param>
/// <param name="NumeroContratoInterno">Numero do contrato no ente (ex.: "0012/2026").</param>
/// <param name="Objeto">Objeto contratado.</param>
/// <param name="ValorGlobal">Valor global do contrato.</param>
/// <param name="DataAssinatura">Data de assinatura.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="DocumentoFornecedor">CPF/CNPJ do fornecedor contratado.</param>
/// <param name="ChaveIdempotencia">
/// Chave de idempotencia da transmissao (mesma chave =&gt; mesma operacao; o gateway nao duplica registro).
/// </param>
public sealed record PublicacaoContratoPncpRequest(
    Guid ContratoId,
    string CnpjOrgao,
    string CodigoUnidade,
    string NumeroContratoInterno,
    string Objeto,
    decimal ValorGlobal,
    DateOnly DataAssinatura,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    string DocumentoFornecedor,
    string ChaveIdempotencia);

/// <summary>Resultado da divulgacao de um contrato no PNCP.</summary>
/// <param name="Sucesso">Verdadeiro se o PNCP registrou o contrato.</param>
/// <param name="NumeroControlePncp">Numero de controle atribuido pelo PNCP (quando sucesso).</param>
/// <param name="CodigoErro">Codigo do erro mapeado pela ACL (quando falha).</param>
/// <param name="MensagemErro">Mensagem do erro (quando falha).</param>
public sealed record PublicacaoContratoPncpResultado(
    bool Sucesso,
    string? NumeroControlePncp,
    string? CodigoErro,
    string? MensagemErro)
{
    /// <summary>Fabrica de resultado de sucesso.</summary>
    /// <param name="numeroControlePncp">Numero de controle atribuido pelo PNCP.</param>
    /// <returns>Resultado de sucesso.</returns>
    public static PublicacaoContratoPncpResultado Ok(string numeroControlePncp)
        => new(true, numeroControlePncp, null, null);

    /// <summary>Fabrica de resultado de falha mapeada.</summary>
    /// <param name="codigo">Codigo do erro.</param>
    /// <param name="mensagem">Mensagem do erro.</param>
    /// <returns>Resultado de falha.</returns>
    public static PublicacaoContratoPncpResultado Falha(string codigo, string mensagem)
        => new(false, null, codigo, mensagem);
}
