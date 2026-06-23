using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Internal;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura do AUTOSSERVICO do servidor ("Minha Folha") — o nucleo de SEGURANCA do recurso (expoe
/// dado pessoal do servidor). Prova o ABAC dado-proprio A PROVA DE BALA: o servidor ve o PROPRIO
/// contracheque; NAO ve o de outro (mesmo "forjando" o id, o que e impossivel: o endpoint nao aceita
/// servidorId — o servidor e sempre resolvido do usuario autenticado); o usuario SEM vinculo e
/// negado (e a tentativa e selada na trilha LGPD); e o vinculo nao vaza entre tenants.
/// </summary>
public sealed class MinhaFolhaAutosservicoTests : RecursosHumanosTestBase
{
    private static readonly Guid UsuarioA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UsuarioB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid UsuarioSemVinculo = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

    /// <summary>ICurrentUser de teste com subject ("sub") configuravel.</summary>
    private sealed class CurrentUserFakeSub(Guid? sub) : ICurrentUser
    {
        public string? UserId => sub?.ToString();

        public string? UserName => "Servidor de Teste";

        public string? IpAddress => "127.0.0.1";
    }

    /// <summary>IRegistroAcessoSensivel de teste que apenas CONTA negativas e leituras seladas.</summary>
    private sealed class RegistroAcessoSpy : IRegistroAcessoSensivel
    {
        public int Leituras { get; private set; }

        public int Negativas { get; private set; }

        public Task RegistrarAsync(string entidade, string? entidadeId, BaseLegalLgpd baseLegal, CancellationToken cancellationToken = default)
        {
            Leituras++;
            return Task.CompletedTask;
        }

        public Task RegistrarNegacaoAsync(string entidade, string? entidadeId, BaseLegalLgpd baseLegalRejeitada, CancellationToken cancellationToken = default)
        {
            Negativas++;
            return Task.CompletedTask;
        }
    }

    private static ResolvedorServidorDoUsuarioAutenticado Resolvedor(
        RecursosHumanosDbContext ctx, Guid? sub, RegistroAcessoSpy spy)
        => new(new CurrentUserFakeSub(sub), new VinculoServidorUsuarioRepository(ctx), spy);

    private static async Task<Guid> SemearServidorAsync(RecursosHumanosDbContext ctx, Guid tenantId, string matricula, string cpf)
    {
        var servidor = Servidor.Admitir(
            tenantId,
            Cpf.Create(cpf),
            Matricula.De(matricula),
            DadosPessoais.Criar("Servidor " + matricula, new DateOnly(1990, 1, 1)),
            CargoId.New(),
            RegimePrevidenciario.Rpps,
            new DateOnly(2026, 1, 5));
        ctx.Servidores.Add(servidor);
        await ctx.SaveChangesAsync();
        return servidor.Id.Value;
    }

    private static async Task VincularAsync(RecursosHumanosDbContext ctx, Guid tenantId, Guid usuarioId, Guid servidorId)
    {
        ctx.VinculosServidorUsuario.Add(
            VinculoServidorUsuario.Criar(tenantId, usuarioId, new ServidorId(servidorId)));
        await ctx.SaveChangesAsync();
    }

    private static async Task SemearContrachequeMensalAsync(
        RecursosHumanosDbContext ctx, Guid tenantId, Guid servidorId, int ano, int mes, decimal vencimento)
    {
        var folha = FolhaDePagamento.Abrir(tenantId, Competencia.De(ano, mes));
        folha.AdicionarEvento(
            servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento,
            BaseCalculo.De(vencimento), vencimento, RegimePrevidenciario.Rpps);
        ctx.FolhasDePagamento.Add(folha);
        await ctx.SaveChangesAsync();
    }

    [Fact] // Servidor ve o PROPRIO contracheque (servidor resolvido do usuario autenticado).
    public async Task Servidor_ve_o_proprio_contracheque()
    {
        await using var ctx = CriarContexto(TenantA);
        var servidorA = await SemearServidorAsync(ctx, TenantA, "M-A", "52998224725");
        await VincularAsync(ctx, TenantA, UsuarioA, servidorA);
        await SemearContrachequeMensalAsync(ctx, TenantA, servidorA, 2026, 3, 8000m);

        var spy = new RegistroAcessoSpy();
        var handler = new ObterMeuContrachequeHandler(Resolvedor(ctx, UsuarioA, spy), new FolhaDePagamentoRepository(ctx));

        var dto = await handler.Handle(new ObterMeuContrachequeQuery(2026, 3), default);

        dto.Should().NotBeNull();
        dto!.ServidorId.Should().Be(servidorA);
        dto.TotalProventos.Should().Be(8000m);
        dto.LiquidoAPagar.Should().Be(8000m);
    }

    [Fact] // Servidor NAO ve o contracheque de OUTRO: o endpoint nao aceita servidorId; resolve SEMPRE
            // o proprio. Mesmo o usuario A NAO consegue ver os dados do servidor B — so enxerga os seus.
    public async Task Servidor_nao_ve_contracheque_de_outro_mesmo_sem_poder_forjar_id()
    {
        await using var ctx = CriarContexto(TenantA);
        var servidorA = await SemearServidorAsync(ctx, TenantA, "M-A", "52998224725");
        var servidorB = await SemearServidorAsync(ctx, TenantA, "M-B", "39053344705");
        await VincularAsync(ctx, TenantA, UsuarioA, servidorA);
        await VincularAsync(ctx, TenantA, UsuarioB, servidorB);

        // SO o servidor B tem contracheque nesta competencia.
        await SemearContrachequeMensalAsync(ctx, TenantA, servidorB, 2026, 3, 9000m);

        var spy = new RegistroAcessoSpy();
        // O usuario A consulta — resolve para o servidor A, que NAO tem eventos nessa folha => null.
        var handler = new ObterMeuContrachequeHandler(Resolvedor(ctx, UsuarioA, spy), new FolhaDePagamentoRepository(ctx));

        var dto = await handler.Handle(new ObterMeuContrachequeQuery(2026, 3), default);

        // A nunca enxerga as verbas de B: o filtro por servidor-proprio retorna vazio (null).
        dto.Should().BeNull();
    }

    [Fact] // A ancora resolve o servidor do PROPRIO usuario — nunca o de outro usuario.
    public async Task Resolvedor_devolve_apenas_o_servidor_do_proprio_usuario()
    {
        await using var ctx = CriarContexto(TenantA);
        var servidorA = await SemearServidorAsync(ctx, TenantA, "M-A", "52998224725");
        var servidorB = await SemearServidorAsync(ctx, TenantA, "M-B", "39053344705");
        await VincularAsync(ctx, TenantA, UsuarioA, servidorA);
        await VincularAsync(ctx, TenantA, UsuarioB, servidorB);

        var spy = new RegistroAcessoSpy();
        var resolvidoA = await Resolvedor(ctx, UsuarioA, spy).ResolverServidorAtualAsync(default);
        var resolvidoB = await Resolvedor(ctx, UsuarioB, spy).ResolverServidorAtualAsync(default);

        resolvidoA.Value.Should().Be(servidorA);
        resolvidoB.Value.Should().Be(servidorB);
        resolvidoA.Value.Should().NotBe(servidorB);
    }

    [Fact] // Usuario SEM vinculo e NEGADO (deny-by-default) e a tentativa e selada na trilha LGPD.
    public async Task Usuario_sem_vinculo_e_negado_e_auditado()
    {
        await using var ctx = CriarContexto(TenantA);
        await SemearServidorAsync(ctx, TenantA, "M-A", "52998224725"); // existe servidor, mas sem vinculo p/ este usuario

        var spy = new RegistroAcessoSpy();
        var handler = new ObterMeuContrachequeHandler(Resolvedor(ctx, UsuarioSemVinculo, spy), new FolhaDePagamentoRepository(ctx));

        var acao = async () => await handler.Handle(new ObterMeuContrachequeQuery(2026, 3), default);

        await acao.Should().ThrowAsync<UsuarioSemVinculoServidorException>();
        spy.Negativas.Should().Be(1); // trilha de NEGATIVA selada (accountability LGPD).
    }

    [Fact] // Sem usuario autenticado (sub ausente) tambem e negado e auditado.
    public async Task Sem_usuario_autenticado_e_negado_e_auditado()
    {
        await using var ctx = CriarContexto(TenantA);

        var spy = new RegistroAcessoSpy();
        var resolvedor = Resolvedor(ctx, sub: null, spy);

        var acao = async () => await resolvedor.ResolverServidorAtualAsync(default);

        await acao.Should().ThrowAsync<UsuarioSemVinculoServidorException>();
        spy.Negativas.Should().Be(1);
    }

    [Fact] // ISOLAMENTO POR TENANT: o vinculo do tenant B NAO e visivel do contexto do tenant A.
    public async Task Vinculo_nao_vaza_entre_tenants()
    {
        Guid servidorB;
        await using (var ctxB = CriarContexto(TenantB))
        {
            servidorB = await SemearServidorAsync(ctxB, TenantB, "M-B", "39053344705");
            // No tenant B, o UsuarioA esta vinculado ao servidor B.
            await VincularAsync(ctxB, TenantB, UsuarioA, servidorB);
        }

        await using var ctxA = CriarContexto(TenantA);
        var spy = new RegistroAcessoSpy();
        var resolvedor = Resolvedor(ctxA, UsuarioA, spy);

        // No contexto do tenant A, o vinculo de A (que so existe em B) nao e visivel: NEGADO.
        var acao = async () => await resolvedor.ResolverServidorAtualAsync(default);

        await acao.Should().ThrowAsync<UsuarioSemVinculoServidorException>();
        spy.Negativas.Should().Be(1);
    }

    [Fact] // Espelho de ponto proprio: ve a apuracao do PROPRIO servidor.
    public async Task Servidor_ve_o_proprio_espelho_de_ponto()
    {
        await using var ctx = CriarContexto(TenantA);
        var servidorA = await SemearServidorAsync(ctx, TenantA, "M-A", "52998224725");
        await VincularAsync(ctx, TenantA, UsuarioA, servidorA);

        var apuracao = ApuracaoPonto.Apurar(
            TenantA, servidorA, Competencia.De(2026, 3),
            new ResultadoApuracaoCompetencia(8800, 8800, 120, 0, []));
        ctx.PontoApuracoes.Add(apuracao);
        await ctx.SaveChangesAsync();

        var spy = new RegistroAcessoSpy();
        var handler = new ObterMeuEspelhoDePontoHandler(Resolvedor(ctx, UsuarioA, spy), new ApuracaoPontoRepository(ctx));

        var dto = await handler.Handle(new ObterMeuEspelhoDePontoQuery(2026, 3), default);

        dto.Should().NotBeNull();
        dto!.ServidorId.Should().Be(servidorA);
        dto.MinutosExtras.Should().Be(120);
    }
}
