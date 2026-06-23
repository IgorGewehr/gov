namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

/// <summary>
/// Excecao de dominio das consignacoes: sinaliza violacao de invariante de negocio (ex.: averbacao
/// que estouraria a margem consignavel do balde, ou consignataria inativa). Folha que estoura a margem
/// e erro auditavel pelo TCE — nunca deve ser engolida silenciosamente (CLAUDE.md S16).
/// </summary>
public sealed class ConsignacaoException : Exception
{
    /// <summary>Cria a excecao com a mensagem informada.</summary>
    /// <param name="mensagem">Descricao da violacao de invariante.</param>
    public ConsignacaoException(string mensagem)
        : base(mensagem)
    {
    }

    /// <summary>Cria a excecao com mensagem e causa.</summary>
    /// <param name="mensagem">Descricao da violacao.</param>
    /// <param name="inner">Excecao original.</param>
    public ConsignacaoException(string mensagem, Exception inner)
        : base(mensagem, inner)
    {
    }
}
