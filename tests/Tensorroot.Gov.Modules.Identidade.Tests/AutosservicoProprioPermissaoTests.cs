using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Semantica da permissao do AUTOSSERVICO do servidor (<c>autosservico.proprio</c>): e um verbo
/// ATRIBUIVEL por RBAC ao papel "Servidor" (precisa ser CONHECIDA para persistir), mas
/// DELIBERADAMENTE FORA de <see cref="DomainPermissoes.Todas"/> — assim o papel "Administrador"
/// (que recebe <c>Todas</c>) nao a recebe e o autosservico fica restrito a quem e, de fato, um
/// servidor. Distingue-se das permissoes de PLATAFORMA, que nem sequer sao atribuiveis por tenant.
/// </summary>
public sealed class AutosservicoProprioPermissaoTests
{
    [Fact]
    public void AutosservicoProprio_tem_valor_de_claim_estavel()
        => DomainPermissoes.AutosservicoProprio.Should().Be("autosservico.proprio");

    [Fact]
    public void AutosservicoProprio_NAO_pertence_a_Todas_do_admin_de_tenant()
        => DomainPermissoes.Todas.Should().NotContain(DomainPermissoes.AutosservicoProprio);

    [Fact]
    public void AutosservicoProprio_e_CONHECIDA_logo_atribuivel_a_um_papel()
    {
        DomainPermissoes.EhConhecida(DomainPermissoes.AutosservicoProprio).Should().BeTrue();
        DomainPermissoes.Conhecidas.Should().Contain(DomainPermissoes.AutosservicoProprio);
    }

    [Fact]
    public void Papel_Servidor_aceita_apenas_autosservico_proprio_e_nada_de_terceiros()
    {
        var papel = Papel.Criar(Guid.NewGuid(), "Servidor", new[] { DomainPermissoes.AutosservicoProprio });

        papel.Permissoes.Should().ContainSingle().Which.Should().Be(DomainPermissoes.AutosservicoProprio);
        // O papel "Servidor" NAO tem o "ver de todos" do RH (gestor) — so o dado-proprio.
        papel.Permissoes.Should().NotContain(DomainPermissoes.RecursosHumanosVer);
    }
}
