namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Excecao de transicao invalida de um convenio federal RECEBIDO (fluxo A). Sinaliza violacao de uma
/// invariante de maquina de estados (A-INV-*): a operacao nao e admitida na situacao atual do agregado.
/// </summary>
public sealed class InvalidConvenioStateException : InvalidOperationException
{
    /// <summary>Inicializa a excecao com a mensagem informada.</summary>
    /// <param name="message">Descricao da violacao (cita a invariante).</param>
    public InvalidConvenioStateException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Excecao de transicao invalida de uma parceria-saida OSC (MROSC, fluxo B). Sinaliza violacao de uma
/// invariante de maquina de estados (B-INV-*): a operacao nao e admitida na situacao atual do agregado.
/// </summary>
public sealed class InvalidParceriaStateException : InvalidOperationException
{
    /// <summary>Inicializa a excecao com a mensagem informada.</summary>
    /// <param name="message">Descricao da violacao (cita a invariante).</param>
    public InvalidParceriaStateException(string message)
        : base(message)
    {
    }
}
