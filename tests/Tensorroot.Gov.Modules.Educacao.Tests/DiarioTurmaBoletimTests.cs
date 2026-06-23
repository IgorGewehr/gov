using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>
/// Cobertura do diario por turma + boletim/historico (ONDA3-DESIGN §1.1, sub-onda 3a): lancamento de
/// frequencia e nota em LOTE por turma (so matriculas Ativas e com diario aberto), invariantes (nao
/// lancar em diario fechado/sem matricula) e a projecao de leitura (boletim e diario coletivo refletem
/// os lancamentos). Orquestracao + read model sobre os agregados existentes (Turma, Matricula, Aluno,
/// DiarioClasse 1-1). Roda sobre SQLite em memoria com o Global Query Filter por tenant ativo; o
/// <see cref="EducacaoDbContext"/> atua como Unit of Work dos handlers.
/// </summary>
public sealed class DiarioTurmaBoletimTests : EducacaoTestBase
{
    private static readonly DateOnly DataReferencia = new(2026, 3, 31);
    private static readonly DateOnly DiaAula = new(2026, 4, 10);
    private const int CargaHorariaTotal = 800;

    private static DadosCivis Dados(string nome)
        => new(nome, new DateOnly(2014, 5, 10), Sexo.Masculino, "Mae do " + nome, null, null, DataReferencia);

    private static EnderecoAluno Endereco()
        => new("Rua das Flores", "100", "Centro", "Maximiliano de Almeida", "RS", "99880000");

    // Alunos do cenario sao menores (nascidos em 2014); I-A2 exige >= 1 responsavel no cadastro.
    private static IReadOnlyCollection<Responsavel> Responsaveis(string nomeAluno)
        => [Responsavel.Criar("Mae do " + nomeAluno, null, Parentesco.Mae, "5199990000", true, true)];

    /// <summary>Cenario: 1 turma com 2 alunos Ativos (cada um com diario aberto) + 1 matricula Encerrada.</summary>
    private sealed record Cenario(
        Guid TurmaId, Guid AlunoAtivoA, Guid MatriculaAtivaA, Guid MatriculaAtivaB, Guid MatriculaEncerrada);

    private static async Task<Cenario> SemearTurmaAsync(EducacaoDbContext contexto)
    {
        var escolaId = EscolaId.New();
        var turma = Turma.Criar(TenantA, escolaId, 2026, Etapa.Fundamental1, "5o ano", Turno.Matutino, 30);

        var alunoA = Aluno.Cadastrar(TenantA, Dados("Ana"), Endereco(), null, Responsaveis("Ana"), DataReferencia);
        var alunoB = Aluno.Cadastrar(TenantA, Dados("Bruno"), Endereco(), null, Responsaveis("Bruno"), DataReferencia);
        var alunoC = Aluno.Cadastrar(TenantA, Dados("Carlos"), Endereco(), null, Responsaveis("Carlos"), DataReferencia);

        var matA = Matricula.MatricularAluno(TenantA, alunoA.Id, turma.Id, escolaId, DataReferencia);
        var matB = Matricula.MatricularAluno(TenantA, alunoB.Id, turma.Id, escolaId, DataReferencia);
        var matC = Matricula.MatricularAluno(TenantA, alunoC.Id, turma.Id, escolaId, DataReferencia);
        matC.Concluir(); // encerrada — nao deve receber lancamento em lote.

        var diarioA = DiarioClasseAggregate.Abrir(TenantA, matA.Id, CargaHorariaTotal);
        var diarioB = DiarioClasseAggregate.Abrir(TenantA, matB.Id, CargaHorariaTotal);

        contexto.Turmas.Add(turma);
        contexto.Alunos.AddRange(alunoA, alunoB, alunoC);
        contexto.Matriculas.AddRange(matA, matB, matC);
        contexto.DiariosClasse.AddRange(diarioA, diarioB);
        await contexto.SaveChangesAsync();

        return new Cenario(turma.Id.Value, alunoA.Id.Value, matA.Id.Value, matB.Id.Value, matC.Id.Value);
    }

