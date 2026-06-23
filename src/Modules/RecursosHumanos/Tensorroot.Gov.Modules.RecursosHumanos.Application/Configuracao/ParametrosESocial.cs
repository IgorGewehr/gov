using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

/// <summary>
/// Parametros do eSocial por tenant, lidos da configuracao (Key Vault/IOptions), NUNCA hardcoded
/// (CLAUDE.md §7). Inclui o empregador/EFR (origem do S-1000/S-1005, pois nao ha agregado de empregador
/// no RH) e o ambiente de transmissao. ESOCIAL-SPEC §1.1/§2.1.
/// </summary>
public sealed class ParametrosESocial
{
    /// <summary>Secao de configuracao (<c>RecursosHumanos:ESocial</c>).</summary>
    public const string SecaoConfiguracao = "RecursosHumanos:ESocial";

    /// <summary>CNPJ do ente (declarante/empregador) — <c>ideEmpregador/nrInsc</c>.</summary>
    public string? CnpjEnte { get; set; }

    /// <summary>Nome/razao social do ente.</summary>
    public string? NomeEnte { get; set; }

    /// <summary>Classificacao tributaria (Tabela 08). // TODO(validar-oficial).</summary>
    public string? ClassTrib { get; set; }

    /// <summary>CNPJ do Ente Federado Responsavel (EFR) — obrigatorio p/ ente publico no S-1000.</summary>
    public string? CnpjEfr { get; set; }

    /// <summary>Inicio de validade das tabelas (<c>AAAA-MM</c>) para S-1000/S-1005/S-1010.</summary>
    public string? InicioValidade { get; set; }

    /// <summary>Identificador da tabela de rubricas (<c>ideTabRubr</c>) usado no S-1010/S-1200.</summary>
    public string IdeTabRubricas { get; set; } = "RUBRICAS";

    /// <summary>
    /// Ambiente de transmissao. Producao Restrita por padrao (homologacao sem efeito juridico —
    /// ESOCIAL-SPEC §3). So Producao quando o ente estiver apto e autorizado pelo dono.
    /// </summary>
    public AmbienteESocial Ambiente { get; set; } = AmbienteESocial.ProducaoRestrita;
}
