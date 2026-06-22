using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Cofre.Domain;
using Tensorroot.Gov.Modules.Cofre.Infrastructure;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence;
using Xunit;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>
/// Testes de integracao do servico de assinatura sobre SQLite em memoria (A1-DESIGN §8): assinatura
/// valida com auditoria de SUCESSO, auditoria de FALHA quando nao ha cert ativo, e isolamento por
/// tenant (um tenant nao assina com o cert do outro — Global Query Filter).
/// </summary>
public sealed class ServicoAssinaturaDigitalTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;
    private readonly IProvedorKek _kek = CofreTestHelpers.CriarProvedorKek();

    public ServicoAssinaturaDigitalTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Assina_xml_com_cert_ativo_e_audita_sucesso()
    {
        await CadastrarCertAtivoAsync(TenantA);
        await using var contexto = CriarContexto(TenantA);
        var servico = CriarServico(contexto, TenantA);

        var xml = Encoding.UTF8.GetBytes("<doc><x>1</x></doc>");
        var resultado = await servico.AssinarXmlAsync(xml, new OpcoesAssinaturaXml(DestinoAssinatura.ESocial), default);

        resultado.XmlAssinado.Should().NotBeEmpty();
        resultado.HashArtefatoSha256.Should().HaveLength(64);

        var auditoria = await contexto.AssinaturaAuditLogs.SingleAsync();
        auditoria.Sucesso.Should().BeTrue();
        auditoria.Thumbprint.Should().Be(resultado.Thumbprint);
        auditoria.HashArtefatoSha256.Should().Be(resultado.HashArtefatoSha256);
        auditoria.UserId.Should().Be("teste");
    }

    [Fact]
    public async Task Sem_cert_ativo_lanca_e_audita_falha()
    {
        await using var contexto = CriarContexto(TenantA);
        var servico = CriarServico(contexto, TenantA);

        var acao = async () => await servico.AssinarXmlAsync(
            Encoding.UTF8.GetBytes("<doc/>"), new OpcoesAssinaturaXml(DestinoAssinatura.ESocial), default);

        await acao.Should().ThrowAsync<CertificadoInvalidoException>();

        var auditoria = await contexto.AssinaturaAuditLogs.SingleAsync();
        auditoria.Sucesso.Should().BeFalse();
        auditoria.Motivo.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Tenant_b_nao_enxerga_cert_do_tenant_a()
    {
        await CadastrarCertAtivoAsync(TenantA);

        await using var contexto = CriarContexto(TenantB);
        var servico = CriarServico(contexto, TenantB);

        var acao = async () => await servico.ObterInfoCertificadoAsync(default);

        // Global Query Filter: o cert do A nao existe para o B.
        await acao.Should().ThrowAsync<CertificadoInvalidoException>();
    }

    private async Task CadastrarCertAtivoAsync(Guid tenant)
    {
        await using var contexto = CriarContexto(tenant);
        var cripto = new CofreCripto(_kek);
        var custodia = new ServicoCustodiaCertificado(
            new CofreCertificadoRepository(contexto), cripto, new TenantContextFake(tenant), TimeProvider.System);

        await custodia.CadastrarOuRotacionarAsync("11222333000181", CofreTestHelpers.GerarPfx(), CofreTestHelpers.SenhaPfx, default);
    }

    private ServicoAssinaturaDigital CriarServico(CofreDbContext contexto, Guid tenant)
        => new(
            new CofreCertificadoRepository(contexto),
            new CofreCripto(_kek),
            new TenantContextFake(tenant),
            new CurrentUserFake(),
            TimeProvider.System,
            NullLogger<ServicoAssinaturaDigital>.Instance);

    private CofreDbContext CriarContexto(Guid tenant)
    {
        var tenantContext = new TenantContextFake(tenant);
        var options = new DbContextOptionsBuilder<CofreDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new CofreDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TenantContextFake(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;

        public bool HasTenant => true;
    }

    private sealed class CurrentUserFake : ICurrentUser
    {
        public string? UserId => "teste";

        public string? UserName => "Usuario de Teste";

        public string? IpAddress => "127.0.0.1";
    }
}
