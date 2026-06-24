using FluentAssertions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Cobertura do Painel de Frota (ONDA3-DESIGN §3.2, sub-onda 3a) sobre o agregado <see cref="Veiculo"/>
/// ja rico: agregacao de custo (combustivel + manutencao + multas), consumo medio km/L (so determinavel
/// com >= 2 abastecimentos), CNH a vencer/vencida e manutencoes abertas. Read models puros — sem dominio
/// novo. Roda sobre SQLite em memoria, atravessando o repositorio EF e o handler de aplicacao, com o
/// Global Query Filter por tenant ativo.
/// </summary>
public sealed class PainelFrotaTests : PatrimonioTestBase
{
    private static readonly DateOnly InicioPeriodo = new(2026, 6, 1);
    private static readonly DateOnly FimPeriodo = new(2026, 6, 30);

    private static Veiculo VeiculoTombado(string renavam, string placa)
    {
        var veiculo = Veiculo.IncorporarVeiculo(
            TenantA,
            "Caminhao basculante",
            ValorMonetario.De(200000m),
            ValorMonetario.De(20000m),
            120,
            new DateOnly(2026, 1, 10),
            "Aquisicao",
            Placa.Criar(placa),
            Renavam.Criar(renavam),
            Odometro.De(50000),
            Horimetro.De(1000m));
        veiculo.Tombar($"TOMBO-{placa}");
        return veiculo;
    }

    [Fact] // Consumo medio exige >= 2 abastecimentos: 1 abastecimento => consumo indeterminado.
    public async Task Consumo_com_um_unico_abastecimento_e_indeterminado()
    {
        var veiculo = VeiculoTombado("12345678900", "ABC1D23");
        veiculo.RegistrarAbastecimento(InicioPeriodo, 50m, ValorMonetario.De(300m), 50500, 1010m, 100m, null);

        await using var contexto = CriarContexto(TenantA);
        contexto.Veiculos.Add(veiculo);
        await contexto.SaveChangesAsync();

        var handler = new ObterCustoPorVeiculoHandler(new VeiculoRepository(contexto));
        var custo = await handler.Handle(
            new ObterCustoPorVeiculoQuery(veiculo.Id.Value, InicioPeriodo, FimPeriodo), default);

        custo.Should().NotBeNull();
        custo!.LitrosAbastecidos.Should().Be(50m);
        custo.GastoCombustivel.Should().Be(300m);
        custo.KmRodados.Should().Be(0, "com um unico abastecimento nao ha intervalo de odometro");
        custo.ConsumoMedioKmL.Should().BeNull("consumo so e determinavel com >= 2 abastecimentos");
    }

    [Fact] // Consumo medio tank-to-tank: (maxOdometro - minOdometro) / litros DO 2o abastecimento em diante.
    public async Task Consumo_com_dois_abastecimentos_calcula_km_por_litro()
    {
        var veiculo = VeiculoTombado("12345678900", "ABC1D23");
        // 50000 -> 50400 = 400 km rodados. O 1o abastecimento (50 L @ 50000) apenas fixa o odometro
        // inicial — seu combustivel queimou ANTES do trecho medido. O 2o abastecimento (50 L @ 50400)
        // reabasteceu os 400 km => 400 km / 50 L = 8,0 km/L.
        veiculo.RegistrarAbastecimento(InicioPeriodo, 50m, ValorMonetario.De(300m), 50000, 1010m, 100m, null);
        veiculo.RegistrarAbastecimento(InicioPeriodo.AddDays(5), 50m, ValorMonetario.De(300m), 50400, 1020m, 100m, null);

        await using var contexto = CriarContexto(TenantA);
        contexto.Veiculos.Add(veiculo);
        await contexto.SaveChangesAsync();

        var handler = new ObterCustoPorVeiculoHandler(new VeiculoRepository(contexto));
        var custo = await handler.Handle(
            new ObterCustoPorVeiculoQuery(veiculo.Id.Value, InicioPeriodo, FimPeriodo), default);

        custo.Should().NotBeNull();
        custo!.LitrosAbastecidos.Should().Be(100m, "litros totais abastecidos no periodo (lado custo) somam os dois");
        custo.KmRodados.Should().Be(400);
        custo.ConsumoMedioKmL.Should().Be(8.0m, "400 km / 50 L (litros do 2o abastecimento) = 8,0 km/L");
    }

    [Fact] // Painel agrega custo total = combustivel + manutencao (OS concluida no periodo) + multas.
    public async Task Painel_agrega_combustivel_manutencao_e_multas_no_periodo()
    {
        var veiculo = VeiculoTombado("12345678900", "ABC1D23");
        veiculo.RegistrarAbastecimento(InicioPeriodo, 50m, ValorMonetario.De(300m), 50000, 1010m, 100m, null);
        veiculo.RegistrarAbastecimento(InicioPeriodo.AddDays(5), 50m, ValorMonetario.De(300m), 50400, 1020m, 100m, null);
        var os = veiculo.AbrirOrdemServico("Revisao", ValorMonetario.De(400m), 50410);
        veiculo.ConcluirManutencao(os, ValorMonetario.De(450m), InicioPeriodo.AddDays(10));
        veiculo.RegistrarMulta("CTB-501-00", ValorMonetario.De(195m), InicioPeriodo.AddDays(12), null);

        await using var contexto = CriarContexto(TenantA);
        contexto.Veiculos.Add(veiculo);
        await contexto.SaveChangesAsync();

        var handler = new ObterPainelFrotaHandler(new VeiculoRepository(contexto));
        var painel = await handler.Handle(new ObterPainelFrotaQuery(InicioPeriodo, FimPeriodo), default);

        painel.QuantidadeVeiculos.Should().Be(1);
        painel.GastoCombustivel.Should().Be(600m);
        painel.GastoManutencao.Should().Be(450m, "OS concluida no periodo entra pelo custo realizado");
        painel.GastoMultas.Should().Be(195m);
        painel.GastoTotal.Should().Be(600m + 450m + 195m);
        painel.LitrosTotais.Should().Be(100m);
        painel.ConsumoMedioFrotaKmL.Should().Be(8.0m, "tank-to-tank: 400 km / 50 L (2o abastecimento) = 8,0 km/L");
    }

