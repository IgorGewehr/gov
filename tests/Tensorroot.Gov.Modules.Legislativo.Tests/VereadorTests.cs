using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura do agregado <see cref="Vereador"/>: invariantes do cadastro (nome/partido/legislatura),
/// maquina de situacao de mandato, vinculo ao <see cref="VereadorId"/> existente e isolamento por tenant.
/// </summary>
public sealed class VereadorTests : LegislativoTestBase
{
    private static Vereador NovoVereador(string nome = "Ana Paula", string partido = "PSDB")
        => Vereador.Cadastrar(TenantA, nome, nome, partido, 2025, 2028, CargoMesa.Presidente);

    [Fact] // V-1: cadastro nasce EmExercicio e emite VereadorCadastrado.
    public void Cadastro_nasce_em_exercicio_e_emite_evento()
    {
        var vereador = NovoVereador();

        vereador.Situacao.Should().Be(SituacaoVereador.EmExercicio);
        vereador.Partido.Should().Be("PSDB");
        vereador.DomainEvents.OfType<VereadorCadastrado>().Should().ContainSingle();
    }

    [Fact] // V-2: nome civil vazio e rejeitado.
    public void Cadastro_nome_vazio_e_rejeitado()
    {
        var acao = () => Vereador.Cadastrar(TenantA, " ", "X", "PT", 2025, 2028);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // V-2: partido vazio e rejeitado.
    public void Cadastro_partido_vazio_e_rejeitado()
    {
        var acao = () => Vereador.Cadastrar(TenantA, "Fulano", "Fulano", " ", 2025, 2028);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // V-3: legislatura implausivel (fim <= inicio) e rejeitada.
    public void Cadastro_legislatura_invalida_e_rejeitada()
    {
        var acao = () => Vereador.Cadastrar(TenantA, "Fulano", "Fulano", "PT", 2028, 2025);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // Reuso de VereadorId existente (vinculo a presencas/votos preexistentes sem quebrar o modelo).
    public void Cadastro_reusa_vereador_id_informado()
    {
        var id = VereadorId.New();

        var vereador = Vereador.Cadastrar(TenantA, "Fulano", "Fulano", "PT", 2025, 2028, id: id);

        vereador.Id.Should().Be(id);
    }

    [Fact] // V-4: edicao atualiza nome/partido/cargo e normaliza a sigla.
    public void Edicao_atualiza_cadastro()
    {
        var vereador = NovoVereador();

        vereador.AtualizarCadastro("Ana Paula Rodrigues", "Ana P.", "pt", CargoMesa.Nenhum);

        vereador.NomeCivil.Should().Be("Ana Paula Rodrigues");
        vereador.Partido.Should().Be("PT");
        vereador.CargoMesa.Should().Be(CargoMesa.Nenhum);
    }

    [Fact] // V-5: Encerrado e terminal — nao admite nova alteracao.
    public void Situacao_encerrada_e_terminal()
    {
        var vereador = NovoVereador();
        vereador.AlterarSituacao(SituacaoVereador.Encerrado);

        vereador.Terminal.Should().BeTrue();
        ((Action)(() => vereador.AlterarSituacao(SituacaoVereador.EmExercicio)))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => vereador.AtualizarCadastro("X", "X", "PT", CargoMesa.Nenhum)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // Persistencia + isolamento por tenant (Global Query Filter) + auditoria.
    public async Task Persiste_com_auditoria_e_isolamento_por_tenant()
    {
        VereadorId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var vereador = NovoVereador();
            id = vereador.Id;
            contexto.Vereadores.Add(vereador);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            (await contexto.Vereadores.SingleAsync(v => v.Id == id)).NomeParlamentar.Should().Be("Ana Paula");
            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Vereadores.ToListAsync()).Should().BeEmpty();
        }
    }
}
