using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Paciente"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Paciente.rules.md, sobre SQLite em memoria com
/// auditoria e isolamento por tenant.
/// </summary>
public sealed class PacienteFluxoTests : SaudeTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static Identificacao NovaIdentificacao()
        => new("Maria da Silva", new DateOnly(1990, 5, 10), Sexo.Feminino, "Maria S.", Cpf.Create("52998224725"), Hoje);

    private static Endereco NovoEndereco()
        => new("Rua das Flores", "100", "Centro", "Maximiliano de Almeida", "RS", "99970000");

    private static Paciente NovoPaciente(string cns = CnsValido)
        => Paciente.Cadastrar(TenantA, new Cns(cns), NovaIdentificacao(), NovoEndereco());

    // ---------- Invariantes ----------

    [Fact] // I-4 + Cenario 1: cadastro nasce Ativo, CnsConfirmado falso, emite PacienteCadastrado.
    public void Invariante_4_cadastro_nasce_ativo_e_emite_evento()
    {
        var paciente = NovoPaciente();

        paciente.Situacao.Should().Be(SituacaoPaciente.Ativo);
        paciente.CnsConfirmado.Should().BeFalse();
        paciente.DomainEvents.OfType<PacienteCadastrado>().Should().ContainSingle();
    }

    [Fact] // I-1 + B-1: CNS vazio e rejeitado.
    public void Invariante_1_cns_vazio_e_rejeitado()
    {
        var acao = () => new Cns("   ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-1 + B-2: CNS com formato/DV invalido e rejeitado.
    public void Invariante_1_cns_invalido_e_rejeitado()
    {
        var acao = () => new Cns("123456789012345");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-3 + B-4: data de nascimento futura e rejeitada na Identificacao.
    public void Invariante_3_data_nascimento_futura_e_rejeitada()
    {
        var acao = () => new Identificacao("Joao", Hoje.AddDays(1), Sexo.Masculino, null, null, Hoje);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-3: nome vazio e rejeitado.
    public void Invariante_3_nome_vazio_e_rejeitado()
    {
        var acao = () => new Identificacao("  ", new DateOnly(1990, 1, 1), Sexo.Masculino, null, null, Hoje);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-10 + B-6: ConfirmarCadastro e idempotente (false->true; reconfirmar nao regride/erra).
    public void Invariante_10_confirmar_cadastro_e_idempotente()
    {
        var paciente = NovoPaciente();

        paciente.ConfirmarCadastro();
        paciente.CnsConfirmado.Should().BeTrue();
        paciente.DomainEvents.OfType<CadastroConfirmadoNoCadsus>().Should().ContainSingle();

        paciente.ConfirmarCadastro(); // segunda confirmacao: nao lanca, nao emite de novo.
        paciente.CnsConfirmado.Should().BeTrue();
        paciente.DomainEvents.OfType<CadastroConfirmadoNoCadsus>().Should().ContainSingle();
    }

    [Fact] // I-7 + B-7: registrar condicao com codigo vazio e rejeitado.
    public void Invariante_7_condicao_codigo_vazio_e_rejeitado()
    {
        var paciente = NovoPaciente();

        var acao = () => paciente.RegistrarCondicaoDeSaude("  ", "Diabetes", Hoje);

        acao.Should().Throw<ArgumentException>();
        paciente.Condicoes.Should().BeEmpty();
    }

    [Fact] // I-8 + B-8: registrar alergia com substancia vazia e rejeitado.
    public void Invariante_8_alergia_substancia_vazia_e_rejeitada()
    {
        var paciente = NovoPaciente();

        var acao = () => paciente.RegistrarAlergia("  ", "Grave", Hoje);

        acao.Should().Throw<ArgumentException>();
        paciente.Alergias.Should().BeEmpty();
    }

    [Fact] // I-6/I-11 + B-9: inativar paciente ja inativo e rejeitado.
    public void Invariante_11_inativar_ja_inativo_e_rejeitado()
    {
        var paciente = NovoPaciente();
        paciente.Inativar("Obito");

        var acao = () => paciente.Inativar("De novo");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-1 (factory): motivo de inativacao vazio e rejeitado.
    public void Invariante_inativar_motivo_vazio_e_rejeitado()
    {
        var paciente = NovoPaciente();

        var acao = () => paciente.Inativar("   ");

        acao.Should().Throw<ArgumentException>();
        paciente.Situacao.Should().Be(SituacaoPaciente.Ativo);
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 5: Ativo --RegistrarCondicaoDeSaude--> Ativo, emite CondicaoDeSaudeRegistrada.
    public void Transicao_registrar_condicao_mantem_ativo_e_emite_evento()
    {
        var paciente = NovoPaciente();

        paciente.RegistrarCondicaoDeSaude("E11", "Diabetes tipo 2", Hoje);

        paciente.Situacao.Should().Be(SituacaoPaciente.Ativo);
        paciente.Condicoes.Should().ContainSingle().Which.Codigo.Should().Be("E11");
        paciente.DomainEvents.OfType<CondicaoDeSaudeRegistrada>().Should().ContainSingle();
    }

    [Fact] // Cenario 6: Ativo --RegistrarAlergia--> Ativo, emite AlergiaRegistrada.
    public void Transicao_registrar_alergia_mantem_ativo_e_emite_evento()
    {
        var paciente = NovoPaciente();

        paciente.RegistrarAlergia("Penicilina", "Grave", Hoje);

        paciente.Situacao.Should().Be(SituacaoPaciente.Ativo);
        paciente.Alergias.Should().ContainSingle().Which.Substancia.Should().Be("Penicilina");
        paciente.DomainEvents.OfType<AlergiaRegistrada>().Should().ContainSingle();
    }

    [Fact] // Cenario 7: Ativo --AtualizarCadastro--> Ativo, emite CadastroAtualizado.
    public void Transicao_atualizar_cadastro_mantem_ativo_e_emite_evento()
    {
        var paciente = NovoPaciente();
        var novoEndereco = new Endereco("Av. Brasil", "200", "Centro", "Porto Alegre", "RS", "90000000");

        paciente.AtualizarCadastro(NovaIdentificacao(), novoEndereco);

        paciente.Situacao.Should().Be(SituacaoPaciente.Ativo);
        paciente.Endereco.Municipio.Should().Be("Porto Alegre");
        paciente.DomainEvents.OfType<CadastroAtualizado>().Should().ContainSingle();
    }

    [Fact] // Cenario 9: Ativo --Inativar--> Inativo, emite PacienteInativado.
    public void Transicao_inativar_de_ativo_para_inativo()
    {
        var paciente = NovoPaciente();

        paciente.Inativar("Obito");

        paciente.Situacao.Should().Be(SituacaoPaciente.Inativo);
        paciente.DomainEvents.OfType<PacienteInativado>().Should().ContainSingle()
            .Which.Motivo.Should().Be("Obito");
    }

    [Fact] // Cenario 8 + I-6 + B-10: paciente inativo nao admite alteracoes.
    public void Cenario_8_paciente_inativo_nao_admite_alteracoes()
    {
        var paciente = NovoPaciente();
        paciente.Inativar("Transferencia");

        ((Action)(() => paciente.AtualizarCadastro(NovaIdentificacao(), NovoEndereco())))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => paciente.RegistrarCondicaoDeSaude("E11", "Diabetes", Hoje)))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => paciente.RegistrarAlergia("Penicilina", "Grave", Hoje)))
            .Should().Throw<InvalidOperationException>();
        ((Action)paciente.ConfirmarCadastro).Should().Throw<InvalidOperationException>();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 1: cadastro confirmado persiste com auditoria.
    public async Task Cenario_1_cadastro_confirmado_persiste_com_auditoria()
    {
        PacienteId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var paciente = NovoPaciente();
            paciente.ConfirmarCadastro();
            paciente.RegistrarCondicaoDeSaude("E11", "Diabetes tipo 2", Hoje);
            id = paciente.Id;
            contexto.Pacientes.Add(paciente);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var paciente = await contexto.Pacientes
                .Include(p => p.Condicoes)
                .SingleAsync(p => p.Id == id);
            paciente.CnsConfirmado.Should().BeTrue();
            paciente.TenantId.Should().Be(TenantA);
            paciente.Condicoes.Should().ContainSingle();

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do paciente");
        }
    }

    [Fact] // Cenario 4: mesmo CNS em tenants distintos sao cadastros validos e isolados.
    public async Task Cenario_4_mesmo_cns_em_tenants_distintos_e_valido()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Pacientes.Add(NovoPaciente());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            var pacienteB = Paciente.Cadastrar(TenantB, new Cns(CnsValido), NovaIdentificacao(), NovoEndereco());
            contexto.Pacientes.Add(pacienteB);
            await contexto.SaveChangesAsync();

            (await contexto.Pacientes.ToListAsync()).Should().ContainSingle("tenant B enxerga apenas o seu cadastro");
        }
    }

    [Fact] // Cenario 10: consulta por CNS e tenant-scoped (Global Query Filter).
    public async Task Cenario_10_consulta_por_cns_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Pacientes.Add(NovoPaciente());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            var encontrado = await contexto.Pacientes
                .SingleOrDefaultAsync(p => p.Cns == new Cns(CnsValido));
            encontrado.Should().BeNull("o paciente pertence ao tenant A");
        }
    }
}
