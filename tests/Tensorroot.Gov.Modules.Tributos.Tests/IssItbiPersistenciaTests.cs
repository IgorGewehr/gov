using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova de ponta-a-ponta da persistência do ISS (apuração/livro) e do ITBI (transmissão) sobre SQLite
/// em memória, com auditoria e ISOLAMENTO por tenant (Global Query Filter).
/// </summary>
public sealed class IssItbiPersistenciaTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaa3333-3333-3333-3333-333333333333");
    private static readonly Guid TenantB = Guid.Parse("bbbb4444-4444-4444-4444-444444444444");

    private readonly SqliteConnection _connection;

    public IssItbiPersistenciaTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Apuracao_iss_persiste_livro_e_isola_por_tenant()
    {
        ContribuinteId prestadorId;

        await using (var contexto = CriarContexto(TenantA))
        {
            var prestador = Contribuinte.PessoaJuridica(TenantA, Cnpj.Create("11.222.333/0001-81"), "Prestadora ME");
            contexto.Contribuintes.Add(prestador);
            prestadorId = prestador.Id;

            var tabela = TabelaAliquotaIss.Criar(TenantA, 202601, "CTM Municipal");
            tabela.DefinirItem("1.07", 2.0m);
            tabela.DefinirItem("7.02", 3.0m, retencaoObrigatoria: true);
            tabela.Publicar();
            contexto.TabelasAliquotaIss.Add(tabela);

            var competencia = Competencia.De(2026, 5);
            var apuracao = ApuracaoIss.Abrir(TenantA, prestadorId, competencia);

            var nota1 = NotaFiscalServico.Importar(TenantA, "CHV-A", "11222333000181", null, ValorMonetario.De(1_000m), ValorMonetario.De(0m), new DateOnly(2026, 5, 5), competencia, "1.07");
            var nota2 = NotaFiscalServico.Importar(TenantA, "CHV-B", "11222333000181", null, ValorMonetario.De(2_000m), ValorMonetario.De(0m), new DateOnly(2026, 5, 6), competencia, "7.02");
            apuracao.Escriturar(CalculadoraIss.Apurar(nota1, tabela));
            apuracao.Escriturar(CalculadoraIss.Apurar(nota2, tabela));
            apuracao.Encerrar();
            contexto.ApuracoesIss.Add(apuracao);

            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var apuracao = await contexto.ApuracoesIss.Include(a => a.Itens).SingleAsync();
            apuracao.IssProprio.Valor.Should().Be(20m, "item 1.07 R$1.000 @ 2%");
            apuracao.IssRetido.Valor.Should().Be(60m, "item 7.02 R$2.000 @ 3% com retenção obrigatória");
            apuracao.Itens.Should().HaveCount(2);
            (await contexto.AuditTrail.AnyAsync()).Should().BeTrue();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.ApuracoesIss.AnyAsync()).Should().BeFalse();
            (await contexto.TabelasAliquotaIss.AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Transmissao_itbi_persiste_com_base_declarada_e_isola_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            var transmitente = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Vendedor");
            var adquirente = Contribuinte.PessoaFisica(TenantA, Cpf.Create("168.995.350-09"), "Comprador");
            contexto.Contribuintes.AddRange(transmitente, adquirente);

            var imovel = Imovel.Cadastrar(
                TenantA,
                transmitente.Id,
                IdentificacaoImovel.Criar("000.555.0001"),
                EnderecoImovel.Criar("Rua X", "Centro", "01-01-001", "ZONA-1"),
                CaracteristicasImovel.Criar(300m, 100m, TipoUsoImovel.Residencial, "MEDIO"));
            contexto.Imoveis.Add(imovel);

            var aliquota = AliquotaItbi.Criar(TenantA, 2026, 2.0m, 0.5m, "Lei ITBI Municipal");
            aliquota.Publicar();
            contexto.AliquotasItbi.Add(aliquota);

            // Venal R$ 300.000; declarado R$ 250.000 → base DECLARADA 250.000 × 2% = R$ 5.000 (Tema 1.113).
            var memoria = CalculadoraItbi.Calcular(ValorMonetario.De(300_000m), ValorMonetario.De(250_000m), 2.0m, 0.5m);
            var transmissao = TransmissaoImobiliaria.Registrar(TenantA, imovel.Id, transmitente.Id, adquirente.Id, 2026, ValorMonetario.De(250_000m), memoria);
            contexto.TransmissoesImobiliarias.Add(transmissao);

            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var transmissao = await contexto.TransmissoesImobiliarias.SingleAsync();
            transmissao.BaseCalculo.Valor.Should().Be(250_000m);
            transmissao.Origem.Should().Be(OrigemBaseCalculoItbi.Declarada);
            transmissao.ProcessoArbitramentoId.Should().BeNull();
            transmissao.ImpostoDevido.Valor.Should().Be(5_000m);
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.TransmissoesImobiliarias.AnyAsync()).Should().BeFalse();
            (await contexto.AliquotasItbi.AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Arbitramento_so_eleva_a_base_apos_processo_concluido()
    {
        Guid transmissaoId;

        await using (var contexto = CriarContexto(TenantA))
        {
            var transmitente = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Vendedor");
            var adquirente = Contribuinte.PessoaFisica(TenantA, Cpf.Create("168.995.350-09"), "Comprador");
            contexto.Contribuintes.AddRange(transmitente, adquirente);

            var memoria = CalculadoraItbi.Calcular(ValorMonetario.De(300_000m), ValorMonetario.De(200_000m), 2.0m, 0.5m);
            var transmissao = TransmissaoImobiliaria.Registrar(TenantA, new ImovelId(Guid.NewGuid()), transmitente.Id, adquirente.Id, 2026, ValorMonetario.De(200_000m), memoria);
            transmissaoId = transmissao.Id.Value;
            contexto.TransmissoesImobiliarias.Add(transmissao);

            // Processo de arbitramento completo: instaurar → contraditório → análise → concluir.
            var processo = ProcessoArbitramentoItbi.Instaurar(
                TenantA, transmissao.Id, "ARB-2026-001",
                "Declaração não fidedigna: preço muito abaixo de mercado, indícios de subfaturamento.",
                ValorMonetario.De(320_000m), "Laudo de avaliação individualizado nº 12/2026.",
                Guid.NewGuid(), new DateOnly(2026, 6, 1));
            processo.AbrirContraditorio(new DateOnly(2026, 6, 2));
            processo.RegistrarContraditorio("Apresento documentos do financiamento.", new DateOnly(2026, 6, 10));
            var resultado = processo.Concluir(ValorMonetario.De(320_000m), new DateOnly(2026, 6, 20));
            contexto.ProcessosArbitramentoItbi.Add(processo);

            var memoriaArbitrada = CalculadoraItbi.RecalcularComArbitramento(
                transmissao.ValorVenalReferencia, transmissao.ValorDeclarado, resultado, 2.0m, 0.5m);
            transmissao.AplicarArbitramento(resultado, memoriaArbitrada);

            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var transmissao = await contexto.TransmissoesImobiliarias.SingleAsync(t => t.Id == new TransmissaoImobiliariaId(transmissaoId));
            transmissao.Origem.Should().Be(OrigemBaseCalculoItbi.ArbitradaArt148);
            transmissao.BaseCalculo.Valor.Should().Be(320_000m, "base arbitrada após processo CTN 148");
            transmissao.ImpostoDevido.Valor.Should().Be(6_400m);
            transmissao.ProcessoArbitramentoId.Should().NotBeNull();

            var processo = await contexto.ProcessosArbitramentoItbi.SingleAsync();
            processo.Estado.Should().Be(EstadoArbitramentoItbi.Concluido);
            processo.ValorArbitradoFinal!.Valor.Should().Be(320_000m);
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.ProcessosArbitramentoItbi.AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Dedup_e_apuracao_isolam_por_tenant_mesma_chave_em_dois_tenants_nao_vaza()
    {
        // IS-6 — A MESMA chave de acesso e o MESMO prestador/competencia existem legitimamente em
        // dois tenants distintos (CNPJs distintos do mesmo grupo, ou colisao). A dedup do Worker e a
        // base da apuracao do ISS NAO podem cruzar tenants: nem descartar a nota do tenant B como
        // "ja existente" (sub-apuracao), nem incluir a nota do tenant A na base do tenant B (vazamento).
        const string chaveCompartilhada = "CHV-COMPARTILHADA-2026";
        const string cnpjPrestador = "11222333000181";
        var competencia = Competencia.De(2026, 5);

        // Tenant A grava a nota com a chave compartilhada.
        await using (var contexto = CriarContexto(TenantA))
        {
            var nota = NotaFiscalServico.Importar(
                TenantA, chaveCompartilhada, cnpjPrestador, null,
                ValorMonetario.De(1_000m), ValorMonetario.De(0m),
                new DateOnly(2026, 5, 5), competencia, "1.07");
            contexto.NotasFiscaisServico.Add(nota);
            await contexto.SaveChangesAsync();
        }

        // Tenant B: a dedup NAO pode considerar a chave "ja existente" (a nota e de outro tenant).
        await using (var contexto = CriarContexto(TenantB))
        {
            var repositorio = new Infrastructure.Persistence.Repositories.NotaFiscalServicoRepository(contexto);
            (await repositorio.ExistePorChaveAsync(chaveCompartilhada, CancellationToken.None))
                .Should().BeFalse("a chave existe apenas no Tenant A; a dedup do Tenant B nao pode descartar a nota como duplicada");

            // Tenant B grava a sua propria nota com a MESMA chave (permitido: indice e (TenantId, Chave)).
            var notaB = NotaFiscalServico.Importar(
                TenantB, chaveCompartilhada, cnpjPrestador, null,
                ValorMonetario.De(2_000m), ValorMonetario.De(0m),
                new DateOnly(2026, 5, 6), competencia, "1.07");
            contexto.NotasFiscaisServico.Add(notaB);
            await contexto.SaveChangesAsync();

            // Agora, no Tenant B, a chave existe (a propria nota dele).
            (await repositorio.ExistePorChaveAsync(chaveCompartilhada, CancellationToken.None))
                .Should().BeTrue("agora a chave existe no proprio Tenant B");
        }

        // A base da apuracao de cada tenant ve SOMENTE a propria nota (sem vazamento de valor).
        await using (var contexto = CriarContexto(TenantA))
        {
            var consulta = new Infrastructure.Persistence.Repositories.NotaFiscalServicoConsulta(contexto);
            var notas = await consulta.ListarVigentesPorPrestadorCompetenciaAsync(cnpjPrestador, competencia, CancellationToken.None);
            notas.Should().ContainSingle();
            notas[0].ValorServico.Valor.Should().Be(1_000m, "Tenant A so enxerga a sua nota de R$1.000");
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            var consulta = new Infrastructure.Persistence.Repositories.NotaFiscalServicoConsulta(contexto);
            var notas = await consulta.ListarVigentesPorPrestadorCompetenciaAsync(cnpjPrestador, competencia, CancellationToken.None);
            notas.Should().ContainSingle();
            notas[0].ValorServico.Valor.Should().Be(2_000m, "Tenant B so enxerga a sua nota de R$2.000");
        }
    }

    [Fact]
    public void Arbitramento_exige_contraditorio_antes_de_concluir()
    {
        var processo = ProcessoArbitramentoItbi.Instaurar(
            TenantA, new TransmissaoImobiliariaId(Guid.NewGuid()), "ARB-2026-002",
            "Declaração omissa.", ValorMonetario.De(100_000m), "Fundamentação.",
            Guid.NewGuid(), new DateOnly(2026, 6, 1));

        // Pular o contraditório e tentar concluir → recusado (contraditório obrigatório, Tema 1.113).
        var acao = () => processo.Concluir(ValorMonetario.De(120_000m), new DateOnly(2026, 6, 5));
        acao.Should().Throw<InvalidOperationException>();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFakeIssItbi(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFakeIssItbi(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }
}

file sealed class TenantContextFakeIssItbi(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFakeIssItbi : ICurrentUser
{
    public string? UserId => "teste-iss-itbi";

    public string? UserName => "Usuário ISS/ITBI";

    public string? IpAddress => "127.0.0.1";
}
