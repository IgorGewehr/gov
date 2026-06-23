using FluentAssertions;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Cidadao.Application.Autenticacao;
using Tensorroot.Gov.Modules.Cidadao.Application.Internal;
using Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;
using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Seguranca;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.PortalCidadao;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.PortalCidadao;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Cidadao.Tests;

/// <summary>
/// Nucleo de SEGURANCA do Portal do Cidadao (ator EXTERNO). Prova o isolamento dado-proprio A PROVA DE
/// BALA: o cidadao ve o PROPRIO debito/divida/DAM/processo; NAO ve o de outro (mesmo forjando id —
/// anti-IDOR); SEM conta-cidadao e negado (e a tentativa selada na trilha LGPD); e nada vaza entre
/// tenants. Cobre tambem a anti-enumeracao do login.
/// </summary>
public sealed class PortalCidadaoSegurancaTests : PortalCidadaoTestBase
{
    private const string CpfA = "52998224725";
    private const string CpfB = "39053344705";

    private static readonly Guid ContaInexistente = Guid.Parse("dddddddd-0000-0000-0000-000000000009");

    private static readonly RegraEncargosDivida RegraEncargos =
        RegraEncargosDivida.Criar(2m, 1m, 0m, "Lei municipal");

    // ---------- Autenticacao ----------

    [Fact] // Cadastro local + login emitem token tipo=cidadao; o documento e normalizado para digitos.
    public async Task Cadastro_e_login_local_emitem_token_do_cidadao()
    {
        await using var ctx = CriarCidadao(TenantA);
        var contas = new CidadaoContaRepository(ctx);
        var hasher = new SenhaHasherCidadao(workFactor: 4);

        var registrar = new RegistrarCidadaoHandler(contas, hasher, ctx, new TenantContextFake(TenantA));
        var contaId = await registrar.Handle(new RegistrarCidadaoCommand("529.982.247-25", "Fulano", "senha-forte-1"), default);

        var emissor = new EmissorTokenCidadao(
            Microsoft.Extensions.Options.Options.Create(new JwtCidadaoOptions
            {
                Secret = "segredo-de-teste-com-mais-de-32-bytes-aqui!!",
            }),
            TimeProvider.System);

        var autenticar = new AutenticarCidadaoHandler(contas, hasher, emissor);
        var resultado = await autenticar.Handle(new AutenticarCidadaoCommand("52998224725", "senha-forte-1"), default);

        contaId.Should().NotBeEmpty();
        resultado.ContaId.Should().Be(contaId);
        resultado.Token.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact] // Login com documento inexistente falha com mensagem GENERICA (anti-enumeracao por timing).
    public async Task Login_documento_inexistente_falha_generico()
    {
        await using var ctx = CriarCidadao(TenantA);
        var contas = new CidadaoContaRepository(ctx);
        var hasher = new SenhaHasherCidadao(workFactor: 4);
        var emissor = EmissorDeTeste();

        var autenticar = new AutenticarCidadaoHandler(contas, hasher, emissor);
        var acao = async () => await autenticar.Handle(new AutenticarCidadaoCommand(CpfA, "qualquer"), default);

        await acao.Should().ThrowAsync<AutenticacaoCidadaoFalhouException>();
    }

    [Fact] // Documento ja cadastrado no tenant e recusado (unicidade (TenantId, Documento)).
    public async Task Cadastro_duplicado_e_recusado()
    {
        await using var ctx = CriarCidadao(TenantA);
        var contas = new CidadaoContaRepository(ctx);
        var hasher = new SenhaHasherCidadao(workFactor: 4);
        var registrar = new RegistrarCidadaoHandler(contas, hasher, ctx, new TenantContextFake(TenantA));

        await registrar.Handle(new RegistrarCidadaoCommand(CpfA, "Fulano", "senha-forte-1"), default);
        var acao = async () => await registrar.Handle(new RegistrarCidadaoCommand(CpfA, "Fulano de novo", "outra-senha-9"), default);

        await acao.Should().ThrowAsync<CidadaoJaCadastradoException>();
    }

    // ---------- Dado-proprio: resolvedor ----------

    [Fact] // SEM conta-cidadao para o sub => NEGADO (deny-by-default) e selado na trilha LGPD.
    public async Task Sem_conta_e_negado_e_auditado()
    {
        await using var ctx = CriarCidadao(TenantA);
        var spy = new RegistroAcessoSpy();
        var resolvedor = new ResolvedorPessoaDoCidadaoAutenticado(
            new CurrentUserCidadaoFake(ContaInexistente), new CidadaoContaRepository(ctx), spy);

        var acao = async () => await resolvedor.ResolverPessoaAtualAsync(default);

        await acao.Should().ThrowAsync<CidadaoSemVinculoException>();
        spy.Negativas.Should().Be(1);
    }

