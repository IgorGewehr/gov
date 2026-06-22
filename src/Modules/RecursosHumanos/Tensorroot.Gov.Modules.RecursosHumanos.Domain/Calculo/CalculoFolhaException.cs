namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;

/// <summary>
/// Excecao de dominio do motor de calculo da folha: sinaliza condicao fail-closed (ex.: falta de
/// tabela RPPS municipal para servidor efetivo) que impede um calculo correto e auditavel. Nunca
/// deve ser engolida silenciosamente — folha incorreta e erro auditavel pelo TCE (CLAUDE.md S16).
/// </summary>
public sealed class CalculoFolhaException : Exception
{
    /// <summary>Cria a excecao com a mensagem informada.</summary>
    /// <param name="mensagem">Descricao do impedimento de calculo.</param>
    public CalculoFolhaException(string mensagem)
        : base(mensagem)
    {
    }

    /// <summary>Cria a excecao com mensagem e causa.</summary>
    /// <param name="mensagem">Descricao do impedimento.</param>
    /// <param name="inner">Excecao original.</param>
    public CalculoFolhaException(string mensagem, Exception inner)
        : base(mensagem, inner)
    {
    }
}
