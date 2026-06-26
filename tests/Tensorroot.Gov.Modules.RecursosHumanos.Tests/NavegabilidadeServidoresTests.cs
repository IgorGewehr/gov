using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura da NAVEGABILIDADE (Onda 0): busca/filtros paginados de servidores e ficha funcional,
/// com isolamento por tenant (Global Query Filter), sobre SQLite em memoria.
/// </summary>
public sealed class NavegabilidadeServidoresTests : RecursosHumanosTestBase
{
    private static readonly DateOnly Nomeacao = new(2026, 1, 5);

    [Fact]
    public async Task Busca_por_nome_matricula_situacao_e_regime()
    {
        var cargoId = CargoId.New();
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Servidores.Add(NovoServidor("MAT-0001", "11144477735", "Joao Pereira", RegimePrevidenciario.Rpps, cargoId));
            contexto.Servidores.Add(NovoServidor("MAT-0002", "12345678909", "Maria Souza", RegimePrevidenciario.Rgps, CargoId.New()));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            contexto.Servidores.Add(NovoServidor("MAT-9999", "52998224725", "Joao do Tenant B", RegimePrevidenciario.Rpps, CargoId.New()));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new ServidorRepository(contexto);

            var porNome = await repo.BuscarAsync("joao", null, null, null, 1, 10, default);
            porNome.Total.Should().Be(1, "tenant A so tem um Joao");
            porNome.Itens.Single().DadosPessoais.Nome.Should().Be("Joao Pereira");

            var porMatricula = await repo.BuscarAsync("MAT-0002", null, null, null, 1, 10, default);
            porMatricula.Total.Should().Be(1);

            var porRegime = await repo.BuscarAsync(null, null, RegimePrevidenciario.Rgps, null, 1, 10, default);
            porRegime.Total.Should().Be(1);
            porRegime.Itens.Single().Regime.Should().Be(RegimePrevidenciario.Rgps);

            var porCargo = await repo.BuscarAsync(null, null, null, cargoId, 1, 10, default);
            porCargo.Total.Should().Be(1);

            var porSituacao = await repo.BuscarAsync(null, SituacaoServidor.Nomeado, null, null, 1, 10, default);
            porSituacao.Total.Should().Be(2, "ambos nascem Nomeado");
        }
    }

    [Fact]
    public async Task Ficha_funcional_traz_dados_vinculo_cargo_e_timeline()
    {
        var cargoId = CargoId.New();
        Guid servidorId;
        await using (var contexto = CriarContexto(TenantA))
        {
            var cargo = Cargo.Criar(
                TenantA, "Analista Administrativo", TipoCargo.Efetivo, Vencimento.De(5000m),
                Lotacao.Criar("12345678000190", "Secretaria de Administracao", "1010"), 3, "Lei 100/2020",
                PoliticaPrevidenciaria.ComRppsProprio); // ente com RPPS proprio: efetivo -> RPPS (coerente com o servidor).
            // O servidor referencia o cargo pelo Id real do cargo criado.
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("11144477735"), Matricula.De("MAT-0001"),
                DadosPessoais.Criar("Joao Pereira", new DateOnly(1985, 7, 2)),
                cargo.Id, RegimePrevidenciario.Rpps, Nomeacao);
            servidor.RegistrarPosse(Nomeacao.AddDays(10));
            servidor.IniciarExercicio(Nomeacao.AddDays(15));
            servidorId = servidor.Id.Value;

            contexto.Cargos.Add(cargo);
            contexto.Servidores.Add(servidor);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var handler = new ObterFichaFuncionalHandler(
                new ServidorRepository(contexto),
                new CargoRepository(contexto),
                new FolhaDePagamentoRepository(contexto),
                new ApuracaoPontoRepository(contexto));

            var ficha = await handler.Handle(new ObterFichaFuncionalQuery(servidorId), default);

            ficha.Should().NotBeNull();
            ficha!.DadosPessoais.Nome.Should().Be("Joao Pereira");
            ficha.DadosPessoais.Cpf.Should().Be("***.444.***-**", "CPF mascarado — LGPD");
            ficha.Vinculo.Cargo.Should().Be("Analista Administrativo");
            ficha.Vinculo.Vencimento.Should().Be(5000m);
            ficha.Vinculo.Lotacao.Should().Be("Secretaria de Administracao");
            ficha.Vinculo.Situacao.Should().Be(nameof(SituacaoServidor.EmExercicio));
            // Timeline em ordem cronologica: Nomeacao -> Posse -> Exercicio.
            ficha.Timeline.Select(t => t.Evento).Should().ContainInOrder("Nomeacao", "Posse", "Exercicio");
        }
    }

    [Fact]
    public async Task Ficha_de_servidor_inexistente_retorna_nulo()
    {
        await using var contexto = CriarContexto(TenantA);
        var handler = new ObterFichaFuncionalHandler(
            new ServidorRepository(contexto),
            new CargoRepository(contexto),
            new FolhaDePagamentoRepository(contexto),
            new ApuracaoPontoRepository(contexto));

        var ficha = await handler.Handle(new ObterFichaFuncionalQuery(Guid.NewGuid()), default);

        ficha.Should().BeNull();
    }

    private static Servidor NovoServidor(string matricula, string cpf, string nome, RegimePrevidenciario regime, CargoId cargoId)
        => Servidor.Admitir(
            TenantA,
            Cpf.Create(cpf),
            Matricula.De(matricula),
            DadosPessoais.Criar(nome, new DateOnly(1990, 3, 20)),
            cargoId,
            regime,
            Nomeacao);
}
