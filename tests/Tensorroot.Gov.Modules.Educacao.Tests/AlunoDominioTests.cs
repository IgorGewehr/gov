using FluentAssertions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Testes-chave de dominio do agregado <see cref="Aluno"/> (Onda 1): cadastro valido e invariantes
/// I-A1 (data de nascimento nao futura), I-A2 (menor exige responsavel), I-A3 (nome da mae) e I-A5
/// (estado terminal bloqueia alteracao). Dominio puro — sem persistencia.
/// </summary>
public sealed class AlunoDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Hoje = new(2026, 6, 20);

    private static DadosCivis DadosDeMaior()
        => new("Maria da Silva", new DateOnly(2000, 5, 10), Sexo.Feminino, "Joana da Silva", null, null, Hoje);

    private static DadosCivis DadosDeMenor()
        => new("Pedro Souza", new DateOnly(2018, 3, 1), Sexo.Masculino, "Ana Souza", null, null, Hoje);

    private static EnderecoAluno Endereco()
        => new("Rua das Flores", "100", "Centro", "Maximiliano de Almeida", "RS", "99880000");

    private static Responsavel ResponsavelMae()
        => Responsavel.Criar("Ana Souza", null, Parentesco.Mae, "5199990000", true, true);

    [Fact]
    public void Cadastrar_aluno_maior_sem_responsavel_deve_nascer_ativo_e_emitir_evento()
    {
        var aluno = Aluno.Cadastrar(Tenant, DadosDeMaior(), Endereco(), Cpf.Create("52998224725"), [], Hoje);

        aluno.Situacao.Should().Be(SituacaoAluno.Ativo);
        aluno.Ativo.Should().BeTrue();
        aluno.TenantId.Should().Be(Tenant);
        aluno.DomainEvents.Should().ContainSingle(e => e is AlunoCadastrado);
    }

    [Fact]
    public void Cadastrar_menor_sem_responsavel_deve_rejeitar_IA2()
    {
        var acao = () => Aluno.Cadastrar(Tenant, DadosDeMenor(), Endereco(), null, [], Hoje);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*responsavel*");
    }

    [Fact]
    public void Cadastrar_menor_com_responsavel_deve_ser_aceito_IA2()
    {
        var aluno = Aluno.Cadastrar(Tenant, DadosDeMenor(), Endereco(), null, [ResponsavelMae()], Hoje);

        aluno.Responsaveis.Should().ContainSingle();
        aluno.Situacao.Should().Be(SituacaoAluno.Ativo);
    }

    [Fact]
    public void DadosCivis_com_data_futura_deve_rejeitar_IA1()
    {
        var acao = () => new DadosCivis("Futuro", Hoje.AddDays(1), Sexo.Ignorado, "Mae", null, null, Hoje);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DadosCivis_sem_nome_da_mae_deve_rejeitar_IA3()
    {
        var acao = () => new DadosCivis("Aluno", new DateOnly(2010, 1, 1), Sexo.Masculino, "  ", null, null, Hoje);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Inativar_e_terminal_e_bloqueia_alteracao_IA5()
    {
        var aluno = Aluno.Cadastrar(Tenant, DadosDeMaior(), Endereco(), null, [], Hoje);
        aluno.Inativar("evasao definitiva");

        aluno.Situacao.Should().Be(SituacaoAluno.Inativo);
        var acao = () => aluno.AtualizarDados(DadosDeMaior(), Endereco());
        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdicionarResponsavel_duplicado_por_parentesco_e_cpf_deve_rejeitar()
    {
        var aluno = Aluno.Cadastrar(Tenant, DadosDeMaior(), Endereco(), null, [], Hoje);
        var cpf = Cpf.Create("52998224725");
        aluno.AdicionarResponsavel(Responsavel.Criar("Mae A", cpf, Parentesco.Mae, null, true, true));

        var acao = () => aluno.AdicionarResponsavel(Responsavel.Criar("Mae A", cpf, Parentesco.Mae, null, true, true));

        acao.Should().Throw<InvalidOperationException>();
    }
}
