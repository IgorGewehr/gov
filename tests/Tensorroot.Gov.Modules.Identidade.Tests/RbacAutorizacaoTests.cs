using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura do RBAC policy-based: o <see cref="PermissaoHandler"/> concede acesso quando o
/// principal possui a claim "perm" correspondente e NEGA por padrao na ausencia dela. E SEGURANCA
/// CRITICA: nenhuma permissao parcial ou claim de outro tipo concede acesso.
/// </summary>
public sealed class RbacAutorizacaoTests
{
    private static ClaimsPrincipal PrincipalCom(params string[] permissoes)
    {
        var claims = permissoes.Select(p => new Claim(EmissorToken.ClaimPermissao, p));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    private static async Task<bool> AvaliarAsync(ClaimsPrincipal principal, string permissaoExigida)
    {
        var requisito = new PermissaoRequirement(permissaoExigida);
        var contexto = new AuthorizationHandlerContext([requisito], principal, resource: null);

        await new PermissaoHandler().HandleAsync(contexto);
        return contexto.HasSucceeded;
    }

    [Fact]
    public async Task Concede_quando_o_principal_possui_a_permissao_exigida()
    {
        var principal = PrincipalCom(DomainPermissoes.TributosGerenciar, DomainPermissoes.TributosVer);

        var permitido = await AvaliarAsync(principal, DomainPermissoes.TributosGerenciar);

        permitido.Should().BeTrue();
    }

    [Fact]
    public async Task Nega_quando_o_principal_nao_possui_a_permissao_exigida()
    {
        var principal = PrincipalCom(DomainPermissoes.TributosVer);

        var permitido = await AvaliarAsync(principal, DomainPermissoes.TributosGerenciar);

        permitido.Should().BeFalse();
    }

    [Fact]
    public async Task Nega_por_padrao_quando_o_principal_nao_tem_nenhuma_permissao()
    {
        var permitido = await AvaliarAsync(PrincipalCom(), DomainPermissoes.IdentidadeUsuariosGerenciar);

        permitido.Should().BeFalse();
    }

    [Fact]
    public async Task Claim_de_outro_tipo_com_o_mesmo_valor_nao_concede_acesso()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("role", DomainPermissoes.TributosGerenciar)],
            authenticationType: "Test"));

        var permitido = await AvaliarAsync(principal, DomainPermissoes.TributosGerenciar);

        permitido.Should().BeFalse();
    }

    [Fact]
    public void Requisito_exige_permissao_nao_vazia()
    {
        var acao = () => new PermissaoRequirement("   ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NomePolitica_prefixa_o_escopo_com_perm()
    {
        var nome = PermissionPolicyProvider.NomePolitica(DomainPermissoes.TributosGerenciar);

        nome.Should().Be("perm:" + DomainPermissoes.TributosGerenciar);
    }

    // ---------- Catalogo de permissoes (negar por padrao no dominio) ----------

    [Fact]
    public void Permissao_fora_do_catalogo_nao_e_conhecida()
    {
        DomainPermissoes.EhConhecida("permissao.inventada").Should().BeFalse();
        DomainPermissoes.EhConhecida(null).Should().BeFalse();
        DomainPermissoes.EhConhecida(DomainPermissoes.TributosGerenciar).Should().BeTrue();
    }
}
