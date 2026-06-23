using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;

namespace Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;

/// <summary>
/// Porta de hash de senha do realm CIDADAO. A implementacao (Infrastructure) usa o MESMO algoritmo
/// forte do Identidade (BCrypt, salgado, comparado em tempo constante), mas e uma porta PROPRIA do
/// modulo para preservar o isolamento (CLAUDE.md §2): o Cidadao nao referencia o interno do Identidade.
/// </summary>
public interface ISenhaHasherCidadao
{
    /// <summary>Calcula o hash de uma senha em claro (sal/parametros embutidos no resultado).</summary>
    /// <param name="senha">Senha em claro.</param>
    /// <returns>Hash serializado, pronto para persistencia.</returns>
    string Hash(string senha);

    /// <summary>Verifica, em tempo constante, se a senha corresponde ao hash armazenado.</summary>
    /// <param name="senha">Senha em claro informada.</param>
    /// <param name="hash">Hash previamente armazenado.</param>
    /// <returns><c>true</c> se corresponder.</returns>
    bool Verificar(string senha, string hash);
}

/// <summary>Token de acesso do cidadao emitido apos autenticacao bem-sucedida.</summary>
/// <param name="AccessToken">JWT assinado.</param>
/// <param name="ExpiraEm">Instante de expiracao (UTC).</param>
public sealed record TokenCidadaoEmitido(string AccessToken, DateTimeOffset ExpiraEm);

/// <summary>
/// Porta de emissao do JWT do CIDADAO. A implementacao assina um JWT com <c>sub</c> = id da conta,
/// <c>tenant</c> e a claim <c>tipo=cidadao</c> — e SEM nenhuma permissao RBAC ("perm"): o cidadao nao
/// tem papel no RBAC organizacional. O segredo/issuer/audience sao os MESMOS do token interno (mesma
/// infra de validacao no ApiHost), de modo que o resolvedor dado-proprio leia o <c>sub</c> identico.
/// </summary>
public interface IEmissorTokenCidadao
{
    /// <summary>Emite o JWT do cidadao autenticado.</summary>
    /// <param name="contaId">Identificador da conta-cidadao (vira o <c>sub</c>).</param>
    /// <param name="tenantId">Tenant (municipio) da conta.</param>
    /// <param name="nome">Nome de exibicao.</param>
    /// <param name="documento">Documento (CPF/CNPJ em digitos) — claim auxiliar.</param>
    /// <param name="selo">Selo gov.br, quando houver (claim "selo").</param>
    /// <returns>Token emitido.</returns>
    TokenCidadaoEmitido Emitir(
        CidadaoContaId contaId,
        Guid tenantId,
        string nome,
        string documento,
        SeloGovBr? selo);
}

/// <summary>Repositorio do agregado <see cref="CidadaoConta"/>.</summary>
public interface ICidadaoContaRepository
{
    /// <summary>Marca uma nova conta-cidadao para insercao.</summary>
    /// <param name="conta">Conta a adicionar.</param>
    void Adicionar(CidadaoConta conta);

    /// <summary>
    /// Obtem a conta-cidadao por DOCUMENTO no tenant atual (Global Query Filter), para autenticacao.
    /// </summary>
    /// <param name="documento">CPF/CNPJ em digitos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta, ou <c>null</c> se inexistente no tenant.</returns>
    Task<CidadaoConta?> ObterPorDocumentoAsync(string documento, CancellationToken cancellationToken);

    /// <summary>
    /// Obtem a conta-cidadao pelo seu identificador (o <c>sub</c> do JWT) no tenant atual — ANCORA do
    /// resolvedor dado-proprio.
    /// </summary>
    /// <param name="contaId">Identificador da conta (subject do JWT).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta, ou <c>null</c>.</returns>
    Task<CidadaoConta?> ObterPorIdAsync(CidadaoContaId contaId, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe conta com o documento no tenant atual (unicidade).</summary>
    /// <param name="documento">CPF/CNPJ em digitos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja houver conta.</returns>
    Task<bool> DocumentoJaCadastradoAsync(string documento, CancellationToken cancellationToken);
}
