namespace Tensorroot.Gov.Modules.Cofre.Domain;

/// <summary>
/// Erro de dominio do cofre: certificado expirado, serie incorreta, cadeia ICP-Brasil invalida,
/// CNPJ divergente do tenant ou tentativa de operacao em estado incompativel (A1-DESIGN §2/§5).
/// A mensagem NUNCA carrega material sensivel (.pfx/senha/chave/DEK).
/// </summary>
public sealed class CertificadoInvalidoException : Exception
{
    /// <summary>Cria a excecao com a mensagem informada.</summary>
    /// <param name="message">Motivo da invalidez (sem material sensivel).</param>
    public CertificadoInvalidoException(string message)
        : base(message)
    {
    }

    /// <summary>Cria a excecao com mensagem e causa.</summary>
    /// <param name="message">Motivo da invalidez.</param>
    /// <param name="innerException">Excecao subjacente.</param>
    public CertificadoInvalidoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
