using FluentAssertions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Pncp;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// W9.1 — INVARIANTE DE BLOQUEIO do empenho (Lei 14.133/2021, art. 94): contrato sem numero de controle
/// PNCP e ineficaz e NAO empenha; divulgado no PNCP, empenha. Cobre a regra no DOMINIO
/// (<see cref="Contrato.PodeEmpenhar"/>), a tempestividade da divulgacao, o alerta de prazo a vencer/vencido
/// e o mapeamento cross-module (<see cref="ConsultaContratoParaEmpenho"/>) que Financas consome ANTES de
/// empenhar — incluindo o round-trip de persistencia do VO <see cref="PrazoPncp"/> sobre SQLite.
/// </summary>
public sealed class PncpBloqueioEmpenhoTests : AdministracaoTestBase
{
    private static Contrato NovoContrato(Guid tenant, DateOnly assinatura)
        => Contrato.Celebrar(
            tenant,
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrigemContratacao.Licitacao,
            "Servicos de manutencao predial",
            ValorMonetario.De(100000m),
            assinatura,
            assinatura,
            assinatura.AddYears(1),
            fornecedorImpedido: false,
            PrazoDivulgacaoPadrao,
            Calendario);

    // ---------- Invariante de bloqueio no dominio ----------

    [Fact]
    public void Contrato_recem_celebrado_sem_pncp_NAO_pode_empenhar()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        contrato.PublicadoNoPncp.Should().BeFalse();
        contrato.PodeEmpenhar().Should().BeFalse("sem numero de controle PNCP o contrato e ineficaz (art. 94)");
    }

    [Fact]
    public void Contrato_divulgado_no_pncp_PODE_empenhar()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        contrato.PublicarContratoPncp("07000000000001-2-000001/2026", new DateOnly(2026, 1, 6));
        contrato.PodeEmpenhar().Should().BeTrue("com numero de controle PNCP o contrato e eficaz");
    }

    [Fact]
    public void Contrato_extinto_NAO_pode_empenhar_mesmo_com_pncp()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        contrato.PublicarContratoPncp("07000000000001-2-000001/2026", new DateOnly(2026, 1, 6));
        contrato.ConfirmarDotacao(EmpenhoRef.De(Guid.NewGuid(), "2026NE000001"));
        contrato.IniciarExecucao();
        contrato.Encerrar(new DateOnly(2026, 6, 1));
        contrato.PodeEmpenhar().Should().BeFalse("contrato encerrado/extinto nao sustenta empenho");
    }

    // ---------- Tempestividade / prazo ----------

    [Fact]
    public void Divulgacao_dentro_do_prazo_nao_marca_intempestividade()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        // 05/01/2026 +20 d.u. (so fins de semana) = 02/02/2026. Publicar bem antes.
        contrato.PublicarContratoPncp("07000000000001-2-000001/2026", new DateOnly(2026, 1, 6));
        contrato.PublicacaoPncpVencida.Should().BeFalse();
    }

    [Fact]
    public void Divulgacao_fora_do_prazo_marca_intempestividade_sem_impedir_eficacia()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        // Publica muito depois do vencimento (20 d.u.): intempestiva, porem ainda eficaz.
        contrato.PublicarContratoPncp("07000000000001-2-000001/2026", new DateOnly(2026, 3, 1));
        contrato.PublicacaoPncpVencida.Should().BeTrue("divulgacao fora do prazo do art. 94");
        contrato.PodeEmpenhar().Should().BeTrue("o ato foi praticado: eficaz, ainda que intempestivo");
    }

    [Fact]
    public void AvaliarPrazoPncp_vencido_quando_passou_o_prazo_sem_divulgar()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        // Sem divulgar; hoje muito apos o vencimento -> Vencido.
        var alerta = contrato.AvaliarPrazoPncp(new DateOnly(2026, 3, 1), antecedenciaDiasUteis: 5, Calendario);
        alerta.Should().Be(AlertaPrazoPncp.Vencido);
    }

    [Fact]
    public void AvaliarPrazoPncp_nenhum_apos_divulgado()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        contrato.PublicarContratoPncp("07000000000001-2-000001/2026", new DateOnly(2026, 1, 6));
        contrato.AvaliarPrazoPncp(new DateOnly(2026, 3, 1), 5, Calendario).Should().Be(AlertaPrazoPncp.Nenhum);
    }

    // ---------- Cross-module: ConsultaContratoParaEmpenho sobre SQLite (round-trip do VO PrazoPncp) ----------

    [Fact]
    public async Task Consulta_cross_module_bloqueia_sem_pncp_e_libera_com_pncp()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        Guid contratoId;

        using (var ctx = CriarContexto(TenantA))
        {
            ctx.Contratos.Add(contrato);
            await ctx.SaveChangesAsync();
            contratoId = contrato.Id.Value;
        }

        // Antes da divulgacao: bloqueado por falta de PNCP.
        using (var ctx = CriarContexto(TenantA))
        {
            var consulta = new ConsultaContratoParaEmpenho(ctx);
            var status = await consulta.ConsultarAsync(contratoId, CancellationToken.None);
            status.Should().Be(StatusContratoParaEmpenho.BloqueadoSemPncp);
        }

        // Divulga no PNCP e persiste.
        using (var ctx = CriarContexto(TenantA))
        {
            var alvo = await ctx.Contratos.FindAsync(new ContratoId(contratoId));
            alvo!.PublicarContratoPncp("07000000000001-2-000001/2026", new DateOnly(2026, 1, 6));
            await ctx.SaveChangesAsync();
        }

        // Depois da divulgacao: apto. Prova tambem o round-trip do VO PrazoPncp (reidratado da coluna).
        using (var ctx = CriarContexto(TenantA))
        {
            var consulta = new ConsultaContratoParaEmpenho(ctx);
            var status = await consulta.ConsultarAsync(contratoId, CancellationToken.None);
            status.Should().Be(StatusContratoParaEmpenho.AptoParaEmpenho);
        }
    }

    [Fact]
    public async Task Consulta_cross_module_isola_por_tenant_outro_tenant_nao_encontra()
    {
        var contrato = NovoContrato(TenantA, new DateOnly(2026, 1, 5));
        contrato.PublicarContratoPncp("07000000000001-2-000001/2026", new DateOnly(2026, 1, 6));
        Guid contratoId;
        using (var ctx = CriarContexto(TenantA))
        {
            ctx.Contratos.Add(contrato);
            await ctx.SaveChangesAsync();
            contratoId = contrato.Id.Value;
        }

        // Tenant B nao enxerga o contrato do tenant A (Global Query Filter) -> NaoEncontrado.
        using (var ctx = CriarContexto(TenantB))
        {
            var consulta = new ConsultaContratoParaEmpenho(ctx);
            var status = await consulta.ConsultarAsync(contratoId, CancellationToken.None);
            status.Should().Be(StatusContratoParaEmpenho.NaoEncontrado);
        }
    }
}
