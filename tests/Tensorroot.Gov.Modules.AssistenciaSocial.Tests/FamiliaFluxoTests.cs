using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Familia"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Familia.rules.md, sobre SQLite em memoria com
/// auditoria e isolamento por tenant.
/// </summary>
public sealed class FamiliaFluxoTests : AssistenciaSocialTestBase
{
    private static readonly Guid CrasCentro = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static MembroFamiliar Responsavel(decimal renda, bool ehPcd = false, string cpf = CpfResponsavel)
        => MembroFamiliar.Registrar(Cpf.Create(cpf), Parentesco.ResponsavelFamiliar, new DateOnly(1980, 1, 1), ValorMonetario.De(renda), ehPcd);

    private static MembroFamiliar Membro(Parentesco parentesco, decimal renda, string cpf = CpfMembro)
        => MembroFamiliar.Registrar(Cpf.Create(cpf), parentesco, new DateOnly(2010, 1, 1), ValorMonetario.De(renda), false);

    private static Familia NovaFamilia(
        string territorio = "Centro",
        DateOnly? dataReferenciamento = null,
        string nis = NisValido,
        IReadOnlyCollection<MembroFamiliar>? membros = null)
        => Familia.Referenciar(
            TenantA,
            Nis.Create(nis),
            Cpf.Create(CpfResponsavel),
            Endereco(territorio),
            CrasCentro,
            territorio,
            membros ?? [Responsavel(1000m), Membro(Parentesco.Filho, 0m)],
            dataReferenciamento ?? new DateOnly(2026, 1, 10));

    // ---------- Invariantes ----------

