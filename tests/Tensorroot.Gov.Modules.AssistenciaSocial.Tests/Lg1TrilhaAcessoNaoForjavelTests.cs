using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Xunit;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// LG-1: a trilha de acesso ao prontuario sigiloso NAO e forjavel. O usuario gravado e SEMPRE o
/// principal autenticado (claim <c>sub</c> via <see cref="ICurrentUser"/>), nunca um valor do
/// cliente. Cobre a query (leitura) e o comando (registro de acesso), e a recusa quando nao ha
/// identidade rastreavel.
/// </summary>
public sealed class Lg1TrilhaAcessoNaoForjavelTests
{
    private static readonly Guid FamiliaId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid CrasId = Guid.Parse("cccccccc-0000-0000-0000-0000000000c1");
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Identidade do principal autenticado (claim 'sub'): e ESTA que deve ir para a trilha.
    private static readonly Guid PrincipalSub = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000a001");

    // Identidade de um colega que o atacante tentaria forjar — JAMAIS pode aparecer na trilha.
    private static readonly Guid ColegaForjado = Guid.Parse("bbbbbbbb-0000-0000-0000-00000000b002");

    [Fact] // Leitura: o acesso gravado usa o 'sub' do principal, ignorando qualquer valor de cliente.
    public async Task Query_grava_usuario_do_principal_nao_do_cliente()
    {
        var prontuario = NovoProntuario();
        var repo = new ProntuarioRepoFake(prontuario);
        var handler = new ObterProntuarioDaFamiliaHandler(
            repo, new UnitOfWorkFake(), new CurrentUserFixo(PrincipalSub.ToString()), TimeProvider.System);

        await handler.Handle(new ObterProntuarioDaFamiliaQuery(FamiliaId, "Acompanhamento PAIF"), CancellationToken.None);

        prontuario.Acessos.Should().ContainSingle();
        prontuario.Acessos.Single().UsuarioId.Should().Be(PrincipalSub);
        prontuario.Acessos.Single().UsuarioId.Should().NotBe(ColegaForjado);
    }

    [Fact] // Comando: idem — o usuario do acesso vem do principal, nunca do payload.
    public async Task Comando_grava_usuario_do_principal_nao_do_cliente()
    {
        var prontuario = NovoProntuario();
        var repo = new ProntuarioRepoFake(prontuario);
        var handler = new RegistrarAcessoProntuarioHandler(
            repo, new UnitOfWorkFake(), new CurrentUserFixo(PrincipalSub.ToString()), TimeProvider.System);

        await handler.Handle(new RegistrarAcessoProntuarioCommand(prontuario.Id.Value, "Leitura para parecer"), CancellationToken.None);

        prontuario.Acessos.Should().ContainSingle();
        prontuario.Acessos.Single().UsuarioId.Should().Be(PrincipalSub);
    }

    [Fact] // Sem principal autenticado, a leitura sigilosa e NEGADA (nunca trilha sem identidade).
    public async Task Sem_principal_autenticado_a_leitura_e_negada()
    {
        var prontuario = NovoProntuario();
        var handler = new ObterProntuarioDaFamiliaHandler(
            new ProntuarioRepoFake(prontuario), new UnitOfWorkFake(), new CurrentUserFixo(null), TimeProvider.System);

        var acao = async () => await handler.Handle(
            new ObterProntuarioDaFamiliaQuery(FamiliaId, "qualquer"), CancellationToken.None);

        await acao.Should().ThrowAsync<InvalidOperationException>();
        prontuario.Acessos.Should().BeEmpty();
    }

    [Fact] // 'sub' nao-GUID (ex.: futuro SSO) tambem e negado — identidade nao rastreavel.
    public async Task Sub_nao_guid_e_negado()
    {
        var prontuario = NovoProntuario();
        var handler = new ObterProntuarioDaFamiliaHandler(
            new ProntuarioRepoFake(prontuario), new UnitOfWorkFake(), new CurrentUserFixo("nao-e-guid"), TimeProvider.System);

        var acao = async () => await handler.Handle(
            new ObterProntuarioDaFamiliaQuery(FamiliaId, "qualquer"), CancellationToken.None);

        await acao.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact] // A query nao expoe mais UsuarioId: o contrato so aceita FamiliaId + MotivoAcesso (LG-1).
    public void Query_nao_tem_mais_parametro_de_usuario()
    {
        var construtores = typeof(ObterProntuarioDaFamiliaQuery).GetConstructors();
        construtores.Should().ContainSingle();
        construtores[0].GetParameters().Select(p => p.Name)
            .Should().BeEquivalentTo("FamiliaId", "MotivoAcesso");
    }

    private static ProntuarioSuas NovoProntuario()
        => ProntuarioSuas.Abrir(TenantA, FamiliaId, CrasId, new DateOnly(2026, 6, 21));

    private sealed class ProntuarioRepoFake(ProntuarioSuas prontuario) : IProntuarioSuasRepository
    {
        public void Adicionar(ProntuarioSuas p)
        {
        }

        public Task<ProntuarioSuas?> ObterPorIdAsync(ProntuarioSuasId id, CancellationToken cancellationToken)
            => Task.FromResult<ProntuarioSuas?>(prontuario.Id == id ? prontuario : null);

        public Task<ProntuarioSuas?> ObterPorFamiliaAsync(Guid familiaId, CancellationToken cancellationToken)
            => Task.FromResult<ProntuarioSuas?>(prontuario.FamiliaId == familiaId ? prontuario : null);
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class CurrentUserFixo(string? userId) : ICurrentUser
    {
        public string? UserId => userId;

        public string? UserName => "Principal";

        public string? IpAddress => "203.0.113.7";
    }
}
