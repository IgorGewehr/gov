using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.ApiHost.Provisioning;

/// <summary>Requisição de provisionamento de um tenant.</summary>
/// <param name="Cnpj">CNPJ do ente.</param>
/// <param name="Nome">Nome do ente.</param>
/// <param name="Poder">Poder (Executivo/Legislativo).</param>
/// <param name="ConnectionString">Conexão do banco dedicado (opcional; fallback por convenção).</param>
/// <param name="Modulos">Módulos a licenciar.</param>
public sealed record ProvisionarTenantRequest(
    string Cnpj,
    string Nome,
    PoderTenant Poder,
    string? ConnectionString,
    IReadOnlyList<string> Modulos);
