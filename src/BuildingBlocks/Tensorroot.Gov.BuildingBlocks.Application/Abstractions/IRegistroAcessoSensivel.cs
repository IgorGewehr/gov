using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Porta (LG-2) para registrar, de forma append-only e a prova de adulteracao, um ACESSO de LEITURA
/// a um dado pessoal sensivel: <c>{Tenant, UserId, Ip, Entidade, EntityId, BaseLegal, Ts}</c>. A
/// implementacao reusa a cadeia de hash da trilha de auditoria, de modo que a leitura fique selada
/// na mesma trilha imutavel das mutacoes (art. 37 LGPD, CLAUDE.md §6).
/// </summary>
public interface IRegistroAcessoSensivel
{
    /// <summary>Registra (sela na trilha) um acesso de leitura a recurso sensivel.</summary>
    /// <param name="entidade">Nome da entidade/recurso lido.</param>
    /// <param name="entidadeId">Identificador do recurso, quando conhecido.</param>
    /// <param name="baseLegal">Hipotese legal LGPD que autoriza o acesso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa da operacao.</returns>
    Task RegistrarAsync(
        string entidade,
        string? entidadeId,
        BaseLegalLgpd baseLegal,
        CancellationToken cancellationToken = default);
}