    [Fact] // Sub ausente (nao autenticado) => NEGADO e auditado.
    public async Task Sem_principal_autenticado_e_negado_e_auditado()
    {
        await using var ctx = CriarCidadao(TenantA);
        var spy = new RegistroAcessoSpy();
        var resolvedor = new ResolvedorPessoaDoCidadaoAutenticado(
            new CurrentUserCidadaoFake(sub: null), new CidadaoContaRepository(ctx), spy);

        var acao = async () => await resolvedor.ResolverPessoaAtualAsync(default);

        await acao.Should().ThrowAsync<CidadaoSemVinculoException>();
        spy.Negativas.Should().Be(1);
    }

    [Fact] // Conta INATIVA => NEGADO (deny-by-default).
    public async Task Conta_inativa_e_negada()
    {
        Guid contaId;
        await using (var ctx = CriarCidadao(TenantA))
        {
            var conta = CidadaoConta.CriarLocal(TenantA, CpfA, "Fulano", new SenhaHasherCidadao(4).Hash("senha-forte-1"));
            conta.Desativar();
            ctx.Contas.Add(conta);
            await ctx.SaveChangesAsync();
            contaId = conta.Id.Value;
        }

        await using var ctx2 = CriarCidadao(TenantA);
        var spy = new RegistroAcessoSpy();
        var resolvedor = new ResolvedorPessoaDoCidadaoAutenticado(
            new CurrentUserCidadaoFake(contaId), new CidadaoContaRepository(ctx2), spy);

        var acao = async () => await resolvedor.ResolverPessoaAtualAsync(default);

        await acao.Should().ThrowAsync<CidadaoSemVinculoException>();
        spy.Negativas.Should().Be(1);
    }

    [Fact] // ISOLAMENTO POR TENANT: a conta criada no tenant B nao e resolvivel do contexto do tenant A.
    public async Task Conta_nao_vaza_entre_tenants()
    {
        Guid contaB;
        await using (var ctxB = CriarCidadao(TenantB))
        {
            var conta = CidadaoConta.CriarLocal(TenantB, CpfA, "Fulano B", new SenhaHasherCidadao(4).Hash("senha-forte-1"));
            ctxB.Contas.Add(conta);
            await ctxB.SaveChangesAsync();
            contaB = conta.Id.Value;
        }

        await using var ctxA = CriarCidadao(TenantA);
        var spy = new RegistroAcessoSpy();
        var resolvedor = new ResolvedorPessoaDoCidadaoAutenticado(
            new CurrentUserCidadaoFake(contaB), new CidadaoContaRepository(ctxA), spy);

        var acao = async () => await resolvedor.ResolverPessoaAtualAsync(default);

        await acao.Should().ThrowAsync<CidadaoSemVinculoException>();
        spy.Negativas.Should().Be(1);
    }

    // ---------- Meus debitos / divida (Tributos via Contracts) ----------

    [Fact] // Cidadao ve o PROPRIO lancamento em aberto; NAO ve o de outro contribuinte.
    public async Task Cidadao_ve_proprio_debito_e_nao_o_de_outro()
    {
        Guid contaA;
        await using (var ctxCid = CriarCidadao(TenantA))
        {
            var conta = CidadaoConta.CriarLocal(TenantA, CpfA, "Fulano", new SenhaHasherCidadao(4).Hash("senha-forte-1"));
            ctxCid.Contas.Add(conta);
            await ctxCid.SaveChangesAsync();
            contaA = conta.Id.Value;
        }

        await using (var ctxTrib = CriarTributos(TenantA))
        {
            var contribA = Contribuinte.PessoaFisica(TenantA, Cpf.Create(CpfA), "Fulano");
            var contribB = Contribuinte.PessoaFisica(TenantA, Cpf.Create(CpfB), "Sicrano");
            ctxTrib.Contribuintes.AddRange(contribA, contribB);
            ctxTrib.Lancamentos.Add(Lancamento.Lancar(TenantA, contribA.Id, TipoTributo.Iptu, Competencia.De(2026, 1), ValorMonetario.De(500m), new DateOnly(2026, 3, 10)));
            ctxTrib.Lancamentos.Add(Lancamento.Lancar(TenantA, contribB.Id, TipoTributo.Iptu, Competencia.De(2026, 1), ValorMonetario.De(999m), new DateOnly(2026, 3, 10)));
            await ctxTrib.SaveChangesAsync();
        }

        await using var ctxCid2 = CriarCidadao(TenantA);
        await using var ctxTrib2 = CriarTributos(TenantA);
        var handler = new ObterMeusDebitosHandler(
            Resolvedor(ctxCid2, contaA),
            new ConsultaCidadaoEmEscopoDedicadoFake(new ConsultaTributariaCidadao(ctxTrib2)));

        var debitos = await handler.Handle(new ObterMeusDebitosQuery(), default);

        debitos.Should().HaveCount(1);
        debitos[0].ValorPrincipal.Should().Be(500m); // SO o de Fulano (CpfA) — nunca o de Sicrano (999).
    }