    [Fact] // I-1: NIS obrigatorio e valido no referenciamento.
    public void Invariante_1_nis_invalido_e_rejeitado()
    {
        // DV correto de 1234567891 e 9; o terminador 0 torna o NIS invalido.
        var acao = () => Nis.Create("12345678910");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-2 + Cenario 2/B-3: endereco fora do territorio do CRAS e rejeitado, sem evento.
    public void Invariante_2_endereco_fora_do_territorio_e_rejeitado()
    {
        var acao = () => Familia.Referenciar(
            TenantA,
            Nis.Create(NisValido),
            Cpf.Create(CpfResponsavel),
            Endereco("Bairro Norte"),
            CrasCentro,
            "Centro",
            [Responsavel(500m)],
            new DateOnly(2026, 1, 10));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*territorio*");
    }

    [Fact] // I-3 + Cenario 4: renda per capita = soma das rendas / numero de membros.
    public void Invariante_3_renda_per_capita_e_a_media()
    {
        // (1200 + 0 + 0) / 3 = 400.
        var familia = NovaFamilia(membros:
        [
            Responsavel(1200m),
            Membro(Parentesco.Conjuge, 0m, CpfMembro),
            Membro(Parentesco.Filho, 0m, "11144477735"),
        ]);

        familia.RendaPerCapita.Valor.Should().Be(400m);
    }

    [Fact] // I-4 + B-2: composicao vazia e rejeitada (divisao por zero proibida).
    public void Invariante_4_composicao_vazia_e_rejeitada()
    {
        var acao = () => Familia.Referenciar(
            TenantA, Nis.Create(NisValido), Cpf.Create(CpfResponsavel), Endereco(), CrasCentro, "Centro",
            [], new DateOnly(2026, 1, 10));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*ao menos um membro*");
    }

    [Fact] // I-5 + B-5: vigencia vence estritamente apos marco + 24 meses; no marco exato ainda e valida.
    public void Invariante_5_vigencia_vence_estritamente_apos_24_meses()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2026, 1, 10));

        // B-5: exatamente no marco + 24 meses => ainda valida.
        familia.VigenciaVencida(new DateOnly(2028, 1, 10)).Should().BeFalse();
        // Um dia depois => vencida.
        familia.VigenciaVencida(new DateOnly(2028, 1, 11)).Should().BeTrue();
    }

    [Fact] // I-6: familia AtualizacaoVencida nao esta apta a novos beneficios.
    public void Invariante_6_familia_vencida_nao_esta_apta()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2020, 1, 10));
        familia.ProcessarVigenciaCadastral(new DateOnly(2026, 1, 10));

        familia.Situacao.Should().Be(SituacaoFamilia.AtualizacaoVencida);
        familia.EstaAptaANovosBeneficios(new DateOnly(2026, 1, 10)).Should().BeFalse();
    }

    [Fact] // I-7: atualizacao de renda renova o marco cadastral imediatamente.
    public void Invariante_7_atualizar_renda_renova_marco_cadastral()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2024, 1, 10));

        familia.AtualizarRendaFamiliar([Responsavel(2000m)], new DateOnly(2026, 6, 21));

        familia.DataUltimaAtualizacaoCadastral.Should().Be(new DateOnly(2026, 6, 21));
        familia.RendaPerCapita.Valor.Should().Be(2000m);
    }

    [Fact] // I-8: referenciamento nasce em Referenciada e emite FamiliaReferenciada (NIS mascarado).
    public void Invariante_8_referenciamento_emite_evento_com_nis_mascarado()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2026, 3, 1));

        familia.Situacao.Should().Be(SituacaoFamilia.Referenciada);
        familia.DataReferenciamento.Should().Be(new DateOnly(2026, 3, 1));
        var evento = familia.DomainEvents.OfType<FamiliaReferenciada>().Should().ContainSingle().Subject;
        evento.NisMascarado.Should().NotContain(NisValido[..8]);
        evento.NisMascarado.Should().EndWith(NisValido[8..]);
        evento.Territorio.Should().Be("Centro");
    }

    [Fact] // I-10: CPF do responsavel invalido e rejeitado no referenciamento.
    public void Invariante_10_cpf_invalido_e_rejeitado()
    {
        var acao = () => Cpf.Create("00000000000");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-11 + B-4: dois Responsaveis Familiares e rejeitado.
    public void Invariante_11_dois_responsaveis_e_rejeitado()
    {
        var acao = () => NovaFamilia(membros:
        [
            Responsavel(1000m, cpf: CpfResponsavel),
            Responsavel(1000m, cpf: CpfMembro),
        ]);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*Responsavel Familiar*");
    }

    [Fact] // I-12: AtualizacaoVencida so vai para Regularizada via regularizacao cadastral.
    public void Invariante_12_regularizar_exige_cadastro_vencido()
    {
        var familia = NovaFamilia();

        // Referenciada (nao vencida) nao pode regularizar diretamente.
        ((Action)(() => familia.RegularizarCadastro(new DateOnly(2026, 6, 21))))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // B-9: renda familiar total = 0 => RendaPerCapita 0 (valida, nao negativa).
    public void Borda_9_renda_zero_e_valida()
    {
        var familia = NovaFamilia(membros: [Responsavel(0m), Membro(Parentesco.Filho, 0m)]);

        familia.RendaPerCapita.Valor.Should().Be(0m);
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 5: Referenciada --ProcessarVigenciaCadastral--> AtualizacaoVencida.
    public void Transicao_processar_vigencia_vence_cadastro()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2020, 1, 10));

        familia.ProcessarVigenciaCadastral(new DateOnly(2026, 1, 10));

        familia.Situacao.Should().Be(SituacaoFamilia.AtualizacaoVencida);
    }

    [Fact] // Cenario 7 + B-6: vigencia valida => permanece Referenciada (idempotente).
    public void Transicao_processar_vigencia_valida_mantem_referenciada()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2026, 1, 10));

        familia.ProcessarVigenciaCadastral(new DateOnly(2026, 6, 21));

        familia.Situacao.Should().Be(SituacaoFamilia.Referenciada);
    }

    [Fact] // B-6: ProcessarVigenciaCadastral sobre familia ja vencida e idempotente.
    public void Transicao_processar_vigencia_e_idempotente_quando_vencida()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2020, 1, 10));
        familia.ProcessarVigenciaCadastral(new DateOnly(2026, 1, 10));

        familia.ProcessarVigenciaCadastral(new DateOnly(2026, 2, 10));

        familia.Situacao.Should().Be(SituacaoFamilia.AtualizacaoVencida);
    }

    [Fact] // Cenario 6 + I-12: AtualizacaoVencida --AtualizarRendaFamiliar--> Regularizada.
    public void Transicao_atualizar_renda_regulariza_familia_vencida()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2020, 1, 10));
        familia.ProcessarVigenciaCadastral(new DateOnly(2026, 1, 10));
        familia.Situacao.Should().Be(SituacaoFamilia.AtualizacaoVencida);

        familia.AtualizarRendaFamiliar([Responsavel(900m)], new DateOnly(2026, 6, 21));

        familia.Situacao.Should().Be(SituacaoFamilia.Regularizada);
        familia.RendaPerCapita.Valor.Should().Be(900m);
        familia.DataUltimaAtualizacaoCadastral.Should().Be(new DateOnly(2026, 6, 21));
    }

    [Fact] // AtualizacaoVencida --RegularizarCadastro--> Regularizada.
    public void Transicao_regularizar_cadastro_de_vencida_para_regularizada()
    {
        var familia = NovaFamilia(dataReferenciamento: new DateOnly(2020, 1, 10));
        familia.ProcessarVigenciaCadastral(new DateOnly(2026, 1, 10));

        familia.RegularizarCadastro(new DateOnly(2026, 6, 21));

        familia.Situacao.Should().Be(SituacaoFamilia.Regularizada);
        familia.DataUltimaAtualizacaoCadastral.Should().Be(new DateOnly(2026, 6, 21));
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 1: referenciamento persiste com auditoria e mascara o NIS no evento.
    public async Task Cenario_1_referenciamento_persiste_com_auditoria()
    {
        FamiliaId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var familia = NovaFamilia();
            id = familia.Id;
            contexto.Familias.Add(familia);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var familia = await contexto.Familias.Include(f => f.Membros).SingleAsync(f => f.Id == id);
            familia.Situacao.Should().Be(SituacaoFamilia.Referenciada);
            familia.TenantId.Should().Be(TenantA);
            familia.Membros.Should().HaveCount(2);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar a mutacao da familia");
        }
    }

    [Fact] // Cenario 10: unicidade de NIS por tenant (indice unico (TenantId, Nis)).
    public async Task Cenario_10_nis_e_unico_por_tenant()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.Familias.Add(NovaFamilia(nis: NisValido));
        await contexto.SaveChangesAsync();

        contexto.Familias.Add(Familia.Referenciar(
            TenantA, Nis.Create(NisValido), Cpf.Create(CpfMembro), Endereco(), CrasCentro, "Centro",
            [Responsavel(700m, cpf: CpfMembro)], new DateOnly(2026, 2, 1)));

        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Cenario 8: consulta por territorio e tenant-scoped (Global Query Filter).
    public async Task Cenario_8_consulta_por_territorio_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Familias.Add(NovaFamilia(territorio: "Centro", nis: NisValido));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Familias.Where(f => f.Territorio == "Centro").ToListAsync())
                .Should().BeEmpty();
        }
    }
}
