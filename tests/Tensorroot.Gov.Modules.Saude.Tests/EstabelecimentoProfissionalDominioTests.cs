using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Testes-chave de dominio dos cadastros-mestre da Saude (Onda 1): <see cref="Estabelecimento"/> (CNES)
/// e <see cref="Profissional"/> (CBO/CRM). Cobrem o cadastro valido, o CNES invalido, a inativacao que
/// bloqueia novos vinculos, o vinculo CBO unico ativo, a teleconsulta exigindo CRM ativo (I-9) e a
/// vigencia do vinculo. Dominio puro — sem persistencia.
/// </summary>
public sealed class EstabelecimentoProfissionalDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Endereco Endereco()
        => new("Av. Brasil", "200", "Centro", "Maximiliano de Almeida", "RS", "99880000");

    [Fact]
    public void Cadastrar_estabelecimento_valido_nasce_ativo_e_emite_evento()
    {
        var ubs = Estabelecimento.Cadastrar(Tenant, new CodigoCnes("1234567"), "UBS Central", TipoEstabelecimento.Ubs, Endereco());

        ubs.EstaAtivo.Should().BeTrue();
        ubs.Cnes.Valor.Should().Be("1234567");
        ubs.DomainEvents.Should().ContainSingle(e => e is EstabelecimentoCadastrado);
    }

    [Fact]
    public void Cnes_invalido_deve_rejeitar()
    {
        var acao = () => new CodigoCnes("12");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Estabelecimento_inativado_bloqueia_atualizacao()
    {
        var ubs = Estabelecimento.Cadastrar(Tenant, new CodigoCnes("1234567"), "UBS Central", TipoEstabelecimento.Ubs, Endereco());
        ubs.Inativar();

        ubs.EstaAtivo.Should().BeFalse();
        var acao = () => ubs.AtualizarDados("Novo", TipoEstabelecimento.Ubs, Endereco());
        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cadastrar_profissional_valido_nasce_ativo()
    {
        var prof = Profissional.Cadastrar(Tenant, Cpf.Create("52998224725"), "Dra. Ana", null, new RegistroConselho(TipoConselho.Crm, "RS", "12345"));

        prof.Situacao.Should().Be(SituacaoProfissional.Ativo);
        prof.TemCrmAtivo.Should().BeTrue("profissional com CRM ativo habilita teleconsulta (I-9)");
        prof.DomainEvents.Should().ContainSingle(e => e is ProfissionalCadastrado);
    }

    [Fact]
    public void Profissional_sem_crm_nao_habilita_teleconsulta_I9()
    {
        var prof = Profissional.Cadastrar(Tenant, Cpf.Create("52998224725"), "Enf. Joana", null, new RegistroConselho(TipoConselho.Coren, "RS", "9999"));

        prof.TemCrmAtivo.Should().BeFalse();
    }

    [Fact]
    public void Vincular_duplicado_estabelecimento_e_cbo_deve_rejeitar()
    {
        var prof = Profissional.Cadastrar(Tenant, Cpf.Create("52998224725"), "Dra. Ana", null, null);
        var estab = EstabelecimentoId.New();
        var cbo = new Cbo("225125");
        prof.Vincular(estab, cbo, new DateOnly(2026, 1, 1));

        var acao = () => prof.Vincular(estab, cbo, new DateOnly(2026, 2, 1));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Vinculo_ativo_reflete_vigencia_e_encerramento()
    {
        var prof = Profissional.Cadastrar(Tenant, Cpf.Create("52998224725"), "Dra. Ana", null, null);
        var estab = EstabelecimentoId.New();
        prof.Vincular(estab, new Cbo("225125"), new DateOnly(2026, 1, 1));

        prof.TemVinculoAtivo(estab, new DateOnly(2026, 6, 1)).Should().BeTrue();

        prof.EncerrarVinculo(estab, new DateOnly(2026, 3, 31));

        prof.TemVinculoAtivo(estab, new DateOnly(2026, 6, 1)).Should().BeFalse();
        prof.DomainEvents.Should().Contain(e => e is VinculoProfissionalEncerrado);
    }
}
