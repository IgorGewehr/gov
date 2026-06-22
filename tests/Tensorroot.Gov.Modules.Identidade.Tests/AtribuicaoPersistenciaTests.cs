using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Repositories;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Integracao (SQLite): prova que as <see cref="AtribuicaoDePapel"/> COM ESCOPO persistem na tabela
/// owned "AtribuicoesPapel" e recarregam (UO, vigencia, origem), e que o
/// <see cref="CalculadoraPermissoesEfetivas"/> resolve o escopo efetivo rico (permissao -> UOs)
/// expandindo subunidades sobre a arvore persistida (MODELO §10.3, I5).
/// </summary>
public sealed class AtribuicaoPersistenciaTests : IdentidadeTestBase
{
    private const string Hash = "$2a$04$hashqualquerparaoteste0000000000000000000000000000";

    [Fact]
    public async Task Atribuicao_com_escopo_persiste_e_recarrega_com_UO_vigencia_e_origem()
    {
        var papelId = PapelId.New();
        var uo = UnidadeOrganizacionalId.New();
        var inicio = DateTimeOffset.UtcNow;

        await using (var contexto = CriarContexto(TenantA))
        {
            var usuario = Usuario.Criar(TenantA, "Maria", Email.De("maria@x.gov.br"), Hash);
            usuario.AtribuirPapel(papelId, uo, incluiSubunidades: true, Vigencia.Criar(inicio, inicio.AddDays(30)), OrigemAtribuicao.Direta());
            contexto.Usuarios.Add(usuario);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var recarregado = await contexto.Usuarios.FirstAsync(u => u.Email == Email.De("maria@x.gov.br"));
            recarregado.Atribuicoes.Should().ContainSingle();
            var atribuicao = recarregado.Atribuicoes.Single();
            atribuicao.PapelId.Should().Be(papelId);
            atribuicao.UnidadeId.Should().Be(uo);
            atribuicao.IncluiSubunidades.Should().BeTrue();
            atribuicao.Vigencia.Fim.Should().BeCloseTo(inicio.AddDays(30), TimeSpan.FromSeconds(1));
            atribuicao.Origem.Tipo.Should().Be(TipoOrigem.Direta);
            // Visao plana derivada tambem reidrata (compatibilidade do enforcement/claim).
            recarregado.Papeis.Should().ContainSingle().Which.Should().Be(papelId);
        }
    }

    [Fact]
    public async Task Calculadora_resolve_permissao_para_o_conjunto_de_UOs_expandindo_subunidades()
    {
        // Arvore PREF -> SMS -> UBS; papel "Operador Saude" atribuido em SMS COM subunidades.
        var pref = UnidadeOrganizacional.CriarRaiz(TenantA, "PREF", "Prefeitura", TipoUnidade.Gabinete);
        var sms = UnidadeOrganizacional.CriarFilha(TenantA, "SMS", "Saude", TipoUnidade.Secretaria, pref.Id);
        var ubs = UnidadeOrganizacional.CriarFilha(TenantA, "SMS.UBS", "UBS", TipoUnidade.Setor, sms.Id);
        var papel = Papel.Criar(TenantA, "Operador Saude", [DomainPermissoes.SaudeVer, DomainPermissoes.SaudeGerenciar]);

        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Unidades.AddRange(pref, sms, ubs);
            contexto.Papeis.Add(papel);
            var usuario = Usuario.Criar(TenantA, "Enf", Email.De("enf@x.gov.br"), Hash);
            usuario.AtribuirPapel(papel.Id, sms.Id, incluiSubunidades: true, Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
            contexto.Usuarios.Add(usuario);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var usuario = await contexto.Usuarios.FirstAsync(u => u.Email == Email.De("enf@x.gov.br"));

            // Porta publica de Aplicacao (mesma usada pelo filtro de UO): UNIAO das UOs legiveis.
            var resolvedor = new ResolvedorEscopoUnidade(
                new UsuarioRepository(contexto),
                new PapelRepository(contexto),
                new UnidadeRepository(contexto));

            var legiveis = await resolvedor.ResolverUnidadesLegiveisAsync(
                usuario.Id.Value, DateTimeOffset.UtcNow, CancellationToken.None);

            // O escopo (SMS com subunidades) expande para SMS + UBS — nao alcanca PREF (acima).
            legiveis.Should().BeEquivalentTo(new[] { sms.Id.Value, ubs.Id.Value });
        }
    }
}