    [Fact] // Minha divida ativa: posicao do PROPRIO contribuinte, com valor atualizado pela regra.
    public async Task Cidadao_ve_propria_divida_ativa()
    {
        Guid contaA;
        await using (var ctxCid = CriarCidadao(TenantA))
        {
            var conta = CidadaoConta.CriarLocal(TenantA, CpfA, "Fulano", new SenhaHasherCidadao(4).Hash("senha-forte-1"));
            ctxCid.Contas.Add(conta);
            await ctxCid.SaveChangesAsync();
            contaA = conta.Id.Value;
        }

        await using (var ctxTrib = CriarTributos(TenantA))
        {
            var contribA = Contribuinte.PessoaFisica(TenantA, Cpf.Create(CpfA), "Fulano");
            ctxTrib.Contribuintes.Add(contribA);
            ctxTrib.DividasAtivas.Add(DividaAtiva.Inscrever(
                TenantA, contribA.Id, LancamentoId.New(), TipoTributo.Iptu, ValorMonetario.De(1000m),
                new DateOnly(2025, 1, 10), new DateOnly(2025, 1, 10), new DateOnly(2025, 6, 1),
                1, "IPTU 2025", "Lei municipal", RegraEncargos));
            await ctxTrib.SaveChangesAsync();
        }

        await using var ctxCid2 = CriarCidadao(TenantA);
        await using var ctxTrib2 = CriarTributos(TenantA);
        var handler = new ObterMinhaDividaAtivaHandler(
            Resolvedor(ctxCid2, contaA),
            new ConsultaCidadaoEmEscopoDedicadoFake(new ConsultaTributariaCidadao(ctxTrib2)),
            TimeProvider.System);

        var dividas = await handler.Handle(new ObterMinhaDividaAtivaQuery(new DateOnly(2026, 1, 10)), default);

        dividas.Should().HaveCount(1);
        dividas[0].ValorOriginario.Should().Be(1000m);
        dividas[0].ValorAtualizado.Should().BeGreaterThanOrEqualTo(1000m);
    }

    // ---------- 2a via DAM (anti-IDOR) ----------

    [Fact] // 2a via do PROPRIO DAM funciona; DAM de OUTRO contribuinte retorna null (anti-IDOR), mesmo
            // que o cidadao "forje" o damId.
    public async Task Segunda_via_dam_revalida_titularidade_anti_idor()
    {
        Guid contaA;
        Guid damDeOutro;
        Guid meuDam;

        await using (var ctxCid = CriarCidadao(TenantA))
        {
            var conta = CidadaoConta.CriarLocal(TenantA, CpfA, "Fulano", new SenhaHasherCidadao(4).Hash("senha-forte-1"));
            ctxCid.Contas.Add(conta);
            await ctxCid.SaveChangesAsync();
            contaA = conta.Id.Value;
        }

        await using (var ctxTrib = CriarTributos(TenantA))
        {
            var contribA = Contribuinte.PessoaFisica(TenantA, Cpf.Create(CpfA), "Fulano");
            var contribB = Contribuinte.PessoaFisica(TenantA, Cpf.Create(CpfB), "Sicrano");
            ctxTrib.Contribuintes.AddRange(contribA, contribB);

            var damA = Dam.Gerar(TenantA, LancamentoId.New(), contribA.Id, ValorMonetario.De(300m), 3, new DateOnly(2026, 3, 10));
            var damB = Dam.Gerar(TenantA, LancamentoId.New(), contribB.Id, ValorMonetario.De(700m), 1, new DateOnly(2026, 3, 10));
            ctxTrib.Dams.AddRange(damA, damB);
            await ctxTrib.SaveChangesAsync();
            meuDam = damA.Id.Value;
            damDeOutro = damB.Id.Value;
        }

        await using var ctxCid2 = CriarCidadao(TenantA);
        await using var ctxTrib2 = CriarTributos(TenantA);
        var handler = new ObterSegundaViaDamHandler(
            Resolvedor(ctxCid2, contaA),
            new ConsultaCidadaoEmEscopoDedicadoFake(new ConsultaTributariaCidadao(ctxTrib2)));

        var meu = await handler.Handle(new ObterSegundaViaDamQuery(meuDam), default);
        var forjado = await handler.Handle(new ObterSegundaViaDamQuery(damDeOutro), default);

        meu.Should().NotBeNull();
        meu!.ValorTotal.Should().Be(300m);
        meu.Parcelas.Should().HaveCount(3);
        // ANTI-IDOR: o DAM de Sicrano e indistinguivel de inexistente para Fulano.
        forjado.Should().BeNull();
    }

