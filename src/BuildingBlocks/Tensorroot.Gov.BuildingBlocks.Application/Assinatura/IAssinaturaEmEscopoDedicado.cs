namespace Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

/// <summary>
/// PORTA para assinar com o A1 do tenant a partir de um handler de OUTRO modulo, executando a
/// assinatura num ESCOPO DE DI DEDICADO.
/// <para>
/// Motivacao (guarda H5 — <c>ScopeDbContextHolder</c>): chamar o <see cref="IServicoAssinaturaDigital"/>
/// (modulo Cofre) diretamente de um handler que ja resolveu o <c>ModuleDbContext</c> do seu proprio
/// modulo no mesmo escopo faz o <c>CofreDbContext</c> ser resolvido lado-a-lado — dois
/// <c>ModuleDbContext</c> distintos no mesmo escopo, o que a guarda H5 (corretamente) recusa para nao
/// descartar mutacoes silenciosamente. Esta porta isola a assinatura num escopo proprio (com o tenant
/// corrente reaplicado), onde o UNICO <c>ModuleDbContext</c> e o do Cofre — o mesmo padrao do
/// <c>ScopedOutboxMessageDispatcher</c>. Reusavel por AFD/AEJ (ponto), eSocial e remessas TCE.
/// </para>
/// </summary>
public interface IAssinaturaEmEscopoDedicado
{
    /// <summary>
    /// Assina um conteudo binario/.TXT em CMS/PKCS#7 (CAdES) com o A1 ativo do tenant corrente, num
    /// escopo de DI dedicado (sem colidir com o <c>ModuleDbContext</c> do modulo chamador).
    /// </summary>
    /// <param name="conteudo">Conteudo a assinar.</param>
    /// <param name="opcoes">Opcoes/destino da assinatura CMS.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O envelope CMS (DER) assinado.</returns>
    Task<byte[]> AssinarCmsAsync(
        ReadOnlyMemory<byte> conteudo,
        OpcoesAssinaturaCms opcoes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Assina um documento XML (XML-DSig) com o A1 ativo do tenant corrente, num escopo de DI dedicado
    /// (sem colidir com o <c>ModuleDbContext</c> do modulo chamador). Util para eSocial.
    /// </summary>
    /// <param name="xmlUtf8">Documento XML a assinar, em bytes (UTF-8).</param>
    /// <param name="opcoes">Opcoes/destino que selecionam o perfil criptografico.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O XML assinado e os metadados de auditoria (sem material sensivel).</returns>
    Task<ResultadoAssinatura> AssinarXmlAsync(
        ReadOnlyMemory<byte> xmlUtf8,
        OpcoesAssinaturaXml opcoes,
        CancellationToken cancellationToken);
}
