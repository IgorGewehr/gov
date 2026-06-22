namespace Tensorroot.Gov.Modules.Cofre.Domain;

/// <summary>
/// Bloco AES-256-GCM (AEAD) de um segredo cifrado: texto cifrado + nonce (12 bytes, UNICO por
/// cifragem) + tag de autenticacao (16 bytes). A tag amarra a integridade: adulteracao no banco
/// faz a decifragem FALHAR (A1-DESIGN §7, risco 7). O material em claro NUNCA vive aqui.
/// </summary>
/// <param name="Cipher">Texto cifrado (AES-256-GCM).</param>
/// <param name="Nonce">Nonce GCM de 12 bytes, unico por operacao de cifragem.</param>
/// <param name="Tag">Tag de autenticacao GCM de 16 bytes.</param>
public sealed record MaterialCifrado(byte[] Cipher, byte[] Nonce, byte[] Tag)
{
    /// <summary>Tamanho do nonce GCM em bytes (recomendado pela especificacao).</summary>
    public const int TamanhoNonce = 12;

    /// <summary>Tamanho da tag de autenticacao GCM em bytes.</summary>
    public const int TamanhoTag = 16;
}
