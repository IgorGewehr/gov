namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>
/// Porta de Aplicacao que resolve o conjunto de Unidades Organizacionais (UOs) que um usuario pode
/// LER — a UNIAO das UOs do seu escopo efetivo (qualquer permissao). Encapsula o calculo interno do
/// escopo (que cruza atribuicoes x papeis x arvore de UOs) para que a Infraestrutura (filtro de UO
/// do <c>ModuleDbContext</c>, MODELO §5) o consuma sem acessar o internal da Aplicacao.
/// </summary>
public interface IResolvedorEscopoUnidade
{
    /// <summary>
    /// Resolve as UOs (ids) que o usuario pode ler no instante informado. Conjunto vazio = nenhuma
    /// UO no escopo (deny-by-default na leitura filtrada, invariante I2).
    /// </summary>
    /// <param name="usuarioId">Usuario sujeito da requisicao.</param>
    /// <param name="instante">Momento de referencia para a vigencia das atribuicoes.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>UOs (ids) que o usuario pode ler.</returns>
    Task<IReadOnlyCollection<Guid>> ResolverUnidadesLegiveisAsync(
        Guid usuarioId,
        DateTimeOffset instante,
        CancellationToken cancellationToken);
}
