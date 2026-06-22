using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Fornecedor"/>: invariantes, transicoes de
/// situacao cadastral (sancao impeditiva -> Sancionado; reabilitacao -> Ativo) e os cenarios BDD
/// de Fornecedor.rules.md, sobre SQLite em memoria com auditoria e isolamento por tenant.
/// </summary>
public sealed class FornecedorFluxoTests : AdministracaoTestBase
{
    private const string CnpjValido = "11.222.333/0001-81";
    private static readonly DateOnly Hoje = new(2026, 6, 20);

    private static Fornecedor NovoFornecedor()
        => Fornecedor.Cadastrar(TenantA, Cnpj.Create(CnpjValido), "Fornecedora X Ltda");

    // ---------- Invariantes ----------

    [Fact] // I-5 + Cenario 1: cadastro nasce Ativo/NaoCadastrado e emite FornecedorCadastrado.
    public void Invariante_5_cadastro_nasce_Ativo_e_emite_evento()
    {
        var forn = NovoFornecedor();

        forn.Situacao.Should().Be(SituacaoFornecedor.Ativo);
        forn.NivelCadastralSICAF.Should().Be(NivelCadastralSICAF.NaoCadastrado);
        forn.DomainEvents.OfType<FornecedorCadastrado>().Should().ContainSingle();
    }

    [Fact] // I-1 + Cenario 2: CNPJ invalido e rejeitado no VO.
    public void Invariante_1_cnpj_invalido_e_rejeitado()
    {
        ((Action)(() => Cnpj.Create("11.222.333/0001-99"))).Should().Throw<ArgumentException>();
    }

