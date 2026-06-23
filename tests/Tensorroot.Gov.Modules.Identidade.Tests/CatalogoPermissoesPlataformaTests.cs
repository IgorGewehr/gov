using FluentAssertions;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// SEGURANCA CRITICA — fecha o achado XT-1 (admin de A configurava licencas de modulo de B). A
/// correcao estrutural e mover <c>admin.modulos.configurar</c> para uma permissao de PLATAFORMA
/// (<see cref="DomainPermissoes.PlataformaModulosConfigurar"/>) que NAO pertence a
/// <see cref="DomainPermissoes.Todas"/>. Como o papel "Administrador" semeado por tenant recebe
/// exatamente <c>Todas</c>, ele JAMAIS recebe os verbos de plataforma — deny-by-default por
/// CONSTRUCAO (o mesmo padrao ja aplicado a <c>plataforma.tenants.provisionar</c>).
/// </summary>
public sealed class CatalogoPermissoesPlataformaTests
{
    [Fact]
    public void PlataformaModulosConfigurar_NAO_pertence_a_Todas()
        => DomainPermissoes.Todas.Should().NotContain(DomainPermissoes.PlataformaModulosConfigurar);

    [Fact]
    public void PlataformaTenantsProvisionar_NAO_pertence_a_Todas()
        => DomainPermissoes.Todas.Should().NotContain(DomainPermissoes.PlataformaTenantsProvisionar);

    [Fact]
    public void AdminModulosConfigurar_legado_NAO_pertence_mais_a_Todas()
        => DomainPermissoes.Todas.Should().NotContain(DomainPermissoes.AdminModulosConfigurar);

    [Fact]
    public void Permissoes_de_plataforma_continuam_conhecidas_no_catalogo_pelas_constantes()
    {
        // As constantes existem e sao estaveis (valores de claim), mesmo fora de "Todas".
        DomainPermissoes.PlataformaModulosConfigurar.Should().Be("plataforma.modulos.configurar");
        DomainPermissoes.PlataformaTenantsProvisionar.Should().Be("plataforma.tenants.provisionar");
    }

    [Fact]
    public void Nenhum_verbo_de_plataforma_vaza_para_o_conjunto_do_admin_de_tenant()
    {
        // "Todas" e o teto do admin de tenant; nenhum escopo "plataforma.*" pode estar nele.
        DomainPermissoes.Todas.Should().NotContain(p => p.StartsWith("plataforma.", StringComparison.Ordinal));
    }

    [Fact]
    public void Verbos_eSic_pertencem_ao_catalogo_e_ao_papel_Administrador()
    {
        // Follow-up Onda 2: o catalogo DEVE refletir os verbos e-SIC e o papel "Administrador"
        // (que recebe "Todas") deve concede-los. Sem isso, os endpoints internos de e-SIC ficariam
        // inacessiveis ate concessao manual. Deny-by-default segue valendo (so o admin os recebe).
        DomainPermissoes.TransparenciaEsicVer.Should().Be("transparencia.esic.ver");
        DomainPermissoes.TransparenciaEsicResponder.Should().Be("transparencia.esic.responder");
        DomainPermissoes.Todas.Should().Contain(DomainPermissoes.TransparenciaEsicVer);
        DomainPermissoes.Todas.Should().Contain(DomainPermissoes.TransparenciaEsicResponder);
    }
}
