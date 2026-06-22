using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Cargo"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Cargo.rules.md, sobre SQLite em memoria com auditoria
/// e isolamento por tenant.
/// </summary>
public sealed class CargoFluxoTests : RecursosHumanosTestBase
{
    private static Lotacao NovaLotacao()
        => Lotacao.Criar("12345678000190", "Secretaria de Administracao", "1010");

    private static Cargo NovoCargo(
        TipoCargo tipo = TipoCargo.Efetivo,
        decimal vencimento = 5000m,
        int vagas = 3,
        string denominacao = "Analista Administrativo",
        string lei = "Lei 1.000/2020")
        => Cargo.Criar(
            TenantA,
            denominacao,
            tipo,
            Vencimento.De(vencimento),
            NovaLotacao(),
            vagas,
            lei);

    // ---------- Invariantes ----------

    [Fact] // I-11 + Cenario 1: cargo efetivo nasce Ativo, RPPS, sem ocupantes e emite CargoCriado.
    public void Invariante_11_criacao_efetivo_nasce_ativo_rpps_e_emite_evento()
    {
        var cargo = NovoCargo(TipoCargo.Efetivo);

        cargo.Situacao.Should().Be(SituacaoCargo.Ativo);
        cargo.Regime.Should().Be(RegimePrevidenciario.Rpps);
        cargo.VagasOcupadas.Should().Be(0);
        cargo.DomainEvents.OfType<CargoCriado>().Should().ContainSingle();
    }

    [Fact] // I-2 + Cenario 2: cargo comissionado deriva RGPS.
    public void Invariante_2_comissionado_deriva_rgps()
    {
        var cargo = NovoCargo(TipoCargo.Comissionado);

        cargo.Regime.Should().Be(RegimePrevidenciario.Rgps);
    }

    [Fact] // I-2: cargo temporario deriva RGPS.
    public void Invariante_2_temporario_deriva_rgps()
    {
        var cargo = NovoCargo(TipoCargo.Temporario);

        cargo.Regime.Should().Be(RegimePrevidenciario.Rgps);
    }

