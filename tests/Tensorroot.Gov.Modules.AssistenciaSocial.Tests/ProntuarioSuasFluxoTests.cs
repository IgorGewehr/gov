using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Xunit;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="ProntuarioSuas"/>: invariantes, cada transicao
/// da maquina de estados e os cenarios BDD de ProntuarioSuas.rules.md (acompanhamento sigiloso,
/// trilha de acesso imutavel, PAIF↔CRAS / PAEFI↔CREAS), sobre SQLite em memoria com isolamento por tenant.
/// </summary>
public sealed class ProntuarioSuasFluxoTests : AssistenciaSocialTestBase
{
    private static readonly Guid FamiliaId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid CrasId = Guid.Parse("cccccccc-0000-0000-0000-0000000000c1");
    private static readonly Guid ProfissionalId = Guid.Parse("cccccccc-0000-0000-0000-0000000000f1");
    private static readonly Guid UsuarioId = Guid.Parse("cccccccc-0000-0000-0000-0000000000e1");
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static ProntuarioSuas NovoProntuario(Guid? familiaId = null)
        => ProntuarioSuas.Abrir(TenantA, familiaId ?? FamiliaId, CrasId, Hoje);

    // ---------- Invariantes ----------

    [Fact] // I-1 + B-1: familia obrigatoria na abertura.
    public void Invariante_1_familia_obrigatoria_na_abertura()
    {
        ((Action)(() => ProntuarioSuas.Abrir(TenantA, Guid.Empty, CrasId, Hoje)))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // I-1 + B-1: unidade obrigatoria na abertura.
    public void Invariante_1_unidade_obrigatoria_na_abertura()
    {
        ((Action)(() => ProntuarioSuas.Abrir(TenantA, FamiliaId, Guid.Empty, Hoje)))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // I-2 + Cenario 1: abertura nasce em Aberto com DataAbertura = hoje.
    public void Invariante_2_abertura_nasce_em_aberto()
    {
        var prontuario = NovoProntuario();

        prontuario.Situacao.Should().Be(SituacaoProntuario.Aberto);
        prontuario.DataAbertura.Should().Be(Hoje);
    }

    [Fact] // I-3 + Cenario 4: prontuario encerrado nao admite novos atendimentos.
    public void Invariante_3_encerrado_nao_admite_atendimento()
    {
        var prontuario = NovoProntuario();
        prontuario.EncerrarAcompanhamento("Objetivos alcancados", Hoje);

        ((Action)(() => prontuario.RegistrarAtendimento(TipoServico.Paif, Hoje, "atendimento", ProfissionalId, TipoUnidadeAtendimento.Cras)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-4 + Cenario 3 + B-2: PAEFI em unidade CRAS e rejeitado, sem evento.
    public void Invariante_4_paefi_em_cras_e_rejeitado()
    {
        var prontuario = NovoProntuario();

        ((Action)(() => prontuario.RegistrarAtendimento(TipoServico.Paefi, Hoje, "atendimento", ProfissionalId, TipoUnidadeAtendimento.Cras)))
            .Should().Throw<InvalidOperationException>().WithMessage("*PAEFI*");
        prontuario.Registros.Should().BeEmpty();
        prontuario.DomainEvents.OfType<AtendimentoRegistrado>().Should().BeEmpty();
    }

    [Fact] // I-4 + B-2: PAIF em unidade CREAS e rejeitado.
    public void Invariante_4_paif_em_creas_e_rejeitado()
    {
        var prontuario = NovoProntuario();

        ((Action)(() => prontuario.RegistrarAtendimento(TipoServico.Paif, Hoje, "atendimento", ProfissionalId, TipoUnidadeAtendimento.Creas)))
            .Should().Throw<InvalidOperationException>().WithMessage("*PAIF*");
    }

    [Fact] // I-5 + Cenario 2: PAIF em CRAS adiciona registro e emite AtendimentoRegistrado.
    public void Invariante_5_paif_em_cras_emite_evento()
    {
        var prontuario = NovoProntuario();

        prontuario.RegistrarAtendimento(TipoServico.Paif, Hoje, "Atendimento PAIF", ProfissionalId, TipoUnidadeAtendimento.Cras);

        prontuario.Registros.Should().ContainSingle();
        prontuario.Situacao.Should().Be(SituacaoProntuario.Aberto);
        prontuario.DomainEvents.OfType<AtendimentoRegistrado>().Should().ContainSingle()
            .Which.Servico.Should().Be(TipoServico.Paif);
    }

    [Fact] // I-6 + Cenario 5: encerramento passa a Encerrado e emite AcompanhamentoEncerrado.
    public void Invariante_6_encerramento_emite_evento()
    {
        var prontuario = NovoProntuario();

        prontuario.EncerrarAcompanhamento("Mudanca de municipio", Hoje);

        prontuario.Situacao.Should().Be(SituacaoProntuario.Encerrado);
        prontuario.DataEncerramento.Should().Be(Hoje);
        prontuario.MotivoEncerramento.Should().Be("Mudanca de municipio");
        prontuario.DomainEvents.OfType<AcompanhamentoEncerrado>().Should().ContainSingle();
    }

    [Fact] // I-6 + Cenario 6: encerramento sem motivo e rejeitado.
    public void Invariante_6_encerramento_sem_motivo_e_rejeitado()
    {
        var prontuario = NovoProntuario();

        ((Action)(() => prontuario.EncerrarAcompanhamento("  ", Hoje))).Should().Throw<ArgumentException>();
        prontuario.Situacao.Should().Be(SituacaoProntuario.Aberto);
    }

    [Fact] // I-7 + Cenario 7/8 + B-6: acesso sem motivo e rejeitado; com motivo registra na trilha.
    public void Invariante_7_acesso_exige_motivo_e_registra_trilha()
    {
        var prontuario = NovoProntuario();

        ((Action)(() => prontuario.RegistrarAcesso(UsuarioId, "  ", DateTime.UtcNow))).Should().Throw<ArgumentException>();

        var quando = new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc);
        prontuario.RegistrarAcesso(UsuarioId, "Leitura para acompanhamento", quando);

        prontuario.Acessos.Should().ContainSingle();
        var acesso = prontuario.Acessos.Single();
        acesso.UsuarioId.Should().Be(UsuarioId);
        acesso.MotivoAcesso.Should().Be("Leitura para acompanhamento");
        acesso.DataHoraAcessoUtc.Should().Be(quando);
    }

    [Fact] // I-8: a trilha e append-only — multiplos acessos acumulam, sem sobrescrever.
    public void Invariante_8_trilha_e_append_only()
    {
        var prontuario = NovoProntuario();

        prontuario.RegistrarAcesso(UsuarioId, "Primeiro acesso", new DateTime(2026, 6, 21, 9, 0, 0, DateTimeKind.Utc));
        prontuario.RegistrarAcesso(UsuarioId, "Segundo acesso", new DateTime(2026, 6, 21, 11, 0, 0, DateTimeKind.Utc));

        prontuario.Acessos.Should().HaveCount(2);
        prontuario.Acessos.Select(a => a.MotivoAcesso).Should().Contain(["Primeiro acesso", "Segundo acesso"]);
    }

    [Fact] // I-10: violacao envolvendo crianca/adolescente e registrada (dado sensivel reforcado).
    public void Invariante_10_violacao_crianca_adolescente_e_registrada()
    {
        var prontuario = NovoProntuario();

        prontuario.RegistrarViolacao(TipoViolacaoDireito.TrabalhoInfantil, envolveCriancaAdolescente: true, Hoje);

        prontuario.Violacoes.Should().ContainSingle()
            .Which.EnvolveCriancaAdolescente.Should().BeTrue();
    }

    [Fact] // I-11 + B-4: prontuario encerrado nao admite novo encerramento.
    public void Invariante_11_encerrado_nao_admite_novo_encerramento()
    {
        var prontuario = NovoProntuario();
        prontuario.EncerrarAcompanhamento("Encerrado", Hoje);

        ((Action)(() => prontuario.EncerrarAcompanhamento("De novo", Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-12 + B-5: DefinirPlano e RegistrarViolacao exigem situacao Aberto.
    public void Invariante_12_plano_e_violacao_exigem_aberto()
    {
        var prontuario = NovoProntuario();
        prontuario.EncerrarAcompanhamento("Encerrado", Hoje);

        ((Action)(() => prontuario.DefinirPlano("Objetivos", Hoje))).Should().Throw<InvalidOperationException>();
        ((Action)(() => prontuario.RegistrarViolacao(TipoViolacaoDireito.Negligencia, false, Hoje)))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Aberto --DefinirPlano--> Aberto (mantem).
    public void Transicao_definir_plano_mantem_aberto()
    {
        var prontuario = NovoProntuario();

        prontuario.DefinirPlano("Fortalecer vinculos", Hoje, ["Frequentar SCFV", "Atualizar CadUnico"]);

        prontuario.Situacao.Should().Be(SituacaoProntuario.Aberto);
        prontuario.Plano.Should().NotBeNull();
        prontuario.Plano!.Objetivos.Should().Be("Fortalecer vinculos");
    }

    [Fact] // Aberto --RegistrarAtendimento (SCFV)--> Aberto (mantem).
    public void Transicao_registrar_scfv_mantem_aberto()
    {
        var prontuario = NovoProntuario();

        prontuario.RegistrarAtendimento(TipoServico.Scfv, Hoje, "Grupo de convivencia", ProfissionalId, TipoUnidadeAtendimento.Cras);

        prontuario.Situacao.Should().Be(SituacaoProntuario.Aberto);
        prontuario.Registros.Should().ContainSingle();
    }

    [Fact] // Aberto --EncerrarAcompanhamento--> Encerrado.
    public void Transicao_encerrar_de_aberto_para_encerrado()
    {
        var prontuario = NovoProntuario();

        prontuario.EncerrarAcompanhamento("Objetivos alcancados", Hoje);

        prontuario.Situacao.Should().Be(SituacaoProntuario.Encerrado);
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 7/11: atendimento + acesso persistem; a trilha imutavel permanece consultavel.
    public async Task Cenario_7_acesso_sigiloso_persiste_na_trilha()
    {
        ProntuarioSuasId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var prontuario = NovoProntuario();
            prontuario.RegistrarAtendimento(TipoServico.Paif, Hoje, "Atendimento sigiloso", ProfissionalId, TipoUnidadeAtendimento.Cras);
            prontuario.RegistrarViolacao(TipoViolacaoDireito.Negligencia, envolveCriancaAdolescente: true, Hoje);
            prontuario.RegistrarAcesso(UsuarioId, "Acompanhamento PAIF", new DateTime(2026, 6, 21, 14, 0, 0, DateTimeKind.Utc));
            id = prontuario.Id;
            contexto.Prontuarios.Add(prontuario);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var prontuario = await contexto.Prontuarios
                .Include(p => p.Acessos)
                .Include(p => p.Registros)
                .Include(p => p.Violacoes)
                .SingleAsync(p => p.Id == id);

            prontuario.TenantId.Should().Be(TenantA);
            prontuario.Acessos.Should().ContainSingle();
            prontuario.Registros.Should().ContainSingle();
            prontuario.Violacoes.Should().ContainSingle();

            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }
    }

    [Fact] // Cenario 10: conteudo sigiloso nao cruza tenants (Global Query Filter).
    public async Task Cenario_10_conteudo_sigiloso_nao_cruza_tenants()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Prontuarios.Add(NovoProntuario());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Prontuarios.Where(p => p.FamiliaId == FamiliaId).ToListAsync())
                .Should().BeEmpty();
        }
    }

    [Fact] // B-10: um prontuario por familia por tenant (indice unico (TenantId, FamiliaId)).
    public async Task Borda_10_prontuario_e_unico_por_familia_por_tenant()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.Prontuarios.Add(NovoProntuario());
        await contexto.SaveChangesAsync();

        contexto.Prontuarios.Add(NovoProntuario());

        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }
}
