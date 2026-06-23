using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.GovBr;

/// <summary>
/// STUB do provedor de identidade gov.br (Relying Party OIDC). // TODO(M10-creds): substituir pela
/// implementacao real (authorization code + validacao do id_token contra o JWKS do gov.br + leitura do
/// CPF/selo), atras de ACL + Polly + Outbox, quando houver client_id/secret/JWKS/endpoints E a ADESAO do
/// municipio a Rede gov.br (bloqueadores externos — M8-DESIGN A.1/E.1). Ate la, o login LOCAL cobre 100%
/// do portal; este stub falha explicitamente para nao mascarar a ausencia de credenciais.
/// </summary>
public sealed class ProvedorIdentidadeGovBrStub : IProvedorIdentidadeGovBr
{
    /// <inheritdoc />
    public Task<IdentidadeGovBr> ValidarLoginAsync(string codigoAutorizacao, CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "Login gov.br nao configurado (// TODO(M10-creds): adesao do municipio a Rede gov.br + client_id/secret/JWKS). Use o login local (CPF/CNPJ + senha).");
}