    [Fact] // Custos fora do periodo nao entram na agregacao (filtro de datas).
    public async Task Painel_ignora_custos_fora_do_periodo()
    {
        var veiculo = VeiculoTombado("12345678900", "ABC1D23");
        // abastecimento em maio (fora do periodo de junho).
        veiculo.RegistrarAbastecimento(new DateOnly(2026, 5, 20), 50m, ValorMonetario.De(300m), 50000, 1010m, 100m, null);

        await using var contexto = CriarContexto(TenantA);
        contexto.Veiculos.Add(veiculo);
        await contexto.SaveChangesAsync();

        var handler = new ObterPainelFrotaHandler(new VeiculoRepository(contexto));
        var painel = await handler.Handle(new ObterPainelFrotaQuery(InicioPeriodo, FimPeriodo), default);

        painel.QuantidadeVeiculos.Should().Be(0, "nenhum veiculo teve custo dentro do periodo");
        painel.GastoTotal.Should().Be(0m);
    }

    [Fact] // CNH vencendo dentro da janela e CNH vencida sao listadas; CNH distante nao.
    public async Task Cnh_vencendo_lista_dentro_da_janela_e_vencidas()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var comCnhProxima = VeiculoTombado("22222222205", "AAA1A11");
        comCnhProxima.DesignarMotorista("Joao Proximo", "11111111111", "D", hoje.AddDays(10), hoje);

        var comCnhVencida = VeiculoTombado("33333333302", "BBB2B22");
        // designa com CNH valida hoje e nao ha como vencer no agregado; usa validade = hoje (no limite).
        comCnhVencida.DesignarMotorista("Maria Limite", "22222222222", "D", hoje, hoje);

        var comCnhDistante = VeiculoTombado("98765432103", "CCC3C33");
        comCnhDistante.DesignarMotorista("Carlos Distante", "33333333333", "D", hoje.AddDays(365), hoje);

        await using var contexto = CriarContexto(TenantA);
        contexto.Veiculos.Add(comCnhProxima);
        contexto.Veiculos.Add(comCnhVencida);
        contexto.Veiculos.Add(comCnhDistante);
        await contexto.SaveChangesAsync();

        var handler = new ListarCnhVencendoHandler(new VeiculoRepository(contexto));
        var lista = await handler.Handle(new ListarCnhVencendoQuery(30), default);

        lista.Select(c => c.Nome).Should().Contain("Joao Proximo").And.Contain("Maria Limite");
        lista.Should().NotContain(c => c.Nome == "Carlos Distante", "CNH a 365 dias esta fora da janela de 30 dias");
        lista.Single(c => c.Nome == "Joao Proximo").DiasParaVencer.Should().Be(10);
    }

    [Fact] // Manutencoes abertas listam apenas OS Aberta; OS concluida nao aparece.
    public async Task Manutencoes_abertas_listam_so_OS_aberta()
    {
        var veiculo = VeiculoTombado("12345678900", "ABC1D23");
        var aberta = veiculo.AbrirOrdemServico("Troca de pneus", ValorMonetario.De(800m), 50500);
        var paraConcluir = veiculo.AbrirOrdemServico("Alinhamento", ValorMonetario.De(120m), 50510);
        veiculo.ConcluirManutencao(paraConcluir, ValorMonetario.De(130m), FimPeriodo);

        await using var contexto = CriarContexto(TenantA);
        contexto.Veiculos.Add(veiculo);
        await contexto.SaveChangesAsync();

        var handler = new ListarManutencoesAbertasHandler(new VeiculoRepository(contexto));
        var lista = await handler.Handle(new ListarManutencoesAbertasQuery(), default);

        lista.Should().ContainSingle()
            .Which.OrdemServicoId.Should().Be(aberta.Value);
    }

    [Fact] // Painel e tenant-scoped: veiculos de outro tenant nao entram na agregacao.
    public async Task Painel_e_isolado_por_tenant()
    {
        var veiculo = VeiculoTombado("12345678900", "ABC1D23");
        veiculo.RegistrarAbastecimento(InicioPeriodo, 50m, ValorMonetario.De(300m), 50000, 1010m, 100m, null);
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Veiculos.Add(veiculo);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            var handler = new ObterPainelFrotaHandler(new VeiculoRepository(contexto));
            var painel = await handler.Handle(new ObterPainelFrotaQuery(InicioPeriodo, FimPeriodo), default);
            painel.QuantidadeVeiculos.Should().Be(0);
        }
    }
}