    // ---------- Meus processos (Protocolo via Contracts; nivel de acesso + IDOR) ----------

    [Fact] // Cidadao ve o PROPRIO processo PUBLICO; nao ve o de outro nem um sigiloso seu (anti-IDOR + nivel).
    public async Task Meus_processos_respeitam_titularidade_e_nivel_de_acesso()
    {
        Guid contaA;
        await using (var ctxCid = CriarCidadao(TenantA))
        {
            var conta = CidadaoConta.CriarLocal(TenantA, CpfA, "Fulano", new SenhaHasherCidadao(4).Hash("senha-forte-1"));
            ctxCid.Contas.Add(conta);
            await ctxCid.SaveChangesAsync();
            contaA = conta.Id.Value;
        }

        Guid publicoMeu;
        Guid sigilosoMeu;
        Guid publicoDeOutro;
        await using (var ctxProto = CriarProtocolo(TenantA))
        {
            var meuPublico = Processo.Autuar(TenantA, new Nup("NUP-1"), new Classificacao("CLS"), NivelDeAcesso.Publico, null, null, null, new DateOnly(2026, 1, 10));
            meuPublico.RegistrarInteressado(CpfA);

            var meuSigiloso = Processo.Autuar(TenantA, new Nup("NUP-2"), new Classificacao("CLS"), NivelDeAcesso.Sigiloso, null, null, null, new DateOnly(2026, 1, 11));
            meuSigiloso.RegistrarInteressado(CpfA);

            var deOutro = Processo.Autuar(TenantA, new Nup("NUP-3"), new Classificacao("CLS"), NivelDeAcesso.Publico, null, null, null, new DateOnly(2026, 1, 12));
            deOutro.RegistrarInteressado(CpfB);

            ctxProto.Processos.AddRange(meuPublico, meuSigiloso, deOutro);
            await ctxProto.SaveChangesAsync();
            publicoMeu = meuPublico.Id.Value;
            sigilosoMeu = meuSigiloso.Id.Value;
            publicoDeOutro = deOutro.Id.Value;
        }

        await using var ctxCid2 = CriarCidadao(TenantA);
        await using var ctxProto2 = CriarProtocolo(TenantA);
        var consultaDedicada = new ConsultaCidadaoEmEscopoDedicadoFake(new ConsultaProcessoCidadao(ctxProto2));
        var lista = new ObterMeusProcessosHandler(Resolvedor(ctxCid2, contaA), consultaDedicada);
        var detalhe = new ObterMeuProcessoHandler(Resolvedor(ctxCid2, contaA), consultaDedicada);

        var meus = await lista.Handle(new ObterMeusProcessosQuery(), default);
        var verMeuPublico = await detalhe.Handle(new ObterMeuProcessoQuery(publicoMeu), default);
        var verMeuSigiloso = await detalhe.Handle(new ObterMeuProcessoQuery(sigilosoMeu), default);
        var verDeOutro = await detalhe.Handle(new ObterMeuProcessoQuery(publicoDeOutro), default);

        meus.Should().ContainSingle(p => p.ProcessoId == publicoMeu); // so o publico do proprio interessado.
        verMeuPublico.Should().NotBeNull();
        verMeuSigiloso.Should().BeNull();  // sigiloso nao vai ao portal (nivel de acesso).
        verDeOutro.Should().BeNull();      // anti-IDOR: processo de outro interessado.
    }

    private static ResolvedorPessoaDoCidadaoAutenticado Resolvedor(CidadaoDbContext ctx, Guid contaId)
        => new(new CurrentUserCidadaoFake(contaId), new CidadaoContaRepository(ctx), new RegistroAcessoSpy());

    private static EmissorTokenCidadao EmissorDeTeste()
        => new(
            Microsoft.Extensions.Options.Options.Create(new JwtCidadaoOptions
            {
                Secret = "segredo-de-teste-com-mais-de-32-bytes-aqui!!",
            }),
            TimeProvider.System);
}
