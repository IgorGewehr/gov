using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do M6 (parte 4): inscrição em Dívida Ativa a partir de lançamento vencido, CDA com/sem
/// requisitos legais (LEF art. 2º §5º), atualização/juros/multa parametrizáveis, protesto extrajudicial
/// (remessa/retorno), execução fiscal e PRESCRIÇÃO (CTN art. 174) na data-limite. Nenhum percentual é
/// hardcoded — vêm da regra de encargos (lei municipal). Cálculo determinístico por datas do fato.
/// </summary>
public sealed class DividaAtivaCdaProtestoPrescricaoTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;

    public DividaAtivaCdaProtestoPrescricaoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    private static RegraEncargosDivida RegraTeste()
        => RegraEncargosDivida.Criar(2m, 1m, 0.5m, "Art. 99 do CTM (REFIS)");

    private static DividaAtiva InscreverExemplo(
        Guid tenant,
        ValorMonetario valor,
        DateOnly vencimento,
        DateOnly constituicao,
        DateOnly inscricao,
        long numeroInscricao = 1,
        int anosPrescricao = DividaAtiva.AnosPrescricao)
        => DividaAtiva.Inscrever(
            tenant,
            ContribuinteId.New(),
            LancamentoId.New(),
            TipoTributo.Iss,
            valor,
            vencimento,
            constituicao,
            inscricao,
            numeroInscricao,
            "ISS — competência 03/2020",
            "LC 116/2003 c/c art. 50 do CTM",
            RegraTeste(),
            anosPrescricao);

    // ----------------------------------------------------------------------------------------------
    // 1) INSCRIÇÃO a partir de lançamento VENCIDO e não pago
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Inscrever_so_aceita_lancamento_vencido_e_em_aberto()
    {
        var lancamento = Lancamento.Lancar(
            TenantA, ContribuinteId.New(), TipoTributo.Iptu,
            Competencia.De(2024, 1), ValorMonetario.De(1000m), new DateOnly(2024, 3, 10),
            new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 10));

        // Antes do vencimento: recusa.
        var actAntes = () => lancamento.InscreverEmDividaAtiva(new DateOnly(2024, 1, 1));
        actAntes.Should().Throw<InvalidOperationException>();

        // Depois do vencimento: aceita e muda de estado.
        lancamento.InscreverEmDividaAtiva(new DateOnly(2024, 4, 1));
        lancamento.Situacao.Should().Be(SituacaoLancamento.InscritoEmDividaAtiva);
    }

    // ----------------------------------------------------------------------------------------------
    // 2) CDA — requisitos legais obrigatórios (LEF art. 2º §5º I–VI)
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Cda_emitida_com_todos_os_requisitos_e_valida()
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(2000m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), new DateOnly(2021, 1, 15));

        var cda = divida.EmitirCda("CDA-2021/0001", "Maria Devedora", "Rua X, 100", null, new DateOnly(2021, 1, 15), "PA-2020/777");

        cda.NomeDevedor.Should().Be("Maria Devedora");
        cda.ValorOriginario.Valor.Should().Be(2000m);
        cda.OrigemNatureza.Should().NotBeNullOrWhiteSpace();
        cda.FundamentoLegal.Should().NotBeNullOrWhiteSpace();
        cda.NumeroInscricao.Should().Be(1);
        divida.Situacao.Should().Be(SituacaoDividaAtiva.CdaEmitida);
    }

    [Fact]
    public void Cda_recusa_emissao_sem_nome_do_devedor()
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(2000m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), new DateOnly(2021, 1, 15));

        var act = () => divida.EmitirCda("CDA-2021/0001", "   ", null, null, new DateOnly(2021, 1, 15), null);
        act.Should().Throw<CdaRequisitoAusenteException>()
            .Which.Requisito.Should().Contain("Nome do devedor");
    }

    [Fact]
    public void Cda_recusa_quando_valor_originario_e_zero()
    {
        // Valor originário zero viola o inc. II — a CertidaoDividaAtiva.Emitir recusa.
        var act = () => CertidaoDividaAtiva.Emitir(
            "CDA-X", "Fulano", null, null, ValorMonetario.Zero, new DateOnly(2020, 4, 10),
            "forma", "origem", "fundamento", "correcao", new DateOnly(2021, 1, 1), 1, null);
        act.Should().Throw<CdaRequisitoAusenteException>()
            .Which.Requisito.Should().Contain("Valor originário");
    }

    // ----------------------------------------------------------------------------------------------
    // 3) ATUALIZAÇÃO / JUROS / MULTA — parametrizáveis, determinísticos (datas do fato)
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Encargos_aplicam_multa_unica_juros_e_correcao_por_mes_de_atraso()
    {
        // Originário 1.000; vencimento 10/01/2020; data-base 10/07/2020 = 6 meses em atraso.
        // Multa 2% única = 20; juros 1% a.m. × 6 = 60; correção 0,5% a.m. × 6 = 30. Atualizado = 1.110.
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(1000m),
            new DateOnly(2020, 1, 10), new DateOnly(2020, 1, 10), new DateOnly(2020, 1, 10));

        var encargos = divida.ApurarEncargos(new DateOnly(2020, 7, 10));

        encargos.Multa.Valor.Should().Be(20m);
        encargos.Juros.Valor.Should().Be(60m);
        encargos.CorrecaoMonetaria.Valor.Should().Be(30m);
        encargos.ValorAtualizado.Valor.Should().Be(1110m);
    }

    [Fact]
    public void Encargos_sem_atraso_nao_aplicam_multa_nem_juros()
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(1000m),
            new DateOnly(2020, 1, 10), new DateOnly(2020, 1, 10), new DateOnly(2020, 1, 10));

        var encargos = divida.ApurarEncargos(new DateOnly(2020, 1, 10));

        encargos.Multa.Valor.Should().Be(0m);
        encargos.Juros.Valor.Should().Be(0m);
        encargos.ValorAtualizado.Valor.Should().Be(1000m);
    }

    [Fact]
    public void Encargos_sao_reproduziveis_mesma_data_mesmo_valor()
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(1234.56m),
            new DateOnly(2019, 2, 5), new DateOnly(2019, 2, 5), new DateOnly(2019, 2, 5));

        var a = divida.ApurarEncargos(new DateOnly(2021, 8, 5));
        var b = divida.ApurarEncargos(new DateOnly(2021, 8, 5));

        a.ValorAtualizado.Valor.Should().Be(b.ValorAtualizado.Valor);
    }

    // ----------------------------------------------------------------------------------------------
    // 3) PROTESTO extrajudicial — remessa e retorno (ato auditável)
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Protesto_requer_cda_emitida_e_registra_remessa()
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(500m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), new DateOnly(2021, 1, 15));

        // Sem CDA: recusa.
        var actSemCda = () => divida.GerarRemessaProtesto("CRA-RS", new DateOnly(2021, 2, 1));
        actSemCda.Should().Throw<InvalidOperationException>();

        divida.EmitirCda("CDA-2021/0002", "João", null, null, new DateOnly(2021, 1, 15), null);
        var remessa = divida.GerarRemessaProtesto("CRA-RS", new DateOnly(2021, 2, 1));

        divida.Situacao.Should().Be(SituacaoDividaAtiva.Protestada);
        divida.RemessasProtesto.Should().ContainSingle();

        // Retorno "pago no cartório" quita a dívida.
        divida.RegistrarTransmissaoProtesto(remessa.Id, new DateOnly(2021, 2, 5));
        divida.ProcessarRetornoProtesto(remessa.Id, OcorrenciaProtesto.PagoOuRetirado, new DateOnly(2021, 3, 1), "PROT-123");
        divida.Situacao.Should().Be(SituacaoDividaAtiva.Quitada);
    }

    // ----------------------------------------------------------------------------------------------
    // 4) PRESCRIÇÃO (CTN art. 174) — 5 anos da constituição definitiva, na data-limite
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Prescricao_corre_da_constituicao_definitiva_e_dispara_apos_a_data_limite()
    {
        // Constituição 10/05/2020 → prescreve em 10/05/2025 (5 anos). // CTN art. 174.
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(800m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), new DateOnly(2020, 6, 1));

        divida.DataPrescricao.Should().Be(new DateOnly(2025, 5, 10));

        // NA data-limite ainda não prescreveu (prescreve quando referencia > limite).
        divida.EstaPrescrita(new DateOnly(2025, 5, 10)).Should().BeFalse();

        // Um dia após: prescrita.
        divida.EstaPrescrita(new DateOnly(2025, 5, 11)).Should().BeTrue();
    }

    [Fact]
    public void Interrupcao_reinicia_o_quinquenio_da_data_do_marco()
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(800m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), new DateOnly(2020, 6, 1));

        // Reconhecimento do débito em 01/01/2023 (CTN art. 174 p.ú. IV) reinicia: prescreve 01/01/2028.
        divida.InterromperPrescricao(new DateOnly(2023, 1, 1));
        divida.DataPrescricao.Should().Be(new DateOnly(2028, 1, 1));
        divida.EstaPrescrita(new DateOnly(2025, 6, 1)).Should().BeFalse();
    }

    [Fact]
    public void Prazo_prescricional_e_parametrizavel()
    {
        // Prazo de 10 anos (parametrizado) — não trava em 5.
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(800m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), new DateOnly(2020, 6, 1),
            anosPrescricao: 10);

        divida.DataPrescricao.Should().Be(new DateOnly(2030, 5, 10));
    }

    // ----------------------------------------------------------------------------------------------
    // 5) GARANTIA da execução fiscal por PENHORA (CTN art. 206; Súmula 451-STJ) — habilita CPEN (P2-7)
    // ----------------------------------------------------------------------------------------------

    private static DividaAtiva EmExecucaoFiscal(DateOnly inscricao, DateOnly ajuizamento)
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(2000m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), inscricao);
        divida.EmitirCda("CDA-2021/0050", "Devedor Garantido", null, null, inscricao, null);
        divida.AjuizarExecucaoFiscal(ajuizamento);
        return divida;
    }

    [Fact] // Penhora suficiente afasta a exigibilidade p/ certidão: marca Garantida + emite evento (sem extinguir/suspender).
    public void Registrar_garantia_penhora_marca_garantida_e_emite_evento()
    {
        var divida = EmExecucaoFiscal(new DateOnly(2021, 1, 15), new DateOnly(2021, 6, 1));

        divida.RegistrarGarantiaPenhora(new DateOnly(2021, 7, 10));

        divida.Garantida.Should().BeTrue();
        divida.DataGarantiaPenhora.Should().Be(new DateOnly(2021, 7, 10));
        divida.Situacao.Should().Be(SituacaoDividaAtiva.EmExecucaoFiscal); // continua executada, apenas garantida.
        divida.DomainEvents.Should().ContainItemsAssignableTo<Tensorroot.Gov.Modules.Tributos.Domain.Events.GarantiaPenhoraRegistrada>();
    }

    [Fact] // A garantia só se registra em EXECUÇÃO FISCAL (não em dívida apenas inscrita/CDA).
    public void Registrar_garantia_penhora_fora_de_execucao_fiscal_e_recusada()
    {
        var divida = InscreverExemplo(
            TenantA, ValorMonetario.De(2000m),
            new DateOnly(2020, 4, 10), new DateOnly(2020, 5, 10), new DateOnly(2021, 1, 15));

        var act = () => divida.RegistrarGarantiaPenhora(new DateOnly(2021, 7, 10));

        act.Should().Throw<InvalidOperationException>();
        divida.Garantida.Should().BeFalse();
    }

    [Fact] // Fail-closed (CTN art. 156, V): penhora NÃO regulariza crédito já prescrito.
    public void Registrar_garantia_penhora_em_divida_prescrita_e_recusada()
    {
        // Constituição 10/05/2020 → prescreve 10/05/2025. Penhora em 2026 (após) deve ser barrada.
        var divida = EmExecucaoFiscal(new DateOnly(2020, 6, 1), new DateOnly(2020, 6, 2));

        var act = () => divida.RegistrarGarantiaPenhora(new DateOnly(2026, 1, 10));

        act.Should().Throw<DividaAtivaPrescritaException>();
        divida.Garantida.Should().BeFalse();
    }

    [Fact] // P2-7 persistência: as colunas Garantida/DataGarantiaPenhora (migração nova) round-trip no banco.
    public async Task Garantia_penhora_persiste_as_colunas_novas()
    {
        DividaAtivaId dividaId;
        await using (var contexto = CriarContexto(TenantA))
        {
            var divida = EmExecucaoFiscal(new DateOnly(2021, 1, 15), new DateOnly(2021, 6, 1));
            divida.RegistrarGarantiaPenhora(new DateOnly(2021, 7, 10));
            dividaId = divida.Id;
            contexto.DividasAtivas.Add(divida);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var divida = await contexto.DividasAtivas.SingleAsync(d => d.Id == dividaId);
            divida.Garantida.Should().BeTrue();
            divida.DataGarantiaPenhora.Should().Be(new DateOnly(2021, 7, 10));
        }
    }

    // ----------------------------------------------------------------------------------------------
    // PERSISTÊNCIA + ISOLAMENTO POR TENANT
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Divida_com_encargos_e_protesto_persiste_e_respeita_isolamento_de_tenant()
    {
        DividaAtivaId dividaId;
        await using (var contexto = CriarContexto(TenantA))
        {
            var contribuinte = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Maria Contribuinte");
            contexto.Contribuintes.Add(contribuinte);

            var lancamento = Lancamento.Lancar(
                TenantA, contribuinte.Id, TipoTributo.Iss,
                Competencia.De(2020, 3), ValorMonetario.De(1500m), new DateOnly(2020, 4, 10),
                new DateOnly(2020, 3, 31), new DateOnly(2020, 4, 10));
            contexto.Lancamentos.Add(lancamento);
            await contexto.SaveChangesAsync();

            lancamento.InscreverEmDividaAtiva(new DateOnly(2021, 1, 15));
            var divida = DividaAtiva.Inscrever(
                TenantA, contribuinte.Id, lancamento.Id, TipoTributo.Iss,
                lancamento.ValorPrincipal, lancamento.Vencimento,
                new DateOnly(2020, 5, 10), new DateOnly(2021, 1, 15), 1,
                "ISS — competência 03/2020", "LC 116/2003 c/c CTM", RegraTeste());
            dividaId = divida.Id;

            divida.EmitirCda("CDA-2021/0010", contribuinte.Nome, null, null, new DateOnly(2021, 1, 15), null);
            var remessa = divida.GerarRemessaProtesto("CRA-RS", new DateOnly(2021, 2, 1));
            divida.RegistrarTransmissaoProtesto(remessa.Id, new DateOnly(2021, 2, 5));
            contexto.DividasAtivas.Add(divida);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var divida = await contexto.DividasAtivas
                .Include(d => d.RemessasProtesto)
                .SingleAsync(d => d.Id == dividaId);

            divida.Situacao.Should().Be(SituacaoDividaAtiva.Protestada);
            divida.NumeroCda.Should().Be("CDA-2021/0010");
            divida.RegraEncargos.MultaMoraPercentual.Should().Be(2m);
            divida.RemessasProtesto.Should().ContainSingle()
                .Which.Situacao.Should().Be(SituacaoRemessaProtesto.Transmitida);

            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }

        // Tenant B não enxerga nada (Global Query Filter).
        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.DividasAtivas.ToListAsync()).Should().BeEmpty();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    private sealed class TenantContextFake(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;

        public bool HasTenant => true;
    }

    private sealed class CurrentUserFake : ICurrentUser
    {
        public string? UserId => "teste";

        public string? UserName => "Usuário de Teste";

        public string? IpAddress => "127.0.0.1";
    }
}