    [Fact] // I-1 + Cenario 3 + B-3: criacao sem vagas (< 1) e rejeitada.
    public void Invariante_1_criacao_sem_vagas_e_rejeitada()
    {
        var acao = () => Cargo.Criar(
            TenantA, "Cargo", TipoCargo.Efetivo, Vencimento.De(1000m), NovaLotacao(), 0, "Lei");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-1 + B-1: criacao com denominacao vazia e rejeitada.
    public void Invariante_1_criacao_com_denominacao_vazia_e_rejeitada()
    {
        var acao = () => Cargo.Criar(
            TenantA, "  ", TipoCargo.Efetivo, Vencimento.De(1000m), NovaLotacao(), 1, "Lei");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-5 + Cenario 5: prover alem do quantitativo e rejeitado.
    public void Invariante_5_provimento_alem_do_quantitativo_e_rejeitado()
    {
        var cargo = NovoCargo(vagas: 1);
        cargo.Prover();

        var acao = cargo.Prover;

        acao.Should().Throw<InvalidOperationException>();
        cargo.VagasOcupadas.Should().Be(1);
    }

    [Fact] // I-6 + B-5: vagar sem ocupante e rejeitado.
    public void Invariante_6_vagar_sem_ocupante_e_rejeitado()
    {
        var cargo = NovoCargo();

        var acao = cargo.Vagar;

        acao.Should().Throw<InvalidOperationException>();
        cargo.VagasOcupadas.Should().Be(0);
    }

    [Fact] // I-7 + Cenario 8: reduzir vagas abaixo do ocupado e proibido.
    public void Invariante_7_reduzir_vagas_abaixo_do_ocupado_e_proibido()
    {
        var cargo = NovoCargo(vagas: 3);
        cargo.Prover();
        cargo.Prover();
        cargo.Prover();

        var acao = () => cargo.AlterarQuantidadeVagas(2);

        acao.Should().Throw<InvalidOperationException>();
        cargo.QuantidadeVagas.Should().Be(3);
    }

    [Fact] // B-7: alterar quantidade para exatamente as ocupadas e permitido.
    public void Borda_7_alterar_vagas_para_exatamente_ocupadas_e_permitido()
    {
        var cargo = NovoCargo(vagas: 3);
        cargo.Prover();
        cargo.Prover();

        cargo.AlterarQuantidadeVagas(2);

        cargo.QuantidadeVagas.Should().Be(2);
    }

    [Fact] // I-9 + Cenario 9: extincao com cargo ocupado e rejeitada.
    public void Invariante_9_extincao_com_cargo_ocupado_e_rejeitada()
    {
        var cargo = NovoCargo();
        cargo.Prover();

        var acao = () => cargo.Extinguir("Lei 2.000/2024");

        acao.Should().Throw<InvalidOperationException>();
        cargo.Situacao.Should().NotBe(SituacaoCargo.Extinto);
    }

    [Fact] // I-8 + Cenario 11 + B-8/B-9: cargo extinto e terminal.
    public void Invariante_8_cargo_extinto_e_terminal()
    {
        var cargo = NovoCargo(vagas: 1);
        cargo.Extinguir("Lei 3.000/2024");

        cargo.Situacao.Should().Be(SituacaoCargo.Extinto);
        ((Action)cargo.Prover).Should().Throw<InvalidOperationException>();
        ((Action)(() => cargo.AlterarVencimento(Vencimento.De(9000m)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => cargo.Extinguir("Lei 4.000/2024"))).Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 4: Ativo --Prover--> Ativo, incrementa VagasOcupadas e emite CargoProvido.
    public void Transicao_prover_incrementa_vagas_e_emite_evento()
    {
        var cargo = NovoCargo(vagas: 3);

        cargo.Prover();

        cargo.VagasOcupadas.Should().Be(1);
        cargo.Situacao.Should().Be(SituacaoCargo.Ativo);
        cargo.DomainEvents.OfType<CargoProvido>().Should().ContainSingle();
    }

    [Fact] // Cenario 6: Ativo (VagasOcupadas == 1) --Vagar--> Vago, emite CargoVago.
    public void Transicao_vagar_ultima_vaga_para_Vago_e_emite_evento()
    {
        var cargo = NovoCargo(vagas: 1);
        cargo.Prover();

        cargo.Vagar();

        cargo.VagasOcupadas.Should().Be(0);
        cargo.Situacao.Should().Be(SituacaoCargo.Vago);
        cargo.DomainEvents.OfType<CargoVago>().Should().ContainSingle();
    }

    [Fact] // B-6: vagar com mais de uma ocupada decrementa mas permanece Ativo (sem CargoVago).
    public void Transicao_vagar_com_remanescentes_mantem_Ativo_sem_evento()
    {
        var cargo = NovoCargo(vagas: 3);
        cargo.Prover();
        cargo.Prover();

        cargo.Vagar();

        cargo.VagasOcupadas.Should().Be(1);
        cargo.Situacao.Should().Be(SituacaoCargo.Ativo);
        cargo.DomainEvents.OfType<CargoVago>().Should().BeEmpty();
    }

    [Fact] // B-4: prover cargo Vago retorna a Ativo com VagasOcupadas == 1.
    public void Transicao_prover_cargo_vago_volta_para_Ativo()
    {
        var cargo = NovoCargo(vagas: 1);
        cargo.Prover();
        cargo.Vagar();
        cargo.Situacao.Should().Be(SituacaoCargo.Vago);

        cargo.Prover();

        cargo.Situacao.Should().Be(SituacaoCargo.Ativo);
        cargo.VagasOcupadas.Should().Be(1);
    }

    [Fact] // Cenario 7: AlterarVencimento atualiza valor e emite VencimentoAlterado.
    public void Transicao_alterar_vencimento_emite_evento()
    {
        var cargo = NovoCargo(vencimento: 5000m);

        cargo.AlterarVencimento(Vencimento.De(6500m));

        cargo.Vencimento.Valor.Should().Be(6500m);
        cargo.DomainEvents.OfType<VencimentoAlterado>().Should().ContainSingle()
            .Which.NovoVencimento.Should().Be(6500m);
    }

    [Fact] // Cenario 10: Vago --Extinguir--> Extinto, emite CargoExtinto.
    public void Transicao_extinguir_cargo_vago_para_Extinto()
    {
        var cargo = NovoCargo(vagas: 1);
        cargo.Prover();
        cargo.Vagar();

        cargo.Extinguir("Lei 5.000/2024");

        cargo.Situacao.Should().Be(SituacaoCargo.Extinto);
        cargo.DomainEvents.OfType<CargoExtinto>().Should().ContainSingle();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Persistencia: provimento persiste e registra auditoria no mesmo commit.
    public async Task Provimento_persiste_com_auditoria()
    {
        CargoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var cargo = NovoCargo(vagas: 2);
            id = cargo.Id;
            contexto.Cargos.Add(cargo);
            await contexto.SaveChangesAsync();

            cargo.Prover();
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var cargo = await contexto.Cargos.SingleAsync(c => c.Id == id);
            cargo.VagasOcupadas.Should().Be(1);
            cargo.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do cargo");
        }
    }

    [Fact] // Cenario 12: consulta de cargos e isolada por tenant (Global Query Filter).
    public async Task Cenario_12_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Cargos.Add(NovoCargo());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Cargos.ToListAsync()).Should().BeEmpty();
        }
    }
}
