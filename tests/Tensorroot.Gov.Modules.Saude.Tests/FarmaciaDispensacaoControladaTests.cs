using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Farmacia;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;
using AtendimentoEndereco = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.Endereco;
using SaudeEstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using SaudeProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// SA-1 (W10.6): a dispensacao de medicamento sob CONTROLE ESPECIAL (Portaria SVS/MS 344/1998 — base
/// do SNGPC) e FAIL-CLOSED: exige uma prescricao de origem que EXISTA no tenant e PERTENCA ao mesmo
/// paciente (retencao de receita com rastro). Sem isso, a dispensacao e barrada e o estoque permanece
/// intacto. Medicamento comum (sem controle) dispensa normalmente. Cobre o
/// <see cref="DispensarMedicamentoHandler"/> de ponta a ponta sobre SQLite em memoria.
/// </summary>
public sealed class FarmaciaDispensacaoControladaTests : SaudeTestBase
{
    private static readonly DateTimeOffset DataHora = new(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);

    [Fact] // SA-1: controlado SEM prescricao valida do paciente => RECUSADO (fail-closed), estoque intacto.
    public async Task Dispensar_controlado_sem_prescricao_e_recusado_e_nao_baixa_estoque()
    {
        var cenario = await SemearCenarioAsync(controlado: true, comPrescricaoDoPaciente: false);

        var acao = async () => await cenario.Handler.Handle(cenario.Command, CancellationToken.None);

        await acao.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*controle especial*");

        await using var verificacao = CriarContexto(TenantA);
        var estoque = await new EstoqueMedicamentoRepository(verificacao)
            .ObterPorEstabelecimentoEMedicamentoAsync(cenario.EstabelecimentoId, cenario.MedicamentoId, CancellationToken.None);
        estoque!.Saldo.Should().Be(100m, "a dispensacao recusada nao pode baixar o estoque do controlado");
    }

    [Fact] // SA-1 (contraprova): controlado COM prescricao valida do paciente => dispensa e baixa o estoque.
    public async Task Dispensar_controlado_com_prescricao_valida_do_paciente_e_aceito()
    {
        var cenario = await SemearCenarioAsync(controlado: true, comPrescricaoDoPaciente: true);

        var id = await cenario.Handler.Handle(cenario.Command, CancellationToken.None);
        id.Should().NotBeEmpty();

        await using var verificacao = CriarContexto(TenantA);
        var estoque = await new EstoqueMedicamentoRepository(verificacao)
            .ObterPorEstabelecimentoEMedicamentoAsync(cenario.EstabelecimentoId, cenario.MedicamentoId, CancellationToken.None);
        estoque!.Saldo.Should().Be(98m, "a dispensacao legitima do controlado baixa as 2 unidades");
    }

    [Fact] // SA-1 (contraprova): medicamento comum (sem controle) dispensa sem exigir prescricao.
    public async Task Dispensar_medicamento_comum_sem_prescricao_e_aceito()
    {
        var cenario = await SemearCenarioAsync(controlado: false, comPrescricaoDoPaciente: false);

        var id = await cenario.Handler.Handle(cenario.Command, CancellationToken.None);
        id.Should().NotBeEmpty();

        await using var verificacao = CriarContexto(TenantA);
        var estoque = await new EstoqueMedicamentoRepository(verificacao)
            .ObterPorEstabelecimentoEMedicamentoAsync(cenario.EstabelecimentoId, cenario.MedicamentoId, CancellationToken.None);
        estoque!.Saldo.Should().Be(98m);
    }

    private async Task<Cenario> SemearCenarioAsync(bool controlado, bool comPrescricaoDoPaciente)
    {
        Guid pacienteId;
        SaudeEstabelecimentoId estabelecimentoId;
        MedicamentoId medicamentoId;
        Guid? prescricaoId = null;

        await using (var seed = CriarContexto(TenantA))
        {
            var paciente = Paciente.Cadastrar(
                TenantA, new Cns(CnsValido), NovaIdentificacao(), NovoEndereco());
            paciente.ConfirmarCadastro();
            pacienteId = paciente.Id.Value;
            seed.Pacientes.Add(paciente);

            var estabelecimento = Estabelecimento.Cadastrar(
                TenantA, new CodigoCnes("1234567"), "UBS Central", TipoEstabelecimento.Ubs, NovoEndereco());
            estabelecimentoId = estabelecimento.Id;
            seed.Estabelecimentos.Add(estabelecimento);

            var controle = controlado ? TipoControleSngpc.EntorpecentePsicotropicoA : TipoControleSngpc.SemControle;
            var medicamento = Medicamento.Cadastrar(
                TenantA, "Clonazepam", "comprimido 2 mg, c/ 30", "2 mg",
                FormaFarmaceutica.Comprimido, UnidadeMedidaMedicamento.Unidade, controle);
            medicamentoId = medicamento.Id;
            seed.Medicamentos.Add(medicamento);

            var estoque = EstoqueMedicamento.Abrir(TenantA, estabelecimentoId, medicamentoId, pontoDeRessuprimento: 10);
            estoque.RegistrarEntrada("LOTE-1", new DateOnly(2027, 12, 31), 100m, DateOnly.FromDateTime(DataHora.UtcDateTime));
            seed.EstoquesMedicamento.Add(estoque);

            if (comPrescricaoDoPaciente)
            {
                var atendimento = Atendimento.Registrar(
                    TenantA,
                    new Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId(pacienteId),
                    estabelecimentoId, new SaudeProfissionalId(Guid.NewGuid()),
                    DataHora, Competencia.De(DataHora), ModalidadeAtendimento.Presencial);
                atendimento.AdicionarPrescricao("Clonazepam 2 mg", "1 cp a noite", DataHora);
                seed.Atendimentos.Add(atendimento);
                await seed.SaveChangesAsync();
                prescricaoId = atendimento.Prescricoes.Single().Id.Value;
            }
            else
            {
                await seed.SaveChangesAsync();
            }
        }

        var contexto = CriarContexto(TenantA);
        var handler = new DispensarMedicamentoHandler(
            new PacienteRepository(contexto),
            new EstabelecimentoRepository(contexto),
            new MedicamentoRepository(contexto),
            new EstoqueMedicamentoRepository(contexto),
            new DispensacaoRepository(contexto),
            new AtendimentoRepository(contexto),
            new ContextoUnitOfWork(contexto),
            new TenantContextFake(TenantA),
            TimeProvider.System);

        var command = new DispensarMedicamentoCommand(
            pacienteId,
            estabelecimentoId.Value,
            Guid.NewGuid(),
            prescricaoId,
            new[] { new ItemDispensacaoInput(medicamentoId.Value, 2m, "1 cp a noite") });

        return new Cenario(contexto, handler, command, estabelecimentoId, medicamentoId);
    }

    private static Identificacao NovaIdentificacao()
        => new("Maria da Silva", new DateOnly(1990, 5, 10), Sexo.Feminino, "Maria S.", Cpf.Create("52998224725"), new DateOnly(2026, 6, 21));

    private static AtendimentoEndereco NovoEndereco()
        => new("Rua das Flores", "100", "Centro", "Maximiliano de Almeida", "RS", "99970000");

    private sealed record Cenario(
        SaudeDbContext Contexto,
        DispensarMedicamentoHandler Handler,
        DispensarMedicamentoCommand Command,
        SaudeEstabelecimentoId EstabelecimentoId,
        MedicamentoId MedicamentoId);

    private sealed class ContextoUnitOfWork(SaudeDbContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }
}
