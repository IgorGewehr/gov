using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura de PERSISTENCIA (EF Core/SQLite) da Vigilancia Sanitaria (Onda 3c-2): prova que o ciclo
/// <c>cadastro VISA → inspecao com pendencia → auto/intimacao → licenca sanitaria</c> materializa
/// corretamente (VO de documento "J:/F:", itens owned da inspecao, prazos do auto, validade da licenca)
/// e que o isolamento por tenant (Global Query Filter) impede leitura cross-tenant. Mesma imagem de
/// SaudeTestBase (SQLite em memoria + interceptors de tenant/auditoria).
/// </summary>
public sealed class VigilanciaSanitariaPersistenciaTests : SaudeTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 23);
    private const string CnpjValido = "11.222.333/0001-81";

    [Fact]
    public async Task Ciclo_completo_cadastro_inspecao_pendencia_auto_licenca_persiste()
    {
        // --- Cadastro do estabelecimento sujeito a VISA (risco alto = exige inspecao) ---
        Guid estabId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var repo = new EstabelecimentoFiscalizavelRepository(ctx);
            var estab = EstabelecimentoFiscalizavel.Cadastrar(
                TenantA,
                DocumentoResponsavel.Criar(CnpjValido),
                "Restaurante Bom Prato Ltda",
                RamoVisa.Alimentacao,
                GrauRiscoSanitario.Alto,
                EnderecoVisa.Criar("Rua das Flores, 100", "Centro", "Maximiliano de Almeida", "RS", "99970-000"));
            estabId = estab.Id.Value;
            repo.Adicionar(estab);
            await ctx.SaveChangesAsync();
        }

        // --- Inspecao com pendencia (AprovadoComPendencias) ---
        Guid inspecaoId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var repo = new InspecaoRepository(ctx);
            var inspecao = Inspecao.Abrir(TenantA, new EstabelecimentoFiscalizavelId(estabId), Hoje, null, "Roteiro RDC 216/2004");
            inspecao.RegistrarItem("Higienizacao de superficies", ConformidadeItem.Conforme, null);
            inspecao.RegistrarItem("Validade dos produtos", ConformidadeItem.NaoConforme, "produto com validade vencida");
            inspecao.Concluir(houveInfracaoGrave: false);
            inspecaoId = inspecao.Id.Value;
            repo.Adicionar(inspecao);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var inspecao = await new InspecaoRepository(ctx).ObterPorIdAsync(new InspecaoId(inspecaoId), default);
            inspecao.Should().NotBeNull();
            inspecao!.Situacao.Should().Be(SituacaoInspecao.Concluida);
            inspecao.Resultado.Should().Be(ResultadoInspecao.AprovadoComPendencias);
            inspecao.Itens.Should().HaveCount(2, "os itens owned do roteiro foram persistidos com a raiz");
            inspecao.QuantidadePendencias().Should().Be(1);
        }

        // --- Auto de intimacao (pendencia sanavel, sem multa) lavrado a partir da inspecao concluida ---
        Guid autoId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var repo = new AutoVisaRepository(ctx);
            (await repo.ExisteNumeroAsync("AI-2026-0001", default)).Should().BeFalse();
            var auto = AutoVisa.Lavrar(
                TenantA, new EstabelecimentoFiscalizavelId(estabId), new InspecaoId(inspecaoId),
                TipoAutoVisa.Intimacao, "AI-2026-0001", "Sanar validade em 30 dias", Hoje, Hoje.AddDays(30), valorMulta: null);
            autoId = auto.Id.Value;
            repo.Adicionar(auto);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var repo = new AutoVisaRepository(ctx);
            (await repo.ExisteNumeroAsync("AI-2026-0001", default)).Should().BeTrue("unicidade do numero do auto por tenant");
            var auto = await repo.ObterPorIdAsync(new AutoVisaId(autoId), default);
            auto!.Tipo.Should().Be(TipoAutoVisa.Intimacao);
            auto.ValorMulta.Should().BeNull();
            auto.PrazoFinal.Should().Be(Hoje.AddDays(30));

            var lavrados = await repo.ListarPorSituacaoAsync(SituacaoAutoVisa.Lavrado, default);
            lavrados.Should().ContainSingle(a => a.Id.Value == autoId);
        }

        // --- Licenca sanitaria emitida (ciclo fecha) com validade anual ---
        Guid licencaId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var repo = new LicencaSanitariaRepository(ctx);
            var licenca = LicencaSanitaria.Emitir(
                TenantA, new EstabelecimentoFiscalizavelId(estabId), "ALV-2026-0001", Hoje, Hoje.AddYears(1), new InspecaoId(inspecaoId));
            licencaId = licenca.Id.Value;
            repo.Adicionar(licenca);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var repo = new LicencaSanitariaRepository(ctx);
            var doEstab = await repo.ListarPorEstabelecimentoAsync(new EstabelecimentoFiscalizavelId(estabId), default);
            doEstab.Should().ContainSingle();
            doEstab[0].Situacao.Should().Be(SituacaoLicenca.Vigente);
            doEstab[0].ValidadeAte.Should().Be(Hoje.AddYears(1));

            var aVencer = await repo.ListarAVencerAsync(Hoje.AddYears(2), default);
            aVencer.Should().Contain(l => l.Id.Value == licencaId);
        }
    }

    [Fact]
    public async Task Estabelecimento_de_outro_tenant_nao_e_visivel_global_query_filter()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            var estab = EstabelecimentoFiscalizavel.Cadastrar(
                TenantA, DocumentoResponsavel.Criar(CnpjValido), "Farmacia Central Ltda",
                RamoVisa.Farmacia, GrauRiscoSanitario.Medio,
                EnderecoVisa.Criar("Av. Brasil, 200", "Centro", "Maximiliano de Almeida", "RS", "99970-000"));
            new EstabelecimentoFiscalizavelRepository(ctx).Adicionar(estab);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantB))
        {
            var repo = new EstabelecimentoFiscalizavelRepository(ctx);
            var (itens, total) = await repo.BuscarAsync(null, null, null, null, 1, 20, default);
            total.Should().Be(0, "o tenant B nao enxerga o cadastro do tenant A");
            itens.Should().BeEmpty();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var repo = new EstabelecimentoFiscalizavelRepository(ctx);
            var (_, total) = await repo.BuscarAsync("Farmacia", RamoVisa.Farmacia, null, null, 1, 20, default);
            total.Should().Be(1, "o tenant A ve o proprio cadastro pelo filtro de ramo/termo");
        }
    }
}
