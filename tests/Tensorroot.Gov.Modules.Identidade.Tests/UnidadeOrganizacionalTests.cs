using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Domain.Events;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Xunit;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura de dominio da <see cref="UnidadeOrganizacional"/> (arvore de UOs): criacao de
/// raiz/filha, normalizacao de codigo, ativar/desativar preservando historico e, sobretudo, a
/// invariante de SUBARVORE CONEXA E ACICLICA (MODELO §8 I5) — uma UO nunca pode ser pai de si
/// mesma nem de um de seus ancestrais (o que criaria ciclo).
/// </summary>
public sealed class UnidadeOrganizacionalTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Criar_raiz_nasce_ativa_sem_pai_e_com_codigo_normalizado()
    {
        var raiz = UnidadeOrganizacional.CriarRaiz(Tenant, " gab ", "Gabinete do Prefeito", TipoUnidade.Gabinete);

        raiz.EhRaiz.Should().BeTrue();
        raiz.UnidadePaiId.Should().BeNull();
        raiz.Ativa.Should().BeTrue();
        raiz.Codigo.Should().Be("GAB");
        raiz.TenantId.Should().Be(Tenant);
        raiz.DomainEvents.OfType<UnidadeOrganizacionalCriada>().Should().ContainSingle();
    }

    [Fact]
    public void Criar_filha_referencia_o_pai_e_e_conexa_por_construcao()
    {
        var raiz = UnidadeOrganizacional.CriarRaiz(Tenant, "PREF", "Prefeitura", TipoUnidade.Gabinete);
        var filha = UnidadeOrganizacional.CriarFilha(Tenant, "SMS", "Secretaria de Saude", TipoUnidade.Secretaria, raiz.Id);

        filha.EhRaiz.Should().BeFalse();
        filha.UnidadePaiId.Should().Be(raiz.Id);
    }

    [Fact]
    public void DefinirPai_para_si_mesma_e_rejeitado_I5()
    {
        var uo = UnidadeOrganizacional.CriarRaiz(Tenant, "PREF", "Prefeitura", TipoUnidade.Gabinete);

        var acao = () => uo.DefinirPai(uo.Id, ancestraisDoNovoPai: []);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*nao pode ser pai dela mesma*");
    }

    [Fact]
    public void DefinirPai_para_um_descendente_cria_ciclo_e_e_rejeitado_I5()
    {
        // Arvore: raiz -> depto -> setor. Mover a raiz para baixo do setor (cuja cadeia de
        // ancestrais contem a propria raiz) criaria um ciclo.
        var raiz = UnidadeOrganizacional.CriarRaiz(Tenant, "PREF", "Prefeitura", TipoUnidade.Gabinete);
        var depto = UnidadeOrganizacional.CriarFilha(Tenant, "SMS", "Saude", TipoUnidade.Secretaria, raiz.Id);
        var setor = UnidadeOrganizacional.CriarFilha(Tenant, "SMS.VIG", "Vigilancia", TipoUnidade.Setor, depto.Id);

        // ancestrais do setor (candidato a pai): depto, raiz — a raiz aparece, logo seria ciclo.
        var ancestraisDoSetor = new[] { depto.Id, raiz.Id };

        var acao = () => raiz.DefinirPai(setor.Id, ancestraisDoSetor);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*ciclo*");
    }

    [Fact]
    public void DefinirPai_valido_reparenteia_e_mantem_conexidade()
    {
        var raiz = UnidadeOrganizacional.CriarRaiz(Tenant, "PREF", "Prefeitura", TipoUnidade.Gabinete);
        var outraRaiz = UnidadeOrganizacional.CriarRaiz(Tenant, "FMS", "Fundo de Saude", TipoUnidade.Fundo);
        var setor = UnidadeOrganizacional.CriarFilha(Tenant, "SMS.VIG", "Vigilancia", TipoUnidade.Setor, raiz.Id);

        // Mover o setor para baixo de outraRaiz (cuja cadeia de ancestrais nao contem o setor).
        setor.DefinirPai(outraRaiz.Id, ancestraisDoNovoPai: []);

        setor.UnidadePaiId.Should().Be(outraRaiz.Id);
        setor.DomainEvents.OfType<UnidadeOrganizacionalReparenteada>().Should().ContainSingle();
    }

    [Fact]
    public void Desativar_preserva_o_registro_e_e_idempotente()
    {
        var uo = UnidadeOrganizacional.CriarRaiz(Tenant, "PREF", "Prefeitura", TipoUnidade.Gabinete);

        uo.Desativar();
        uo.Desativar();

        uo.Ativa.Should().BeFalse();
        uo.DomainEvents.OfType<UnidadeOrganizacionalDesativada>().Should().ContainSingle();
    }
}
