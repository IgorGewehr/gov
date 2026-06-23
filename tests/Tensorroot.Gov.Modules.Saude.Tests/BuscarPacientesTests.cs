using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura da NAVEGABILIDADE (Onda 0): busca paginada de pacientes por nome (sombra normalizada),
/// CNS e CPF (digitos), com isolamento por tenant (Global Query Filter), sobre SQLite em memoria.
/// </summary>
public sealed class BuscarPacientesTests : SaudeTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    [Fact]
    public async Task Busca_por_nome_e_acento_insensivel()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Pacientes.Add(NovoPaciente(CnsValido, "João da Silva", "52998224725"));
            contexto.Pacientes.Add(NovoPaciente(CnsValido2, "Maria Souza", null));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new PacienteRepository(contexto);
            // termo sem acento e em minusculas deve casar "João" (coluna-sombra normalizada).
            var (itens, total) = await repo.BuscarAsync("joao", null, 1, 10, default);

            total.Should().Be(1);
            itens.Single().Identificacao.Nome.Should().Be("João da Silva");
        }
    }

    [Fact]
    public async Task Busca_por_cns_e_por_cpf_funciona()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Pacientes.Add(NovoPaciente(CnsValido, "João da Silva", "52998224725"));
            contexto.Pacientes.Add(NovoPaciente(CnsValido2, "Maria Souza", null));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new PacienteRepository(contexto);

            var porCns = await repo.BuscarAsync(CnsValido, null, 1, 10, default);
            porCns.Total.Should().Be(1);
            porCns.Itens.Single().Cns.Valor.Should().Be(CnsValido);

            var porCpf = await repo.BuscarAsync("52998224725", null, 1, 10, default);
            porCpf.Total.Should().Be(1);
            porCpf.Itens.Single().Identificacao.Nome.Should().Be("João da Silva");
        }
    }

    [Fact]
    public async Task Busca_e_isolada_por_tenant_e_filtra_por_situacao()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            var ativo = NovoPaciente(CnsValido, "João da Silva", "52998224725");
            var inativo = NovoPaciente(CnsValido2, "João Pereira", null);
            inativo.Inativar("Transferencia");
            contexto.Pacientes.Add(ativo);
            contexto.Pacientes.Add(inativo);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            contexto.Pacientes.Add(NovoPaciente(CnsValido, "João do Tenant B", null));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new PacienteRepository(contexto);

            var todos = await repo.BuscarAsync("joao", null, 1, 10, default);
            todos.Total.Should().Be(2, "tenant A enxerga apenas os seus dois pacientes 'João'");

            var apenasAtivos = await repo.BuscarAsync("joao", SituacaoPaciente.Ativo, 1, 10, default);
            apenasAtivos.Total.Should().Be(1);
            apenasAtivos.Itens.Single().Situacao.Should().Be(SituacaoPaciente.Ativo);
        }
    }

    private static Paciente NovoPaciente(string cns, string nome, string? cpf)
        => Paciente.Cadastrar(
            TenantA,
            new Cns(cns),
            new Identificacao(nome, new DateOnly(1990, 5, 10), Sexo.Masculino, null, cpf is null ? null : Cpf.Create(cpf), Hoje),
            new Endereco("Rua A", "1", "Centro", "Maximiliano de Almeida", "RS", "99970000"));
}
