namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>
/// Configuracao do cofre A1 (secao "Cofre"). Em PRODUCAO a KEK vive no Key Vault (wrap/unwrap, a
/// chave nunca sai do HSM); em DEV usa-se uma KEK de config FORA do repo (A1-DESIGN §1). NUNCA
/// hardcode segredo no codigo (CLAUDE.md §6/§7).
/// </summary>
public sealed class CofreOptions
{
    /// <summary>Nome da secao de configuracao.</summary>
    public const string Secao = "Cofre";

    /// <summary>
    /// Provedor de KEK: "Config" (DEV) ou "KeyVault" (PROD). Default "Config" para desenvolvimento.
    /// </summary>
    public string ProvedorKek { get; set; } = "Config";

    // --- KEK de DEV (provedor "Config") ---

    /// <summary>
    /// KEK em Base64 (32 bytes AES-256) para DEV, vinda de variavel de ambiente/secret manager FORA
    /// do repo. // TODO(prod: Key Vault wrap/unwrap) — em PRODUCAO este campo nao e usado.
    /// </summary>
    public string? KekBase64 { get; set; }

    /// <summary>Identificador/versao logico da KEK de DEV (persistido para rotacao).</summary>
    public string KekKeyIdDev { get; set; } = "dev-local-kek-v1";

    // --- KEK de PROD (provedor "KeyVault") ---

    /// <summary>URI do Azure Key Vault (PROD).</summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>Nome da chave-mestra (KEK) no Key Vault (PROD).</summary>
    public string? KekKeyName { get; set; }
}