    [Fact] // Frequencia em lote so atinge matriculas Ativas com diario aberto; encerrada e ignorada.
    public async Task Frequencia_em_lote_atinge_so_matriculas_ativas()
    {
        await using var contexto = CriarContexto(TenantA);
        var cenario = await SemearTurmaAsync(contexto);

        var handler = new RegistrarFrequenciaTurmaHandler(
            new MatriculaRepository(contexto), new DiarioClasseRepository(contexto), contexto);

        var lancados = await handler.Handle(
            new RegistrarFrequenciaTurmaCommand(
                cenario.TurmaId,
                DiaAula,
                CargaHorariaAula: 4,
                [
                    new FrequenciaAlunoLote(cenario.MatriculaAtivaA, Presente: true),
                    new FrequenciaAlunoLote(cenario.MatriculaAtivaB, Presente: false),
                    new FrequenciaAlunoLote(cenario.MatriculaEncerrada, Presente: true),
                ]),
            default);

        lancados.Should().Be(2, "apenas as 2 matriculas Ativas com diario aberto recebem o lancamento");
    }

    [Fact] // Nota em lote + boletim reflete a media por componente e o % de frequencia.
    public async Task Nota_em_lote_reflete_no_boletim()
    {
        await using var contexto = CriarContexto(TenantA);
        var cenario = await SemearTurmaAsync(contexto);
        var componente = Guid.NewGuid();

        var freqHandler = new RegistrarFrequenciaTurmaHandler(
            new MatriculaRepository(contexto), new DiarioClasseRepository(contexto), contexto);
        // presenca em 600 das 800 h => 75% (inclusivo).
        await freqHandler.Handle(
            new RegistrarFrequenciaTurmaCommand(cenario.TurmaId, DiaAula, 600,
                [new FrequenciaAlunoLote(cenario.MatriculaAtivaA, true)]), default);

        var notaHandler = new LancarNotasTurmaHandler(
            new MatriculaRepository(contexto), new DiarioClasseRepository(contexto), contexto);
        await notaHandler.Handle(
            new LancarNotasTurmaCommand(cenario.TurmaId, componente, "1Bim",
                [new NotaAlunoLote(cenario.MatriculaAtivaA, 8m), new NotaAlunoLote(cenario.MatriculaAtivaB, 6m)]),
            default);
        await notaHandler.Handle(
            new LancarNotasTurmaCommand(cenario.TurmaId, componente, "2Bim",
                [new NotaAlunoLote(cenario.MatriculaAtivaA, 6m)]),
            default);

        var readModel = new DiarioTurmaReadModel(contexto);
        var boletim = await readModel.ObterBoletimAsync(cenario.MatriculaAtivaA, default);

        boletim.Should().NotBeNull();
        boletim!.PercentualFrequencia.Should().Be(0.75m);
        var media = boletim.Medias.Single(m => m.ComponenteCurricularId == componente);
        media.Media.Should().Be(7.0m, "(8 + 6) / 2 = 7,0");
        media.QuantidadeNotas.Should().Be(2);
    }

    [Fact] // Diario coletivo da turma reflete a presenca do dia e lista so alunos Ativos.
    public async Task Diario_da_turma_reflete_presenca_do_dia()
    {
        await using var contexto = CriarContexto(TenantA);
        var cenario = await SemearTurmaAsync(contexto);

        var freqHandler = new RegistrarFrequenciaTurmaHandler(
            new MatriculaRepository(contexto), new DiarioClasseRepository(contexto), contexto);
        await freqHandler.Handle(
            new RegistrarFrequenciaTurmaCommand(cenario.TurmaId, DiaAula, 4,
                [
                    new FrequenciaAlunoLote(cenario.MatriculaAtivaA, true),
                    new FrequenciaAlunoLote(cenario.MatriculaAtivaB, false),
                ]),
            default);

        var readModel = new DiarioTurmaReadModel(contexto);
        var view = await readModel.ObterDiarioDaTurmaAsync(cenario.TurmaId, DiaAula, default);

        view.Should().NotBeNull();
        view!.Linhas.Should().HaveCount(2, "so as matriculas Ativas da turma compoem a chamada");
        view.Linhas.Single(l => l.MatriculaId == cenario.MatriculaAtivaA).Presente.Should().BeTrue();
        view.Linhas.Single(l => l.MatriculaId == cenario.MatriculaAtivaB).Presente.Should().BeFalse();
    }