    [Fact] // I-3: razao social vazia e rejeitada.
    public void Invariante_3_razao_social_vazia_e_rejeitada()
    {
        var acao = () => Fornecedor.Cadastrar(TenantA, Cnpj.Create(CnpjValido), "  ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-10: sancao exige processo administrativo e fundamentacao.
    public void Invariante_10_sancao_exige_processo_e_fundamentacao()
    {
        var forn = NovoFornecedor();

        ((Action)(() => forn.AplicarSancao(
            TipoSancao.Advertencia, Hoje, null, "  ", "Fundamento", null, Hoje)))
            .Should().Throw<ArgumentException>();

        ((Action)(() => forn.AplicarSancao(
            TipoSancao.Advertencia, Hoje, null, "PA-1", "  ", null, Hoje)))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // I-9 + Cenario 7: multa exige valor positivo.
    public void Invariante_9_multa_exige_valor()
    {
        var forn = NovoFornecedor();

        ((Action)(() => forn.AplicarSancao(
            TipoSancao.Multa, Hoje, null, "PA-1", "Fundamento", null, Hoje)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-9: multa com valor positivo e aceita e mantem Ativo (nao impeditiva).
    public void Invariante_9_multa_com_valor_e_aceita_e_mantem_Ativo()
    {
        var forn = NovoFornecedor();

        forn.AplicarSancao(TipoSancao.Multa, Hoje, null, "PA-1", "Fundamento", ValorMonetario.De(500m), Hoje);

        forn.Situacao.Should().Be(SituacaoFornecedor.Ativo);
        forn.EstaImpedido(Hoje).Should().BeFalse();
    }

    [Fact] // I-6 + Cenario 6: advertencia nao impede; mantem Ativo, mas emite evento.
    public void Invariante_6_advertencia_nao_impede()
    {
        var forn = NovoFornecedor();

        forn.AplicarSancao(TipoSancao.Advertencia, Hoje, null, "PA-1", "Atraso", null, Hoje);

        forn.Situacao.Should().Be(SituacaoFornecedor.Ativo);
        forn.EstaImpedido(Hoje).Should().BeFalse();
        forn.DomainEvents.OfType<FornecedorSancionado>().Should().ContainSingle();
    }

    [Fact] // I-12 + Cenario 10: vigencia da sancao pela data (DataInicio<=hoje<=DataFim).
    public void Invariante_12_vigencia_da_sancao_pela_data()
    {
        var forn = NovoFornecedor();
        var inicio = new DateOnly(2026, 1, 1);
        var fim = new DateOnly(2026, 12, 31);
        forn.AplicarSancao(TipoSancao.Impedimento, inicio, fim, "PA-1", "Fraude", null, Hoje);

        forn.EstaImpedido(new DateOnly(2025, 12, 31)).Should().BeFalse(); // antes do inicio.
        forn.EstaImpedido(inicio).Should().BeTrue();
        forn.EstaImpedido(fim).Should().BeTrue(); // DataFim inclusivo.
        forn.EstaImpedido(new DateOnly(2027, 1, 1)).Should().BeFalse(); // apos o fim.
    }

    [Fact] // I-15: atualizar nivel SICAF invalido e rejeitado.
    public void Invariante_15_nivel_sicaf_invalido_e_rejeitado()
    {
        var forn = NovoFornecedor();

        ((Action)(() => forn.AtualizarNivelSicaf((NivelCadastralSICAF)99)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-15: atualizar nivel SICAF valido nao altera situacao.
    public void Invariante_15_nivel_sicaf_valido_nao_altera_situacao()
    {
        var forn = NovoFornecedor();

        forn.AtualizarNivelSicaf(NivelCadastralSICAF.RegularidadeFiscalNivel3);

        forn.NivelCadastralSICAF.Should().Be(NivelCadastralSICAF.RegularidadeFiscalNivel3);
        forn.Situacao.Should().Be(SituacaoFornecedor.Ativo);
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // I-6 + Cenario 4: Ativo --inidoneidade vigente--> Sancionado, EstaImpedido == true.
    public void Transicao_inidoneidade_torna_Sancionado()
    {
        var forn = NovoFornecedor();

        forn.AplicarSancao(
            TipoSancao.Inidoneidade, Hoje, Hoje.AddYears(5), "PA-2026-1", "Fraude grave", null, Hoje);

        forn.Situacao.Should().Be(SituacaoFornecedor.Sancionado);
        forn.EstaImpedido(Hoje).Should().BeTrue();
        forn.DomainEvents.OfType<FornecedorSancionado>().Should().ContainSingle()
            .Which.Tipo.Should().Be(TipoSancao.Inidoneidade);
    }

    [Fact] // Cenario 5: fornecedor impedido nega habilitacao (regra EstaImpedido).
    public void Cenario_5_fornecedor_impedido_e_negado()
    {
        var forn = NovoFornecedor();
        forn.AplicarSancao(TipoSancao.Impedimento, Hoje, Hoje.AddYears(2), "PA-1", "Inexecucao", null, Hoje);

        forn.EstaImpedido(Hoje).Should().BeTrue();
    }

    [Fact] // I-11 + Cenario 8: reabilitacao apos cumprimento (sancao expirada) -> Ativo.
    public void Transicao_reabilitar_apos_cumprimento_volta_Ativo()
    {
        var forn = NovoFornecedor();
        var inicio = new DateOnly(2024, 1, 1);
        var fim = new DateOnly(2025, 1, 1); // ja expirada relativo a Hoje (2026).
        forn.AplicarSancao(TipoSancao.Impedimento, inicio, fim, "PA-1", "Inexecucao", null, inicio);
        forn.Situacao.Should().Be(SituacaoFornecedor.Sancionado);

        forn.Reabilitar(Hoje);

        forn.Situacao.Should().Be(SituacaoFornecedor.Ativo);
        forn.EstaImpedido(Hoje).Should().BeFalse();
        forn.DomainEvents.OfType<FornecedorReabilitado>().Should().ContainSingle();
    }

    [Fact] // I-11 + Cenario 9: reabilitacao bloqueada com sancao vigente.
    public void Invariante_11_reabilitacao_bloqueada_com_sancao_vigente()
    {
        var forn = NovoFornecedor();
        forn.AplicarSancao(TipoSancao.Inidoneidade, Hoje, Hoje.AddYears(5), "PA-1", "Fraude", null, Hoje);

        ((Action)(() => forn.Reabilitar(Hoje))).Should().Throw<InvalidOperationException>();
        forn.Situacao.Should().Be(SituacaoFornecedor.Sancionado);
    }

    [Fact] // I-11: reabilitar fornecedor nao Sancionado falha.
    public void Invariante_11_reabilitar_nao_sancionado_falha()
    {
        var forn = NovoFornecedor();

        ((Action)(() => forn.Reabilitar(Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // Ativo --Inativar--> Inativo, emite FornecedorInativado.
    public void Transicao_inativar_de_Ativo_para_Inativo()
    {
        var forn = NovoFornecedor();

        forn.Inativar();

        forn.Situacao.Should().Be(SituacaoFornecedor.Inativo);
        forn.DomainEvents.OfType<FornecedorInativado>().Should().ContainSingle();
        ((Action)forn.Inativar).Should().Throw<InvalidOperationException>(); // ja Inativo.
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Fluxo sancao impeditiva persiste e registra auditoria no mesmo commit.
    public async Task Fluxo_de_sancao_persiste_com_auditoria()
    {
        FornecedorId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var forn = NovoFornecedor();
            id = forn.Id;
            contexto.Fornecedores.Add(forn);
            await contexto.SaveChangesAsync();

            forn.AplicarSancao(TipoSancao.Inidoneidade, Hoje, Hoje.AddYears(5), "PA-1", "Fraude", null, Hoje);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var forn = await contexto.Fornecedores.SingleAsync(f => f.Id == id);
            forn.Situacao.Should().Be(SituacaoFornecedor.Sancionado);
            forn.TenantId.Should().Be(TenantA);
            forn.Sancoes.Should().ContainSingle();

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do fornecedor");
        }
    }

    [Fact] // Cenario 11: consulta e tenant-scoped (Global Query Filter).
    public async Task Cenario_11_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Fornecedores.Add(NovoFornecedor());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Fornecedores.ToListAsync()).Should().BeEmpty();
        }
    }
}
