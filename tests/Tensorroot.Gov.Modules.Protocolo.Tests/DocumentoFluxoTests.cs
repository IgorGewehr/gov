using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.Modules.Protocolo.Domain.Events;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Protocolo.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Documento"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Documento.rules.md, sobre SQLite em memoria com
/// auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class DocumentoFluxoTests : ProtocoloTestBase
{
    private const string HashValido = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e";
    private const string HashOutro = "0bcda32b57b277d9ad9f146ea591a6d40bf420404a011733cfb7b190d62c65bf";
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static CarimboDeTempo NovoCarimbo()
        => CarimboDeTempo.De(new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc), "AC TEMPO TESTE");

    private static Documento NovoDocumento(
        CriticidadeAto criticidade = CriticidadeAto.Baixa,
        NivelDeAcesso nivelAcesso = NivelDeAcesso.Publico,
        bool formatoPdfA = true,
        string hash = HashValido)
        => Documento.Criar(TenantA, Hash.De(hash), criticidade, nivelAcesso, formatoPdfA);

    private static Documento DocumentoJuntado(
        CriticidadeAto criticidade = CriticidadeAto.Baixa,
        string hash = HashValido)
    {
        var documento = NovoDocumento(criticidade, hash: hash);
        documento.Juntar(Guid.NewGuid(), Hoje);
        return documento;
    }

    // ---------- Invariantes ----------

    [Fact] // I-1 + Cenario 2 + B-2: documento nao-PDF/A e rejeitado.
    public void Invariante_1_documento_nao_pdfa_e_rejeitado()
    {
        var acao = () => Documento.Criar(TenantA, Hash.De(HashValido), CriticidadeAto.Baixa, NivelDeAcesso.Publico, formatoPdfA: false);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-1 + B-1: hash invalido (comprimento != 64) e rejeitado pelo VO.
    public void Invariante_1_hash_invalido_e_rejeitado()
    {
        var acao = () => Hash.De("abc123");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-2 + Cenario 1: juntada vincula ao processo, passa a Juntado e emite DocumentoJuntado.
    public void Invariante_2_juntada_emite_evento_e_passa_a_Juntado()
    {
        var documento = NovoDocumento();
        var processoId = Guid.NewGuid();

        documento.Juntar(processoId, Hoje);

        documento.Situacao.Should().Be(SituacaoDocumento.Juntado);
        documento.ProcessoId.Should().Be(processoId);
        documento.DataJuntada.Should().Be(Hoje);
        documento.DomainEvents.OfType<DocumentoJuntado>().Should().ContainSingle();
    }

    [Fact] // I-2: juntar sem processo de destino e rejeitado.
    public void Invariante_2_juntar_sem_processo_e_rejeitado()
    {
        var documento = NovoDocumento();

        var acao = () => documento.Juntar(Guid.Empty, Hoje);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-3/I-4 + Cenario 3 + B-3: juntar duas vezes falha (juntado e imutavel; sem exclusao).
    public void Invariante_3_juntar_documento_ja_juntado_falha()
    {
        var documento = DocumentoJuntado();

        var acao = () => documento.Juntar(Guid.NewGuid(), Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5/I-6 + Cenario 5 + B-6: criticidade Alta rejeita assinatura simples.
    public void Invariante_5_criticidade_alta_rejeita_assinatura_simples()
    {
        var documento = DocumentoJuntado(CriticidadeAto.Alta);

        var acao = () => documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaSimples, NovoCarimbo());

        acao.Should().Throw<InvalidOperationException>();
        documento.Situacao.Should().Be(SituacaoDocumento.Juntado);
    }

    [Fact] // B-6: criticidade Alta rejeita assinatura avancada (exige qualificada).
    public void Borda_6_criticidade_alta_rejeita_assinatura_avancada()
    {
        var documento = DocumentoJuntado(CriticidadeAto.Alta);

        var acao = () => documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaAvancada, NovoCarimbo());

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // B-7: assinatura de nivel superior ao minimo e permitida (Qualificada em criticidade Baixa).
    public void Borda_7_assinatura_de_nivel_superior_e_permitida()
    {
        var documento = DocumentoJuntado(CriticidadeAto.Baixa);

        documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaQualificada, NovoCarimbo());

        documento.Situacao.Should().Be(SituacaoDocumento.Assinado);
    }

    [Fact] // I-7 + B-8: assinar documento Rascunho (nao juntado) falha.
    public void Invariante_7_assinar_rascunho_falha()
    {
        var documento = NovoDocumento();

        var acao = () => documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaSimples, NovoCarimbo());

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + Cenario 9: assinatura sem carimbo de tempo e invalida.
    public void Invariante_8_assinatura_sem_carimbo_e_rejeitada()
    {
        var documento = DocumentoJuntado();

        var acao = () => documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaSimples, null!);

        acao.Should().Throw<ArgumentNullException>();
    }

    [Fact] // I-9 + B-5: TornarSemEfeito sobre Rascunho falha (so Juntado/Assinado).
    public void Invariante_9_tornar_sem_efeito_sobre_rascunho_falha()
    {
        var documento = NovoDocumento();

        var acao = () => documento.TornarSemEfeito("Cancelado");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9: TornarSemEfeito sem motivo e rejeitado.
    public void Invariante_9_tornar_sem_efeito_sem_motivo_e_rejeitado()
    {
        var documento = DocumentoJuntado();

        var acao = () => documento.TornarSemEfeito("   ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-10 + B-4: estado terminal SemEfeito nao admite novas transicoes.
    public void Invariante_10_sem_efeito_nao_admite_transicoes()
    {
        var documento = DocumentoJuntado();
        documento.TornarSemEfeito("Substituido");

        documento.Situacao.Should().Be(SituacaoDocumento.SemEfeito);
        ((Action)(() => documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaSimples, NovoCarimbo())))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => documento.TornarSemEfeito("De novo")))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-12 + Cenario 11: verificacao de integridade compara o hash armazenado.
    public void Invariante_12_verificacao_de_integridade()
    {
        var documento = DocumentoJuntado();

        documento.VerificarIntegridade(HashValido).Should().BeTrue();
        documento.VerificarIntegridade(HashOutro).Should().BeFalse();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 6: Juntado --Assinar(Qualificada)--> Assinado, emite DocumentoAssinado (criticidade Alta).
    public void Transicao_assinar_de_Juntado_para_Assinado()
    {
        var documento = DocumentoJuntado(CriticidadeAto.Alta);
        var signatario = Guid.NewGuid();

        documento.Assinar(signatario, TipoAssinatura.AssinaturaQualificada, NovoCarimbo());

        documento.Situacao.Should().Be(SituacaoDocumento.Assinado);
        documento.SignatarioId.Should().Be(signatario);
        documento.TipoAssinatura.Should().Be(TipoAssinatura.AssinaturaQualificada);
        documento.CarimboTempo.Should().NotBeNull();
        documento.DomainEvents.OfType<DocumentoAssinado>().Should().ContainSingle();
    }

    [Fact] // Cenario 7: criticidade Media aceita assinatura avancada.
    public void Transicao_assinar_criticidade_media_com_avancada()
    {
        var documento = DocumentoJuntado(CriticidadeAto.Media);

        documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaAvancada, NovoCarimbo());

        documento.Situacao.Should().Be(SituacaoDocumento.Assinado);
    }

    [Fact] // B-9 + Cenario 6: re-assinatura/coassinatura a partir de Assinado e permitida.
    public void Transicao_coassinatura_a_partir_de_Assinado()
    {
        var documento = DocumentoJuntado(CriticidadeAto.Baixa);
        documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaSimples, NovoCarimbo());

        documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaSimples, NovoCarimbo());

        documento.Situacao.Should().Be(SituacaoDocumento.Assinado);
        documento.DomainEvents.OfType<DocumentoAssinado>().Should().HaveCount(2);
    }

    [Fact] // Cenario 4: Juntado --TornarSemEfeito--> SemEfeito (preserva o registro).
    public void Transicao_tornar_sem_efeito_de_Juntado()
    {
        var documento = DocumentoJuntado();

        documento.TornarSemEfeito("Erro material");

        documento.Situacao.Should().Be(SituacaoDocumento.SemEfeito);
        documento.MotivoSemEfeito.Should().Be("Erro material");
    }

    [Fact] // Cenario 8: assinar documento SemEfeito falha (I-7).
    public void Cenario_8_assinar_documento_sem_efeito_falha()
    {
        var documento = DocumentoJuntado();
        documento.TornarSemEfeito("Sem efeito");

        var acao = () => documento.Assinar(Guid.NewGuid(), TipoAssinatura.AssinaturaSimples, NovoCarimbo());

        acao.Should().Throw<InvalidOperationException>();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 4 (persistencia): tornar sem efeito persiste o registro (nao excluido) com auditoria.
    public async Task Cenario_4_tornar_sem_efeito_preserva_registro_com_auditoria()
    {
        DocumentoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var documento = DocumentoJuntado();
            id = documento.Id;
            contexto.Documentos.Add(documento);
            await contexto.SaveChangesAsync();

            documento.TornarSemEfeito("Substituido por versao correta");
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var documento = await contexto.Documentos.SingleAsync(d => d.Id == id);
            documento.Situacao.Should().Be(SituacaoDocumento.SemEfeito);
            documento.TenantId.Should().Be(TenantA);

            (await contexto.Documentos.CountAsync()).Should().Be(1, "o registro permanece na trilha (nao excluido)");
            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do documento");
        }
    }

    [Fact] // Cenario 11 (persistencia): verificacao de integridade por hash recalculado divergente.
    public async Task Cenario_11_verificacao_de_integridade_persistida()
    {
        DocumentoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var documento = DocumentoJuntado();
            id = documento.Id;
            contexto.Documentos.Add(documento);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var documento = await contexto.Documentos.SingleAsync(d => d.Id == id);
            documento.VerificarIntegridade(HashValido).Should().BeTrue();
            documento.VerificarIntegridade(HashOutro).Should().BeFalse();
        }
    }

    [Fact] // Cenario 12: consulta de documentos e tenant-scoped (Global Query Filter).
    public async Task Cenario_12_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Documentos.Add(DocumentoJuntado());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Documentos.ToListAsync()).Should().BeEmpty();
        }
    }
}
