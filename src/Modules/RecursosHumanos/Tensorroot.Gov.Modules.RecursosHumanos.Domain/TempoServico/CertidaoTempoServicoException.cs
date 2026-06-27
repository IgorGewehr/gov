namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

/// <summary>
/// Excecao de dominio da certidao de tempo de servico/contribuicao: sinaliza violacao de invariante legal
/// (ex.: certidao sem periodo, ou contagem concomitante vedada pelo art. 96, II, da Lei 8.213/1991). Certidao
/// com tempo apurado errado e' documento publico defeituoso, fonte de fraude previdenciaria — a violacao
/// nunca deve ser engolida silenciosamente (CLAUDE.md S16).
/// </summary>
public sealed class CertidaoTempoServicoException : Exception
{
    /// <summary>Cria a excecao com a mensagem informada.</summary>
    /// <param name="mensagem">Descricao da violacao de invariante.</param>
    public CertidaoTempoServicoException(string mensagem)
        : base(mensagem)
    {
    }

    /// <summary>Cria a excecao com mensagem e causa.</summary>
    /// <param name="mensagem">Descricao da violacao.</param>
    /// <param name="inner">Excecao original.</param>
    public CertidaoTempoServicoException(string mensagem, Exception inner)
        : base(mensagem, inner)
    {
    }
}
