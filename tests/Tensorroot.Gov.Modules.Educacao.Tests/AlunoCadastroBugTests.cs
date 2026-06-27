using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Cobertura de borda do bug de cadastro de aluno com CPF (I-A4 — unicidade por CPF no tenant):
/// a checagem de CPF duplicado precisa ser TRADUZIVEL para SQL. O CPF e mapeado por value converter
/// (VO -&gt; string), de modo que comparar a coluna via igualdade do VO inteiro traduz, enquanto navegar
/// em <c>Cpf.Digitos</c> NAO traduz (a propriedade do VO nao vira coluna) e estoura na execucao da query.
/// Os testes exercitam o <see cref="AlunoRepository"/> real sobre SQLite (onde a traducao acontece).
/// </summary>
public sealed class AlunoCadastroBugTests : EducacaoTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 20);

    // CPF valido usado como fixture nos testes de dominio do modulo.
    private const string CpfValido = "52998224725";

    private static Aluno NovoAlunoMaiorComCpf(Guid tenant, string cpf)
        => Aluno.Cadastrar(
            tenant,
            new DadosCivis("Maria da Silva", new DateOnly(2000, 5, 10), Sexo.Feminino, "Joana da Silva", null, null, Hoje),
            new EnderecoAluno("Rua das Flores", "100", "Centro", "Maximiliano de Almeida", "RS", "99880000"),
            Cpf.Create(cpf),
            [],
            Hoje);

    [Fact] // Bug: cadastrar aluno com CPF nao pode estourar na checagem de unicidade — a query deve traduzir.
    public async Task ExisteCpf_quando_ainda_nao_cadastrado_traduz_e_retorna_falso()
    {
        await using var contexto = CriarContexto(TenantA);
        var repositorio = new AlunoRepository(contexto);

        // Antes do fix, navegar em Cpf.Digitos lancava InvalidOperationException (falha de traducao LINQ->SQL).
        var existe = await repositorio.ExisteCpfAsync(Cpf.Create(CpfValido), CancellationToken.None);

        existe.Should().BeFalse("nenhum aluno com esse CPF foi cadastrado ainda");
    }

    [Fact] // O aluno com CPF entra (persiste) e passa a ser detectado pela checagem de unicidade.
    public async Task Aluno_com_cpf_entra_e_passa_a_ser_detectado_como_existente()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Alunos.Add(NovoAlunoMaiorComCpf(TenantA, CpfValido));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repositorio = new AlunoRepository(contexto);
            var existe = await repositorio.ExisteCpfAsync(Cpf.Create(CpfValido), CancellationToken.None);

            existe.Should().BeTrue("o aluno com esse CPF ja foi persistido no tenant");
        }
    }

    [Fact] // I-A4 e por tenant: o mesmo CPF noutro tenant nao conta como duplicado (Global Query Filter).
    public async Task Unicidade_de_cpf_e_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Alunos.Add(NovoAlunoMaiorComCpf(TenantA, CpfValido));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            var repositorio = new AlunoRepository(contexto);
            var existe = await repositorio.ExisteCpfAsync(Cpf.Create(CpfValido), CancellationToken.None);

            existe.Should().BeFalse("a unicidade de CPF e isolada por tenant");
        }
    }

    [Fact] // A busca por termo de CPF tambem deve traduzir (mesmo VO mapeado por conversor): casa por igualdade exata.
    public async Task Buscar_por_cpf_completo_traduz_e_encontra_o_aluno()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Alunos.Add(NovoAlunoMaiorComCpf(TenantA, CpfValido));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repositorio = new AlunoRepository(contexto);
            var (itens, total) = await repositorio.BuscarAsync(CpfValido, null, 1, 20, CancellationToken.None);

            total.Should().Be(1);
            itens.Should().ContainSingle();
        }
    }
}
