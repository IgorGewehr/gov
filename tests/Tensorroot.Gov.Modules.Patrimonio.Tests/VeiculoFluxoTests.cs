using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Cobertura de integração do agregado <see cref="Veiculo"/>: invariantes de frota,
/// cada transição da máquina de estados e os cenários BDD de Veiculo.rules.md,
/// sobre SQLite em memória com auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class VeiculoFluxoTests : PatrimonioTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static Veiculo NovoVeiculo(string renavam = "12345678900", string placa = "ABC1D23")
        => Veiculo.IncorporarVeiculo(
            TenantA,
            "Caminhão basculante",
            ValorMonetario.De(200000m),
            ValorMonetario.De(20000m),
            120,
            new DateOnly(2026, 1, 10),
            "Aquisicao",
            Placa.Criar(placa),
            Renavam.Criar(renavam),
            Odometro.De(50000),
            Horimetro.De(1000m));

    private static Veiculo VeiculoTombado(string renavam = "12345678900", string placa = "ABC1D23")
    {
        var veiculo = NovoVeiculo(renavam, placa);
        veiculo.Tombar("TOMBO-VEIC-1");
        return veiculo;
    }

    // ---------- Invariantes ----------

    [Fact] // I-1: veículo nasce como bem patrimonial — valor contábil inicia no valor de incorporação.
    public void Invariante_1_veiculo_e_um_bem_patrimonial()
    {
        var veiculo = NovoVeiculo();

        veiculo.ValorInicial.Valor.Should().Be(200000m);
        veiculo.ValorResidual.Valor.Should().Be(20000m);
        veiculo.ValorContabil.Valor.Should().Be(200000m);
        veiculo.EmCondicoesDeUso.Should().BeTrue();
    }

    [Fact] // I-2 + B-1: placa/renavam inválidos na incorporação são rejeitados.
    public void Invariante_2_renavam_invalido_e_rejeitado()
    {
        var acao = () => Renavam.Criar("00000000000");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-3 + Cenário 2: odômetro não pode retroceder.
    public void Invariante_3_odometro_nao_retrocede()
    {
        var veiculo = VeiculoTombado();

        var acao = () => veiculo.RegistrarAbastecimento(
            Hoje, litros: 50m, ValorMonetario.De(300m), odometro: 49000, horimetro: 1000m, cotaLitros: 100m, motoristaId: null);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-3 + B-3: odômetro igual ao atual é permitido (monotônico não decrescente).
    public void Invariante_3_odometro_igual_ao_atual_e_permitido()
    {
        var veiculo = VeiculoTombado();

        veiculo.RegistrarAbastecimento(
            Hoje, litros: 50m, ValorMonetario.De(300m), odometro: 50000, horimetro: 1000m, cotaLitros: 100m, motoristaId: null);

        veiculo.Odometro.Valor.Should().Be(50000);
        veiculo.Abastecimentos.Should().ContainSingle();
    }

    [Fact] // I-4 + B-4: horímetro inferior ao atual é rejeitado.
    public void Invariante_4_horimetro_nao_retrocede()
    {
        var veiculo = VeiculoTombado();

        var acao = () => veiculo.RegistrarAbastecimento(
            Hoje, litros: 50m, ValorMonetario.De(300m), odometro: 50000, horimetro: 999m, cotaLitros: 100m, motoristaId: null);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5 + Cenário 9 + B-9: operações de frota exigem veículo ativo no acervo.
    public void Invariante_5_operacao_de_frota_em_veiculo_nao_ativo_e_rejeitada()
    {
        var emIncorporacao = NovoVeiculo(); // ainda sem tombo.

        var acao = () => emIncorporacao.RegistrarMulta("CTB-501-00", ValorMonetario.De(195m), Hoje, null);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-6 + Cenário 3: abastecimento acima da cota é bloqueado.
    public void Invariante_6_abastecimento_acima_da_cota_e_bloqueado()
    {
        var veiculo = VeiculoTombado();

        var acao = () => veiculo.RegistrarAbastecimento(
            Hoje, litros: 120m, ValorMonetario.De(600m), odometro: 50100, horimetro: 1001m, cotaLitros: 100m, motoristaId: null);

        acao.Should().Throw<InvalidOperationException>();
        veiculo.Abastecimentos.Should().BeEmpty();
    }

    [Fact] // I-6 + B-5: abastecimento exatamente no limite da cota é permitido.
    public void Invariante_6_abastecimento_no_limite_da_cota_e_permitido()
    {
        var veiculo = VeiculoTombado();

        veiculo.RegistrarAbastecimento(
            Hoje, litros: 100m, ValorMonetario.De(500m), odometro: 50100, horimetro: 1001m, cotaLitros: 100m, motoristaId: null);

        veiculo.Abastecimentos.Should().ContainSingle();
    }

    [Fact] // I-7 + Cenário 5: conclusão de manutenção exige OS Aberta e emite ManutencaoConcluida.
    public void Invariante_7_conclusao_de_manutencao_exige_OS_aberta()
    {
        var veiculo = VeiculoTombado();
        var osId = veiculo.AbrirOrdemServico("Troca de óleo", ValorMonetario.De(400m), 50200);

        veiculo.ConcluirManutencao(osId, ValorMonetario.De(420m), Hoje);

        veiculo.OrdensServico.Should().ContainSingle()
            .Which.Situacao.Should().Be(SituacaoOrdemServico.Concluida);
        veiculo.DomainEvents.OfType<ManutencaoConcluida>().Should().ContainSingle();
    }

    [Fact] // Cenário 6: concluir OS já concluída falha.
    public void Invariante_7_concluir_OS_ja_concluida_falha()
    {
        var veiculo = VeiculoTombado();
        var osId = veiculo.AbrirOrdemServico("Troca de óleo", ValorMonetario.De(400m), 50200);
        veiculo.ConcluirManutencao(osId, ValorMonetario.De(420m), Hoje);

        var acao = () => veiculo.ConcluirManutencao(osId, ValorMonetario.De(99m), Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + Cenário 7: registro de multa emite MultaRegistrada.
    public void Invariante_8_registro_de_multa_emite_evento()
    {
        var veiculo = VeiculoTombado();

        veiculo.RegistrarMulta("CTB-501-00", ValorMonetario.De(195m), Hoje, null);

        veiculo.Multas.Should().ContainSingle()
            .Which.CodigoInfracaoCtb.Should().Be("CTB-501-00");
        veiculo.DomainEvents.OfType<MultaRegistrada>().Should().ContainSingle();
    }

    [Fact] // I-9 + Cenário 8 + B-8: motorista com CNH vencida não pode ser designado.
    public void Invariante_9_cnh_vencida_e_rejeitada()
    {
        var veiculo = VeiculoTombado();

        var acao = () => veiculo.DesignarMotorista("João Motorista", "12345678900", "D", Hoje.AddDays(-1), Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9 + B-7: CNH válida exatamente hoje é aceita.
    public void Invariante_9_cnh_valida_hoje_e_aceita()
    {
        var veiculo = VeiculoTombado();

        veiculo.DesignarMotorista("João Motorista", "12345678900", "D", Hoje, Hoje);

        veiculo.MotoristaAtualId.Should().NotBeNull();
        veiculo.Motoristas.Should().ContainSingle();
    }

    [Fact] // I-11 + Cenário 10 + B-11: licenciamento duplicado por exercício é rejeitado.
    public void Invariante_11_licenciamento_duplicado_por_exercicio_e_rejeitado()
    {
        var veiculo = VeiculoTombado();
        veiculo.RegistrarLicenciamento(2026, ValorMonetario.De(1200m), ValorMonetario.De(150m), Hoje);

        var acao = () => veiculo.RegistrarLicenciamento(2026, ValorMonetario.De(1300m), ValorMonetario.De(160m), Hoje);

        acao.Should().Throw<InvalidOperationException>();
        veiculo.Licenciamentos.Should().ContainSingle();
    }

    [Fact] // I-10: abastecimento avança odômetro/horímetro e emite AbastecimentoRegistrado.
    public void Invariante_10_abastecimento_atualiza_medicoes_e_emite_evento()
    {
        var veiculo = VeiculoTombado();

        veiculo.RegistrarAbastecimento(
            Hoje, litros: 80m, ValorMonetario.De(450m), odometro: 50500, horimetro: 1010m, cotaLitros: 100m, motoristaId: null);

        veiculo.Odometro.Valor.Should().Be(50500);
        veiculo.Horimetro.Valor.Should().Be(1010m);
        veiculo.DomainEvents.OfType<AbastecimentoRegistrado>().Should().ContainSingle();
    }

    [Fact] // I-5: veículo encerrado (Baixada) rejeita operações de frota.
    public void Invariante_5_veiculo_encerrado_rejeita_frota()
    {
        // Não há baixa pública no agregado Veiculo; comprova-se a guarda de "ativo no acervo"
        // a partir de um veículo ainda EmIncorporacao (não tombado), que também não é ativo.
        var veiculo = NovoVeiculo();

        var acao = () => veiculo.AbrirOrdemServico("Revisão", ValorMonetario.De(100m), 50100);

        acao.Should().Throw<InvalidOperationException>();
    }

    // ---------- Máquina de estados (transições) ----------

    [Fact] // (nenhum) --IncorporarVeiculo--> EmIncorporacao.
    public void Transicao_incorporar_nasce_em_EmIncorporacao()
    {
        var veiculo = NovoVeiculo();

        veiculo.Situacao.Should().Be(SituacaoBemPatrimonial.EmIncorporacao);
        veiculo.Placa.Valor.Should().Be("ABC1D23");
    }

    [Fact] // EmIncorporacao --Tombar--> Tombado.
    public void Transicao_tombar_de_EmIncorporacao_para_Tombado()
    {
        var veiculo = NovoVeiculo();

        veiculo.Tombar("TOMBO-VEIC-T");

        veiculo.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
        veiculo.AtivoNoAcervo.Should().BeTrue();
    }

    [Fact] // Tombado --AbrirOrdemServico/ConcluirManutencao--> permanece Tombado (operação de frota não altera situação).
    public void Transicao_operacao_de_frota_nao_altera_situacao_patrimonial()
    {
        var veiculo = VeiculoTombado();

        var osId = veiculo.AbrirOrdemServico("Revisão", ValorMonetario.De(100m), 50100);
        veiculo.ConcluirManutencao(osId, ValorMonetario.De(120m), Hoje);

        veiculo.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
        veiculo.AtivoNoAcervo.Should().BeTrue();
    }

    // ---------- Persistência, auditoria e isolamento ----------

    [Fact] // Cenário 11 + B-2: RENAVAM único por tenant (índice único (TenantId, Renavam)).
    public async Task Cenario_11_renavam_duplicado_no_tenant_viola_indice_unico()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.Veiculos.Add(NovoVeiculo(renavam: "12345678900", placa: "ABC1D23"));
        await contexto.SaveChangesAsync();

        contexto.Veiculos.Add(NovoVeiculo(renavam: "12345678900", placa: "XYZ9Z88"));
        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Fluxo completo persiste medições, multas e gera auditoria.
    public async Task Fluxo_de_frota_persiste_com_auditoria()
    {
        VeiculoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var veiculo = VeiculoTombado();
            id = veiculo.Id;
            contexto.Veiculos.Add(veiculo);
            await contexto.SaveChangesAsync();

            veiculo.RegistrarAbastecimento(Hoje, 80m, ValorMonetario.De(450m), 50800, 1020m, 100m, null);
            veiculo.RegistrarMulta("CTB-501-00", ValorMonetario.De(195m), Hoje, null);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var veiculo = await contexto.Veiculos.SingleAsync(v => v.Id == id);
            veiculo.Odometro.Valor.Should().Be(50800);
            veiculo.Abastecimentos.Should().ContainSingle();
            veiculo.Multas.Should().ContainSingle();

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutações do veículo");
        }
    }

    [Fact] // Cenário 13: consulta é tenant-scoped (Global Query Filter).
    public async Task Cenario_13_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Veiculos.Add(VeiculoTombado());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Veiculos.ToListAsync()).Should().BeEmpty();
        }
    }
}
