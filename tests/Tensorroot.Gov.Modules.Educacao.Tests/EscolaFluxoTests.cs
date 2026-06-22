using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Escola"/>: invariantes, cada transicao da maquina
/// de estados e os cenarios BDD de Escola.rules.md, sobre SQLite em memoria com auditoria, Outbox e
/// isolamento por tenant. Usa o <see cref="Infrastructure.Persistence.EducacaoDbContext"/> gerado.
/// </summary>
public sealed class EscolaFluxoTests : EducacaoTestBase
{
    private static Endereco NovoEndereco()
        => Endereco.Criar("Rua Central, 100", "Maximiliano de Almeida", "RS", "99840000", -27.6, -51.8);

    private static Infraestrutura NovaInfraestrutura()
        => Infraestrutura.Criar(numeroSalas: 12, numeroDependencias: 5, possuiAcessibilidade: true);

    private static Escola NovaEscola(string codigoInep = "43000001", string nome = "EMEF Central")
        => Escola.Credenciar(
            TenantA,
            CodigoInep.Criar(codigoInep),
            nome,
            DependenciaAdministrativa.Municipal,
            NovoEndereco(),
            NovaInfraestrutura());

    // ---------- Invariantes ----------

    [Fact] // I-4 + Cenario 1: credenciamento nasce Credenciada e emite EscolaCredenciada.
    public void Invariante_4_credenciamento_nasce_Credenciada_e_emite_evento()
    {
        var escola = NovaEscola();

        escola.Situacao.Should().Be(SituacaoEscola.Credenciada);
        escola.Ativa.Should().BeTrue();
        escola.DomainEvents.OfType<EscolaCredenciada>().Should().ContainSingle()
            .Which.CodigoInep.Valor.Should().Be("43000001");
    }

    [Fact] // I-1 + B-1: CodigoInep vazio e rejeitado no credenciamento.
    public void Invariante_1_codigo_inep_vazio_e_rejeitado()
    {
        var acao = () => Escola.Credenciar(
            TenantA, new CodigoInep("  "), "EMEF Central",
            DependenciaAdministrativa.Municipal, NovoEndereco(), NovaInfraestrutura());

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-2 + B-2: Nome vazio e rejeitado no credenciamento.
    public void Invariante_2_nome_vazio_e_rejeitado()
    {
        var acao = () => Escola.Credenciar(
            TenantA, CodigoInep.Criar("43000002"), "   ",
            DependenciaAdministrativa.Municipal, NovoEndereco(), NovaInfraestrutura());

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-3 + B-3: Dependencia fora do enum e rejeitada.
    public void Invariante_3_dependencia_invalida_e_rejeitada()
    {
        var acao = () => Escola.Credenciar(
            TenantA, CodigoInep.Criar("43000003"), "EMEF Central",
            (DependenciaAdministrativa)99, NovoEndereco(), NovaInfraestrutura());

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-5: atualizar dados do Censo emite DadosCensoAtualizados e mantem a situacao.
    public void Invariante_5_atualizar_dados_censo_emite_evento_e_mantem_situacao()
    {
        var escola = NovaEscola();
        var novoEndereco = Endereco.Criar("Av. Nova, 200", "Maximiliano de Almeida", "RS", "99840000", -27.7, -51.9);
        var novaInfra = Infraestrutura.Criar(15, 7, true);

        escola.AtualizarDadosCenso(novoEndereco, novaInfra);

        escola.Situacao.Should().Be(SituacaoEscola.Credenciada);
        escola.Endereco.Should().Be(novoEndereco);
        escola.Infraestrutura.Should().Be(novaInfra);
        escola.DomainEvents.OfType<DadosCensoAtualizados>().Should().ContainSingle();
    }

    [Fact] // I-5/I-8 + Cenario 4: escola desativada nao admite atualizacao de dados do Censo.
    public void Invariante_5_atualizar_dados_censo_em_escola_desativada_e_rejeitado()
    {
        var escola = NovaEscola();
        escola.Desativar();

        var acao = () => escola.AtualizarDadosCenso(NovoEndereco(), NovaInfraestrutura());

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + B-6: estado Desativada e terminal; desativar de novo falha.
    public void Invariante_8_desativada_e_terminal_nao_admite_nova_transicao()
    {
        var escola = NovaEscola();
        escola.Desativar();

        escola.Encerrada.Should().BeTrue();
        ((Action)escola.Desativar).Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // (nenhum) --Credenciar--> Credenciada.
    public void Transicao_credenciar_para_Credenciada()
    {
        var escola = NovaEscola();

        escola.Situacao.Should().Be(SituacaoEscola.Credenciada);
        escola.CodigoInep.Valor.Should().Be("43000001");
    }

    [Fact] // Credenciada --AtualizarDadosCenso--> Credenciada.
    public void Transicao_atualizar_dados_censo_mantem_Credenciada()
    {
        var escola = NovaEscola();

        escola.AtualizarDadosCenso(NovoEndereco(), NovaInfraestrutura());

        escola.Situacao.Should().Be(SituacaoEscola.Credenciada);
    }

    [Fact] // Credenciada --Desativar--> Desativada.
    public void Transicao_desativar_de_Credenciada_para_Desativada()
    {
        var escola = NovaEscola();

        escola.Desativar();

        escola.Situacao.Should().Be(SituacaoEscola.Desativada);
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 1/3: credenciamento + atualizacao persistem com trilha de auditoria.
    public async Task Cenario_3_fluxo_persiste_com_auditoria()
    {
        EscolaId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var escola = NovaEscola();
            id = escola.Id;
            contexto.Escolas.Add(escola);
            await contexto.SaveChangesAsync();

            escola.AtualizarDadosCenso(
                Endereco.Criar("Av. Nova, 200", "Maximiliano de Almeida", "RS", "99840000", -27.7, -51.9),
                Infraestrutura.Criar(15, 7, true));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var escola = await contexto.Escolas.SingleAsync(e => e.Id == id);
            escola.TenantId.Should().Be(TenantA);
            escola.Infraestrutura.NumeroSalas.Should().Be(15);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da escola");
        }
    }

    [Fact] // I-1 + B-4 + Cenario 2: codigo INEP unico por rede (indice unico (TenantId, CodigoInep)).
    public async Task Cenario_2_codigo_inep_duplicado_na_rede_falha_na_persistencia()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.Escolas.Add(NovaEscola(codigoInep: "43000099"));
        await contexto.SaveChangesAsync();

        contexto.Escolas.Add(NovaEscola(codigoInep: "43000099", nome: "EMEF Duplicada"));
        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // I-7 + B-7 + Cenario 6: listagem e tenant-scoped (Global Query Filter).
    public async Task Cenario_6_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Escolas.Add(NovaEscola(codigoInep: "43000010"));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Escolas.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact] // I-7: mesmo codigo INEP em tenants distintos coexiste (escopo por rede).
    public async Task Isolamento_mesmo_codigo_inep_em_tenants_distintos_coexiste()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Escolas.Add(NovaEscola(codigoInep: "43000050"));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            contexto.Escolas.Add(Escola.Credenciar(
                TenantB, CodigoInep.Criar("43000050"), "EMEF Rede B",
                DependenciaAdministrativa.Municipal, NovoEndereco(), NovaInfraestrutura()));
            await contexto.SaveChangesAsync();

            (await contexto.Escolas.CountAsync()).Should().Be(1);
        }
    }
}