    [Fact] // Invariante: nao lanca em diario fechado (Apurado) — diario apurado e ignorado no lote.
    public async Task Lote_nao_lanca_em_diario_apurado()
    {
        await using var contexto = CriarContexto(TenantA);
        var cenario = await SemearTurmaAsync(contexto);

        // Apura o diario da matricula A (fecha para novos lancamentos).
        var diarioA = await contexto.DiariosClasse
            .Include(d => d.Frequencias)
            .SingleAsync(d => d.MatriculaId == new MatriculaId(cenario.MatriculaAtivaA));
        diarioA.RegistrarFrequencia(DiaAula, presente: true, cargaHorariaAula: 800);
        diarioA.ApurarResultado();
        await contexto.SaveChangesAsync();

        var handler = new LancarNotasTurmaHandler(
            new MatriculaRepository(contexto), new DiarioClasseRepository(contexto), contexto);
        var lancados = await handler.Handle(
            new LancarNotasTurmaCommand(cenario.TurmaId, Guid.NewGuid(), "1Bim",
                [
                    new NotaAlunoLote(cenario.MatriculaAtivaA, 9m), // diario apurado => ignorado.
                    new NotaAlunoLote(cenario.MatriculaAtivaB, 7m), // aberto => lancado.
                ]),
            default);

        lancados.Should().Be(1, "o diario ja apurado nao recebe novos lancamentos");
    }

    [Fact] // Invariante: matricula sem diario aberto nao recebe lancamento (sem matricula/diario no conjunto).
    public async Task Lote_para_matricula_sem_diario_nao_lanca()
    {
        await using var contexto = CriarContexto(TenantA);
        var cenario = await SemearTurmaAsync(contexto);

        var handler = new RegistrarFrequenciaTurmaHandler(
            new MatriculaRepository(contexto), new DiarioClasseRepository(contexto), contexto);

        // matricula encerrada (e sem diario) — nao deve lancar nada.
        var lancados = await handler.Handle(
            new RegistrarFrequenciaTurmaCommand(cenario.TurmaId, DiaAula, 4,
                [new FrequenciaAlunoLote(cenario.MatriculaEncerrada, true)]),
            default);

        lancados.Should().Be(0);
    }

    [Fact] // Historico escolar longitudinal reflete a matricula/turma e o resultado quando apurado.
    public async Task Historico_escolar_reflete_matriculas_do_aluno()
    {
        await using var contexto = CriarContexto(TenantA);
        var cenario = await SemearTurmaAsync(contexto);

        var readModel = new DiarioTurmaReadModel(contexto);
        var historico = await readModel.ObterHistoricoEscolarAsync(cenario.AlunoAtivoA, default);

        historico.AlunoId.Should().Be(cenario.AlunoAtivoA);
        historico.Itens.Should().ContainSingle()
            .Which.TurmaId.Should().Be(cenario.TurmaId);
    }

    [Fact] // Read model e tenant-scoped: outro tenant nao enxerga o diario da turma.
    public async Task Diario_da_turma_e_isolado_por_tenant()
    {
        Guid turmaId;
        await using (var contexto = CriarContexto(TenantA))
        {
            var cenario = await SemearTurmaAsync(contexto);
            turmaId = cenario.TurmaId;
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            var readModel = new DiarioTurmaReadModel(contexto);
            var view = await readModel.ObterDiarioDaTurmaAsync(turmaId, DiaAula, default);
            view.Should().BeNull("a turma pertence ao TenantA — o Global Query Filter isola");
        }
    }
}
