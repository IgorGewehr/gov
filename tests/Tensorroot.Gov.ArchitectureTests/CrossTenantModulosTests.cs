using FluentAssertions;
using Tensorroot.Gov.ApiHost.Admin;
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// SEGURANCA CRITICA — fecha a defesa-em-profundidade do achado XT-1 (admin de A configurava modulos
/// de B). Alem da segregacao de permissao de plataforma (provada no modulo Identidade), os endpoints
/// de modulo comparam o tenant da ROTA com o do CONTEXTO: um principal de TENANT so opera o proprio
/// tenant; operador de PLATAFORMA (sem tenant ligado) opera qualquer um no onboarding.
/// </summary>
public sealed class CrossTenantModulosTests
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Principal_de_tenant_operando_OUTRO_tenant_e_negado()
        => AdminEndpoints.DeveNegarOperacaoCrossTenant(TenantB, contextoTemTenant: true, tenantContexto: TenantA)
            .Should().BeTrue();

    [Fact]
    public void Principal_de_tenant_operando_o_PROPRIO_tenant_e_permitido()
        => AdminEndpoints.DeveNegarOperacaoCrossTenant(TenantA, contextoTemTenant: true, tenantContexto: TenantA)
            .Should().BeFalse();

    [Fact]
    public void Operador_de_plataforma_sem_tenant_ligado_opera_qualquer_tenant()
        => AdminEndpoints.DeveNegarOperacaoCrossTenant(TenantA, contextoTemTenant: false, tenantContexto: Guid.Empty)
            .Should().BeFalse();
}
