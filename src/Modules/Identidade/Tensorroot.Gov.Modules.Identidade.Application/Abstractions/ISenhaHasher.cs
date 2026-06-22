namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>
/// Porta de hash de senha. A implementacao (Infrastructure) deve usar um algoritmo lento e salgado
/// resistente a forca-bruta (ex.: PBKDF2/Argon2/BCrypt) e comparar em tempo constante.
/// </summary>
public interface ISenhaHasher
{
    /// <summary>Calcula o hash de uma senha em claro (sal e parametros embutidos no resultado).</summary>
    /// <param name="senha">Senha em claro.</param>
    /// <returns>Hash serializado, pronto para persistencia.</returns>
    string Hash(string senha);

    /// <summary>Verifica, em tempo constante, se uma senha em claro corresponde ao hash armazenado.</summary>
    /// <param name="senha">Senha em claro informada.</param>
    /// <param name="hash">Hash previamente armazenado.</param>
    /// <returns><c>true</c> se corresponder; caso contrario, <c>false</c>.</returns>
    bool Verificar(string senha, string hash);
}
