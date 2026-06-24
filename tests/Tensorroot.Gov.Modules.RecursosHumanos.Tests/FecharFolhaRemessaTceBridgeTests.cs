using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura da PONTE RH -> Transparencia: ao fechar a folha, o handler enfileira no Outbox o
/// <see cref="FolhaResumoRemessaTceIntegrationEvent"/> (snapshot servidores/rubricas/lancamentos) que a
/// Transparencia consome para montar a remessa de folha ao TCE-RS (Res. 1099) — espelhando a MSC.
/// </summary>
public sealed class FecharFolhaRemessaTceBridgeTests : RecursosHumanosTestBase
{
    private sealed class IntegrationEventWriterFake : IIntegrationEventWriter
    {
        public List<IIntegrationEvent> Enfileirados { get; } = [];

        public void Enfileirar(IIntegrationEvent integrationEvent) => Enfileirados.Add(integrationEvent);
    }

    [Fact] // Fechar folha enfileira o resumo da remessa TCE com servidores, rubricas e lancamentos.
    public async Task Fechar_folha_enfileira_resumo_remessa_tce_com_snapshot()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 5);
        var servidorId = Guid.NewGuid();

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA,
                Cpf.Create("52998224725"),
                Matricula.De("0001"),
                DadosPessoais.Criar("Maria da Silva", new DateOnly(1985, 4, 2)),
                CargoId.New(),
                RegimePrevidenciario.Rpps,
                new DateOnly(2010, 3, 1));
            ctx.Servidores.Add(servidor);
            servidorId = servidor.Id.Value;

            ctx.Rubricas.Add(RubricaFolha.Criar(
                TenantA, Rubrica.De("1001"), "Vencimento Basico", NaturezaRubrica.Provento, competencia, incideRpps: true));
            ctx.Rubricas.Add(RubricaFolha.Criar(
                TenantA, Rubrica.De("9001"), "Contribuicao RPPS", NaturezaRubrica.Desconto, competencia, incideRpps: true));

            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidorId, Rubrica.De("1001"), TipoEvento.Provento, BaseCalculo.De(8_000m), 8_000m, RegimePrevidenciario.Rpps);
            folha.AdicionarEvento(servidorId, Rubrica.De("9001"), TipoEvento.Desconto, BaseCalculo.De(8_000m), 1_120m, RegimePrevidenciario.Rpps);
            folha.Calcular(50_000m, new DateOnly(2026, 5, 30));
            ctx.FolhasDePagamento.Add(folha);
            folhaId = folha.Id.Value;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var writer = new IntegrationEventWriterFake();
            var handler = new FecharFolhaHandler(
                new FolhaDePagamentoRepository(ctx),
                new ServidorRepository(ctx),
                new CargoRepository(ctx),
                new RubricaFolhaRepository(ctx),
                ctx,
                writer,
                new DataHojeTenantFake(TimeProvider.System),
                TimeProvider.System);

            await handler.Handle(new FecharFolhaCommand(folhaId), default);

            var resumo = writer.Enfileirados.OfType<FolhaResumoRemessaTceIntegrationEvent>().Should().ContainSingle().Subject;
            resumo.TenantId.Should().Be(TenantA);
            resumo.Competencia.Should().Be("2026-05");
            resumo.Servidores.Should().ContainSingle().Which.Cpf.Should().Be("52998224725");
            resumo.Rubricas.Should().HaveCount(2);
            resumo.Lancamentos.Should().HaveCount(2);
            resumo.Lancamentos.Should().Contain(lancamento => lancamento.Operacao == "D" && lancamento.Valor == 1_120m);

            // H5: TODOS os integration events cross-module saem pelo Outbox (nunca via IPublisher in-process),
            // para que o consumidor (Financas/Educacao/Painel) resolva o proprio DbContext em escopo isolado.
            writer.Enfileirados.OfType<DespesaPessoalApuradaIntegrationEvent>().Should().ContainSingle();
            writer.Enfileirados.OfType<FolhaFechadaIntegrationEvent>().Should().ContainSingle();
            writer.Enfileirados.OfType<RemuneracaoMagisterioApuradaIntegrationEvent>().Should().ContainSingle();
        }
    }
}
