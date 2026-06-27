namespace Tensorroot.Gov.Modules.Financas.Domain.Exceptions;

/// <summary>Lançada quando a remessa CNAB240 está inválida (sem favorecidos ou registro fora do tamanho).</summary>
public sealed class RemessaCnabInvalidaException : InvalidOperationException
{
    /// <summary>Cria a exceção com a mensagem informada.</summary>
    /// <param name="message">Mensagem de erro.</param>
    public RemessaCnabInvalidaException(string message) : base(message)
    {
    }
}
