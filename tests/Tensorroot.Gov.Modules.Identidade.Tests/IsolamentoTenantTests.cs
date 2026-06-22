using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Repositories;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura do isolamento multi-tenant do modulo Identidade sobre SQLite em memoria: o Global
/// Query Filter por TenantId impede que um tenant enxergue usuarios/papeis de outro, e a leitura
/// de autenticacao (que ignora o filtro) ainda exige correspondencia explicita de tenant. E
/// SEGURANCA CRITICA: vazamento cross-tenant e falha critica.
/// </summary>
public sealed class IsolamentoTenantTests : IdentidadeTestBase
{
    private static Usuario NovoUsuario(Guid tenantId, string email)
        => Usuario.Criar(tenantId, "Usuario", Email.De(email), "$2a$04$hashqualquerparaoteste0000000000000000000000000000");

    [Fact]
    public async Task Usuario_de_um_tenant_nao_e_visivel_pelo_outro()
    {
        await using (var ctxA = CriarContexto(TenantA))
        {
            ctxA.Usuarios.Add(NovoUsuario(TenantA, "a@x.gov.br"));
            await ctxA.SaveChangesAsync();
        }

        await using var ctxB = CriarContexto(TenantB);
        var repoB = new UsuarioRepository(ctxB);

        var visivelEmB = await repoB.ObterPorEmailAsync(Email.De("a@x.gov.br"), CancellationToken.None);
        var listaB = await repoB.ListarAsync(CancellationToken.None);

        visivelEmB.Should().BeNull();
        listaB.Should().BeEmpty();
    }

    [Fact]
    public async Task Cada_tenant_ve_apenas_os_proprios_usuarios()
    {
        await using (var ctxA = CriarContexto(TenantA))
        {
            ctxA.Usuarios.Add(NovoUsuario(TenantA, "a@x.gov.br"));
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = CriarContexto(TenantB))
        {
            ctxB.Usuarios.Add(NovoUsuario(TenantB, "b@x.gov.br"));
            await ctxB.SaveChangesAsync();
        }

        await using var leituraA = CriarContexto(TenantA);
        await using var leituraB = CriarContexto(TenantB);

        var deA = await new UsuarioRepository(leituraA).ListarAsync(CancellationToken.None);
        var deB = await new UsuarioRepository(leituraB).ListarAsync(CancellationToken.None);

        deA.Should().ContainSingle().Which.Email.Valor.Should().Be("a@x.gov.br");
        deB.Should().ContainSingle().Which.Email.Valor.Should().Be("b@x.gov.br");
    }

    [Fact]
    public async Task Leitura_de_autenticacao_exige_o_tenant_correto_mesmo_ignorando_o_filtro()
    {
        await using (var ctxA = CriarContexto(TenantA))
        {
            ctxA.Usuarios.Add(NovoUsuario(TenantA, "a@x.gov.br"));
            await ctxA.SaveChangesAsync();
        }

        await using var ctx = CriarContexto(TenantB);
        var repo = new UsuarioRepository(ctx);

        var noTenantErrado = await repo.ObterParaAutenticacaoAsync(TenantB, Email.De("a@x.gov.br"), CancellationToken.None);
        var noTenantCorreto = await repo.ObterParaAutenticacaoAsync(TenantA, Email.De("a@x.gov.br"), CancellationToken.None);

        noTenantErrado.Should().BeNull();
        noTenantCorreto.Should().NotBeNull();
        noTenantCorreto!.TenantId.Should().Be(TenantA);
    }

    [Fact]
    public async Task Papel_de_um_tenant_nao_e_visivel_pelo_outro()
    {
        await using (var ctxA = CriarContexto(TenantA))
        {
            ctxA.Papeis.Add(Papel.Criar(TenantA, "Fiscal", [DomainPermissoes.TributosVer]));
            await ctxA.SaveChangesAsync();
        }

        await using var ctxB = CriarContexto(TenantB);
        var papeisB = await new PapelRepository(ctxB).ListarAsync(CancellationToken.None);

        papeisB.Should().BeEmpty();
    }

    [Fact]
    public async Task Usuario_persiste_e_recarrega_papeis_e_hash_no_proprio_tenant()
    {
        var papel = Papel.Criar(TenantA, "Fiscal", [DomainPermissoes.TributosVer, DomainPermissoes.TributosGerenciar]);
        var usuario = Usuario.Criar(TenantA, "Maria", Email.De("maria@x.gov.br"), "$2a$04$hashqualquerparaoteste0000000000000000000000000000", [papel.Id]);

        await using (var ctx = CriarContexto(TenantA))
        {
            ctx.Papeis.Add(papel);
            ctx.Usuarios.Add(usuario);
            await ctx.SaveChangesAsync();
        }

        await using var leitura = CriarContexto(TenantA);
        var recarregado = await leitura.Usuarios.FirstAsync(u => u.Email == Email.De("maria@x.gov.br"));

        recarregado.TenantId.Should().Be(TenantA);
        recarregado.SenhaHash.Should().StartWith("$2a$");
        recarregado.Papeis.Should().ContainSingle().Which.Should().Be(papel.Id);
    }
}
