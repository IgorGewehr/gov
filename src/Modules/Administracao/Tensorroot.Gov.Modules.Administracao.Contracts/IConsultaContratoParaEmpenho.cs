namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Porta de LEITURA cross-module (CLAUDE.md §2) exposta pelo modulo Administracao para o modulo Financas:
/// responde se um contrato esta APTO A SUSTENTAR EMPENHO. A regra de aptidao e do dominio do contrato
/// (W9.1, invariante de bloqueio): so empenha contrato com <b>numero de controle PNCP</b> (divulgado —
/// Lei 14.133/2021, art. 94) e nao extinto. Financas NUNCA conhece a entidade interna <c>Contrato</c> nem
/// referencia o Administracao.Domain — passa apenas o <c>contratoId</c>; o proprio Administracao resolve o
/// contrato do tenant (Global Query Filter) e devolve um veredito chapado.
/// <para>
/// SEGURANCA: o <c>contratoId</c> e revalidado contra o tenant atual — contrato de outro
/// tenant retorna <see cref="StatusContratoParaEmpenho.NaoEncontrado"/> (indistinguivel de inexistente).
/// </para>
/// </summary>
public interface IConsultaContratoParaEmpenho
{
    /// <summary>
    /// Apura o status de um contrato para fins de empenho no tenant atual. Read-only.
    /// </summary>
    /// <param name="contratoId">Identificador do contrato (do empenho a emitir).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Veredito de aptidao do contrato para empenho.</returns>
    Task<StatusContratoParaEmpenho> ConsultarAsync(Guid contratoId, CancellationToken cancellationToken);
}

/// <summary>Veredito de aptidao de um contrato para sustentar empenho (W9.1, invariante de bloqueio).</summary>
public enum StatusContratoParaEmpenho
{
    /// <summary>Contrato inexistente no tenant (ou de outro tenant — indistinguivel).</summary>
    NaoEncontrado = 0,

    /// <summary>Apto: possui numero de controle PNCP (divulgado — art. 94) e nao foi extinto.</summary>
    AptoParaEmpenho = 1,

    /// <summary>Bloqueado: sem numero de controle PNCP (contrato ineficaz — art. 94). NAO empenha.</summary>
    BloqueadoSemPncp = 2,

    /// <summary>Bloqueado: contrato encerrado/rescindido (extinto). NAO empenha.</summary>
    BloqueadoContratoExtinto = 3,
}
