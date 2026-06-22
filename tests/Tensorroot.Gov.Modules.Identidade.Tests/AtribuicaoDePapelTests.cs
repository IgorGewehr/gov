using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Domain.Events;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura de dominio da atribuicao de papel COM ESCOPO (MODELO §2.2): atribuir/revogar papel
/// numa UO com vigencia e origem, idempotencia por escopo, manutencao da visao plana derivada
/// <see cref="Usuario.Papeis"/> e a PONTE DE COMPATIBILIDADE (<see cref="Usuario.DefinirPapeis"/>)
/// que converte papeis planos legados em atribuicoes no escopo raiz/global com IncluiSubunidades.
/// </summary>
public sealed class AtribuicaoDePapelTests
{
    private const string Hash = "$2a$04$hashqualquerparaoteste0000000000000000000000000000";

    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Usuario NovoUsuario()
        => Usuario.Criar(Tenant, "Maria", Email.De("maria@x.gov.br"), Hash);

    [Fact]
    public void AtribuirPapel_registra_atribuicao_com_escopo_vigencia_e_origem()
    {
        var usuario = NovoUsuario();
        var papel = PapelId.New();
        var uo = UnidadeOrganizacionalId.New();
        var agora = DateTimeOffset.UtcNow;

        usuario.AtribuirPapel(papel, uo, incluiSubunidades: true, Vigencia.Aberta(agora), OrigemAtribuicao.Direta());

        usuario.Atribuicoes.Should().ContainSingle();
        var atribuicao = usuario.Atribuicoes.Single();
        atribuicao.PapelId.Should().Be(papel);
        atribuicao.UnidadeId.Should().Be(uo);
        atribuicao.IncluiSubunidades.Should().BeTrue();
        atribuicao.Origem.Tipo.Should().Be(TipoOrigem.Direta);
        atribuicao.VigenteEm(agora).Should().BeTrue();

        // Visao plana derivada reflete o papel (compatibilidade de enforcement).
        usuario.Papeis.Should().ContainSingle().Which.Should().Be(papel);
        usuario.DomainEvents.OfType<PapelAtribuido>().Should().ContainSingle();
    }

    [Fact]
    public void AtribuirPapel_e_idempotente_para_o_mesmo_escopo()
    {
        var usuario = NovoUsuario();
        var papel = PapelId.New();
        var uo = UnidadeOrganizacionalId.New();
        var agora = DateTimeOffset.UtcNow;

        usuario.AtribuirPapel(papel, uo, incluiSubunidades: true, Vigencia.Aberta(agora), OrigemAtribuicao.Direta());
        usuario.AtribuirPapel(papel, uo, incluiSubunidades: true, Vigencia.Aberta(agora), OrigemAtribuicao.Direta());

        usuario.Atribuicoes.Should().ContainSingle();
    }

    [Fact]
    public void Mesmo_papel_em_UOs_distintas_gera_atribuicoes_distintas_e_um_unico_papel_plano()
    {
        var usuario = NovoUsuario();
        var papel = PapelId.New();
        var saude = UnidadeOrganizacionalId.New();
        var educacao = UnidadeOrganizacionalId.New();
        var agora = DateTimeOffset.UtcNow;

        usuario.AtribuirPapel(papel, saude, incluiSubunidades: false, Vigencia.Aberta(agora), OrigemAtribuicao.Direta());
        usuario.AtribuirPapel(papel, educacao, incluiSubunidades: false, Vigencia.Aberta(agora), OrigemAtribuicao.Direta());

        usuario.Atribuicoes.Should().HaveCount(2);
        usuario.Papeis.Should().ContainSingle().Which.Should().Be(papel);
    }

    [Fact]
    public void RevogarPapel_numa_UO_so_remove_o_papel_plano_quando_nao_resta_nenhuma_atribuicao()
    {
        var usuario = NovoUsuario();
        var papel = PapelId.New();
        var saude = UnidadeOrganizacionalId.New();
        var educacao = UnidadeOrganizacionalId.New();
        var agora = DateTimeOffset.UtcNow;

        usuario.AtribuirPapel(papel, saude, incluiSubunidades: false, Vigencia.Aberta(agora), OrigemAtribuicao.Direta());
        usuario.AtribuirPapel(papel, educacao, incluiSubunidades: false, Vigencia.Aberta(agora), OrigemAtribuicao.Direta());

        usuario.RevogarPapel(papel, saude);

        usuario.Atribuicoes.Should().ContainSingle().Which.UnidadeId.Should().Be(educacao);
        usuario.Papeis.Should().ContainSingle(); // ainda lotado na Educacao

        usuario.RevogarPapel(papel, educacao);

        usuario.Atribuicoes.Should().BeEmpty();
        usuario.Papeis.Should().BeEmpty();
        usuario.DomainEvents.OfType<PapelRevogado>().Should().HaveCount(2);
    }

    [Fact]
    public void DefinirPapeis_compatibilidade_converte_papeis_planos_em_atribuicoes_globais_com_subunidades()
    {
        var papeis = new[] { PapelId.New(), PapelId.New() };

        var usuario = Usuario.Criar(Tenant, "Admin", Email.De("admin@x.gov.br"), Hash, papeis);

        usuario.Papeis.Should().BeEquivalentTo(papeis);
        usuario.Atribuicoes.Should().HaveCount(2);
        usuario.Atribuicoes.Should().OnlyContain(a =>
            a.UnidadeId == UnidadeOrganizacionalId.RaizPendente
            && a.IncluiSubunidades
            && a.Origem.Tipo == TipoOrigem.Direta);
    }

    [Fact]
    public void Vigencia_com_fim_anterior_ao_inicio_e_rejeitada()
    {
        var inicio = DateTimeOffset.UtcNow;

        var acao = () => Vigencia.Criar(inicio, inicio.AddDays(-1));

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Origem_delegada_registra_o_concedente()
    {
        var concedente = UsuarioId.New();

        var origem = OrigemAtribuicao.Delegada(concedente);

        origem.Tipo.Should().Be(TipoOrigem.Delegada);
        origem.ConcedentId.Should().Be(concedente);
    }
}
