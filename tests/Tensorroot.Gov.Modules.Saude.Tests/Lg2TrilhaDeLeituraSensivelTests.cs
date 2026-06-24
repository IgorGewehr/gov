using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Behaviors;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Saude.Application.Atendimento;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// LG-2: toda leitura de dado pessoal SENSIVEL (queries <c>ISensivelLgpd</c>) gera trilha de ACESSO
/// append-only e selada na cadeia de hash da auditoria — quem leu, de qual entidade, sob qual base
/// legal, quando, de qual IP. Antes, a leitura de PEP/historico clinico nao deixava rastro algum.
/// Cobre o behavior transversal + o adaptador <see cref="RegistroAcessoSensivel"/>.
/// </summary>
public sealed class Lg2TrilhaDeLeituraSensivelTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string CnsValido = "700000000000005";
    private static readonly Guid PrincipalSub = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000a001");

    private readonly SqliteConnection _connection;

    public Lg2TrilhaDeLeituraSensivelTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact] // A leitura do historico clinico sela uma linha de acesso (Action=Read) na trilha.
    public async Task Leitura_de_historico_clinico_sela_acesso_na_trilha()
    {
        Guid pacienteId;
        await using (var seed = CriarContexto(out _))
        {
            var paciente = Paciente.Cadastrar(
                TenantA, new Cns(CnsValido), NovaIdentificacao(), NovoEndereco());
            paciente.RegistrarCondicaoDeSaude("E11", "Diabetes tipo 2", new DateOnly(2026, 6, 1));
            pacienteId = paciente.Id.Value;
            seed.Pacientes.Add(paciente);
            await seed.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(out var holder))
        {
            var response = await ExecutarLeituraSensivel(
                contexto, holder,
                new ObterHistoricoClinicoDoPacienteQuery(pacienteId));

            response.PacienteId.Should().Be(pacienteId);

            var acessos = await contexto.AuditTrail
                .Where(t => t.Action == RegistroAcessoSensivel.AcaoLeitura)
                .ToListAsync();

            acessos.Should().ContainSingle();
            var acesso = acessos.Single();
            acesso.EntityName.Should().Be(nameof(Paciente));
            acesso.EntityId.Should().Be(pacienteId.ToString());
            acesso.UserId.Should().Be(PrincipalSub.ToString());
            acesso.IpAddress.Should().Be("203.0.113.7");
            acesso.NewValues.Should().Contain("TutelaDaSaude"); // base legal estruturada (LG-A2)
            acesso.Sequencia.Should().BeGreaterThan(0);
            acesso.HashAtual.Should().NotBeNullOrEmpty();
        }
    }

    [Fact] // SA-2 (W10.6): a leitura do detalhe de um atendimento (prontuario SOAP) sela acesso na trilha.
    public async Task Leitura_de_atendimento_sela_acesso_na_trilha()
    {
        Guid atendimentoId;
        await using (var seed = CriarContexto(out _))
        {
            var atendimento = Atendimento.Registrar(
                TenantA,
                Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId.New(),
                EstabelecimentoId.New(),
                Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId.New(),
                new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero),
                Competencia.De(new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero)),
                ModalidadeAtendimento.Presencial);
            atendimento.AdicionarEvolucaoSOAP(
                "Cefaleia", "PA 120x80", "Cefaleia tensional", "Analgesico",
                new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero));
            atendimentoId = atendimento.Id.Value;
            seed.Atendimentos.Add(atendimento);
            await seed.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(out var holder))
        {
            var query = new ObterAtendimentoPorIdQuery(atendimentoId);
            var handler = new ObterAtendimentoPorIdHandler(new AtendimentoRepository(contexto));
            var registro = new RegistroAcessoSensivel(
                holder, new CurrentUserFixo(PrincipalSub.ToString()), new TenantContextFake(TenantA), TimeProvider.System);
            var behavior = new TrilhaAcessoSensivelBehavior<ObterAtendimentoPorIdQuery, AtendimentoDetalhe?>(
                registro, NullLogger<TrilhaAcessoSensivelBehavior<ObterAtendimentoPorIdQuery, AtendimentoDetalhe?>>.Instance);

            var detalhe = await behavior.Handle(
                query, () => handler.Handle(query, CancellationToken.None), CancellationToken.None);
            detalhe!.Id.Should().Be(atendimentoId);

            var acessos = await contexto.AuditTrail
                .Where(t => t.Action == RegistroAcessoSensivel.AcaoLeitura)
                .ToListAsync();

            acessos.Should().ContainSingle();
            var acesso = acessos.Single();
            acesso.EntityName.Should().Be("PacienteRaiz"); // a entidade sensivel da query e o Paciente (EntidadeSensivel)
            acesso.EntityId.Should().Be(atendimentoId.ToString());
            acesso.UserId.Should().Be(PrincipalSub.ToString());
            acesso.NewValues.Should().Contain("TutelaDaSaude"); // base legal estruturada (LG-A2)
            acesso.HashAtual.Should().NotBeNullOrEmpty();
        }
    }

    [Fact] // A linha de acesso entra na cadeia de hash sem quebra-la: o verificador confirma integridade.
    public async Task Acesso_selado_mantem_a_cadeia_integra()
    {
        Guid pacienteId;
        await using (var seed = CriarContexto(out _))
        {
            var paciente = Paciente.Cadastrar(TenantA, new Cns(CnsValido), NovaIdentificacao(), NovoEndereco());
            pacienteId = paciente.Id.Value;
            seed.Pacientes.Add(paciente);
            await seed.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(out var holder))
        {
            await ExecutarLeituraSensivel(
                contexto, holder, new ObterHistoricoClinicoDoPacienteQuery(pacienteId));

            var verificador = new VerificadorTrilhaAuditoria();
            var resultado = await verificador.VerificarAsync(
                contexto.AuditTrail.AsNoTracking(), TenantA, CancellationToken.None);

            resultado.Integra.Should().BeTrue();
        }
    }

    [Fact] // LG-3 (cross-check): o CNS do paciente NUNCA aparece em claro na trilha de escrita.
    public async Task Cns_e_redigido_na_trilha_de_escrita()
    {
        await using var contexto = CriarContexto(out _);
        var paciente = Paciente.Cadastrar(TenantA, new Cns(CnsValido), NovaIdentificacao(), NovoEndereco());
        contexto.Pacientes.Add(paciente);
        await contexto.SaveChangesAsync();

        var linhas = await contexto.AuditTrail.Where(t => t.EntityName == nameof(Paciente)).ToListAsync();
        linhas.Should().NotBeEmpty();
        linhas.Select(l => l.NewValues).Should().OnlyContain(v => v == null || !v!.Contains(CnsValido));
        linhas.Should().Contain(l => l.NewValues != null && l.NewValues!.Contains("[REDACTED]"));
    }

    private static async Task<HistoricoClinicoDto> ExecutarLeituraSensivel(
        SaudeDbContext contexto,
        ScopeDbContextHolder holder,
        ObterHistoricoClinicoDoPacienteQuery query)
    {
        var handler = new ObterHistoricoClinicoDoPacienteHandler(new PacienteRepository(contexto));
        var registro = new RegistroAcessoSensivel(
            holder, new CurrentUserFixo(PrincipalSub.ToString()), new TenantContextFake(TenantA), TimeProvider.System);
        var behavior = new TrilhaAcessoSensivelBehavior<ObterHistoricoClinicoDoPacienteQuery, HistoricoClinicoDto>(
            registro, NullLogger<TrilhaAcessoSensivelBehavior<ObterHistoricoClinicoDoPacienteQuery, HistoricoClinicoDto>>.Instance);

        return await behavior.Handle(
            query,
            () => handler.Handle(query, CancellationToken.None),
            CancellationToken.None);
    }

    private SaudeDbContext CriarContexto(out ScopeDbContextHolder holder)
    {
        var tenantContext = new TenantContextFake(TenantA);
        holder = new ScopeDbContextHolder();
        var options = new DbContextOptionsBuilder<SaudeDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFixo(PrincipalSub.ToString()), TimeProvider.System))
            .Options;

        var contexto = new SaudeDbContext(options, tenantContext, holder);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    private static Identificacao NovaIdentificacao()
        => new("Maria da Silva", new DateOnly(1990, 5, 10), Sexo.Feminino, "Maria S.", Cpf.Create("52998224725"), new DateOnly(2026, 6, 21));

    private static Endereco NovoEndereco()
        => new("Rua das Flores", "100", "Centro", "Maximiliano de Almeida", "RS", "99970000");

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class CurrentUserFixo(string? userId) : ICurrentUser
    {
        public string? UserId => userId;

        public string? UserName => "Principal";

        public string? IpAddress => "203.0.113.7";
    }
}
