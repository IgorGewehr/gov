using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;

namespace Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;

/// <summary>
/// Resultado do login unico gov.br (apos validar o <c>id_token</c> contra o JWKS do gov.br): o CPF do
/// cidadao e o selo de confiabilidade. // TODO(M10-creds).
/// </summary>
/// <param name="Cpf">CPF do cidadao (somente digitos) extraido do id_token validado.</param>
/// <param name="Nome">Nome do cidadao.</param>
/// <param name="Selo">Selo gov.br (bronze/prata/ouro).</param>
public readonly record struct IdentidadeGovBr(string Cpf, string Nome, SeloGovBr Selo);

/// <summary>
/// Porta (ACL) do gov.br como Provedor de Identidade (OIDC, authorization code) — o ApiHost atua como
/// Relying Party. A implementacao real valida o <c>id_token</c> contra o JWKS do gov.br, le o CPF + selo
/// e faz match/provisionamento de <see cref="CidadaoConta"/> por (TenantId, Documento) com
/// <see cref="OrigemConta.GovBr"/>. Atras de ACL + Polly + Outbox, como toda integracao governamental.
/// <para>
/// // TODO(M10-creds): client_id/secret, endpoints (authorization/token/userinfo), JWKS e o STATUS DE
/// ADESAO do municipio a Rede gov.br sao BLOQUEADORES EXTERNOS (M8-DESIGN A.1/E.1). Ate la, o login
/// LOCAL (CPF/CNPJ + senha) cobre 100% do portal. Segredos somente no Azure Key Vault (CLAUDE.md §5);
/// gating por selo (consulta/protocolo/ato forte) e parametrizavel por tenant via IOptions (nunca
/// hardcoded — CLAUDE.md §7).
/// </para>
/// </summary>
public interface IProvedorIdentidadeGovBr
{
    /// <summary>
    /// Troca o <paramref name="codigoAutorizacao"/> (authorization code) pela identidade validada do
    /// cidadao. // TODO(M10-creds): implementacao real bloqueada por creds + adesao do municipio.
    /// </summary>
    /// <param name="codigoAutorizacao">Authorization code retornado pelo gov.br.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A identidade gov.br validada (CPF + selo).</returns>
    Task<IdentidadeGovBr> ValidarLoginAsync(string codigoAutorizacao, CancellationToken cancellationToken);
}
