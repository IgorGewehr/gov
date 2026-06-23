using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Behaviors;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// LG-A2: a base legal (LGPD art. 7/11) e EXIGIDA e aplicada nas leituras sensiveis. O behavior
/// transversal verifica a hipotese legal APLICADA contra o conjunto FECHADO de bases aplicaveis ao
/// recurso ANTES de projetar qualquer dado; base legal nao-aplicavel ⇒ acesso negado (excecao) e a
/// TENTATIVA selada na trilha imutavel (deny-by-default da accountability). Antes, a base legal era
/// apenas texto/constante registrado, nunca enforced.
/// </summary>
public sealed class LgA2BaseLegalEnforcementTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string CnsValido = "700000000000005";
    private static readonly Guid PrincipalSub = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000a001");

    private readonly SqliteConnection _connection;

    public LgA2BaseLegalEnforcementTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact] // As queries reais de saude declaram base legal de SAUDE (art. 11), nunca de dado comum.
    public void Queries_de_saude_declaram_base_legal_de_saude_aplicavel()
    {
        var historico = new ObterHistoricoClinicoDoPacienteQuery(Guid.NewGuid());
        historico.BasesLegaisAplicaveis.Should().Contain(historico.BaseLegal);
        historico.BasesLegaisAplicaveis.Should().NotContain(BaseLegalLgpd.Consentimento);

        var porCns = new ObterPacientePorCnsQuery(CnsValido);
        porCns.BasesLegaisAplicaveis.Should().Contain(porCns.BaseLegal);
        porCns.BasesLegaisAplicaveis.Should().NotContain(BaseLegalLgpd.Consentimento);
    }

    [Fact] // Base legal NAO aplicavel ao recurso ⇒ acesso negado e dado NUNCA projetado.
    public async Task Base_legal_nao_aplicavel_nega_o_acesso_e_nao_projeta()
    {
        await using var contexto = CriarContexto(out var holder);

        var query = new BaseLegalInaplicavelQuery();

        var ato = async () => await ExecutarComBehavior(
            contexto, holder, query, () => Task.FromResult("conteudo-sensivel"));

        await ato.Should().ThrowAsync<BaseLegalLgpdNaoAplicavelException>();
    }

    [Fact] // Mesmo negado, a TENTATIVA e selada na trilha (Action=ReadDenied) com a base rejeitada.
    public async Task Acesso_negado_sela_trilha_de_negativa()
    {
        // O SaudeDbContext populou o ScopeDbContextHolder ao ser construido (a trilha sela nele).
        await using var contexto = CriarContexto(out var holder);

        var query = new BaseLegalInaplicavelQuery();
        try
        {
            await ExecutarComBehavior(contexto, holder, query, () => Task.FromResult("x"));
        }
        catch (BaseLegalLgpdNaoAplicavelException)
        {
            // esperado
        }

        var negacoes = await contexto.AuditTrail
            .Where(t => t.Action == RegistroAcessoSensivel.AcaoLeituraNegada)
            .ToListAsync();

        negacoes.Should().ContainSingle();
        var negacao = negacoes.Single();
        negacao.EntityName.Should().Be("RecursoDeTeste");
        negacao.UserId.Should().Be(PrincipalSub.ToString());
        negacao.NewValues.Should().Contain("Consentimento"); // base legal rejeitada, estruturada
        negacao.HashAtual.Should().NotBeNullOrEmpty();

        // E NENHUMA leitura legitima foi selada.
        var leituras = await contexto.AuditTrail
            .Where(t => t.Action == RegistroAcessoSensivel.AcaoLeitura)
            .ToListAsync();
        leituras.Should().BeEmpty();
    }

    private static async Task<TResponse> ExecutarComBehavior<TRequest, TResponse>(
        SaudeDbContext contexto,
        ScopeDbContextHolder holder,
        TRequest request,
        Func<Task<TResponse>> handler)
        where TRequest : notnull
    {
        var registro = new RegistroAcessoSensivel(
            holder, new CurrentUserFixoA2(PrincipalSub.ToString()), new TenantContextFake(TenantA), TimeProvider.System);
        var behavior = new TrilhaAcessoSensivelBehavior<TRequest, TResponse>(
            registro, NullLogger<TrilhaAcessoSensivelBehavior<TRequest, TResponse>>.Instance);

        return await behavior.Handle(request, () => handler(), CancellationToken.None);
    }

    private SaudeDbContext CriarContexto(out ScopeDbContextHolder holder)
    {
        var tenantContext = new TenantContextFake(TenantA);
        holder = new ScopeDbContextHolder();
        var options = new DbContextOptionsBuilder<SaudeDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFixoA2(PrincipalSub.ToString()), TimeProvider.System))
            .Options;

        var contexto = new SaudeDbContext(options, tenantContext, holder);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    // Query de teste que declara base legal FORA do conjunto aplicavel (Consentimento de dado comum
    // para um recurso que so admite hipoteses de saude) — exercita o caminho de negativa do LG-A2.
    private sealed record BaseLegalInaplicavelQuery : ISensivelLgpd
    {
        public string EntidadeSensivel => "RecursoDeTeste";

        public string? EntidadeId => null;

        public BaseLegalLgpd BaseLegal => BaseLegalLgpd.Consentimento;

        public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisSaude.Aplicaveis;
    }

    private sealed class CurrentUserFixoA2(string? userId) : ICurrentUser
    {
        public string? UserId => userId;

        public string? UserName => "Principal";

        public string? IpAddress => "203.0.113.7";
    }
}
