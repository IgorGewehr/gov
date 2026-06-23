using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura dos relatorios gerenciais da folha (ONDA3-DESIGN §4.1, sub-onda 3a): folha por
/// secretaria/UO e por fonte soma certo, evolucao mensal da despesa de pessoal, mapa de cargos
/// (ocupados x vagos) e demonstrativo TCE com a contribuicao previdenciaria do segurado vinda das
/// rubricas parametrizadas (nunca hardcoded). Read models puros sobre os agregados existentes,
/// atravessando o provider EF (<see cref="RelatoriosFolhaConsulta"/>) sobre SQLite em memoria,
/// com o Global Query Filter por tenant ativo.
/// </summary>
public sealed class RelatoriosGerenciaisFolhaTests : RecursosHumanosTestBase
{
    private static readonly DateOnly DataFolha = new(2026, 6, 30);

    private sealed class ParametrosFolhaProviderFake : IParametrosFolhaProvider
    {
        public Task<ParametrosFolha> ObterAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ParametrosFolha());
    }

    private static Cargo NovoCargo(
        string denominacao, TipoCargo tipo, string unidade, decimal vencimento, int vagas)
        => Cargo.Criar(
            TenantA,
            denominacao,
            tipo,
            Vencimento.De(vencimento),
            Lotacao.Criar("12345678000190", unidade, "1010"),
            vagas,
            "Lei Municipal 1.000/2020");

    private static Servidor NovoServidor(CargoId cargoId, RegimePrevidenciario regime, string matricula, string cpf)
        => Servidor.Admitir(
            TenantA,
            Cpf.Create(cpf),
            Matricula.De(matricula),
            DadosPessoais.Criar("Servidor de Teste", new DateOnly(1985, 4, 2)),
            cargoId,
            regime,
            new DateOnly(2010, 3, 1));

    [Fact] // Folha por secretaria/UO: cada UO soma proventos/descontos/liquido e conta servidores distintos.
    public async Task Folha_por_secretaria_soma_por_unidade_de_lotacao()
    {
        var cargoSaude = NovoCargo("Medico", TipoCargo.Efetivo, "Secretaria de Saude", 8000m, 5);
        var cargoEduc = NovoCargo("Professor", TipoCargo.Efetivo, "Secretaria de Educacao", 6000m, 5);

        var servSaude = NovoServidor(cargoSaude.Id, RegimePrevidenciario.Rpps, "0001", "52998224725");
        var servEduc = NovoServidor(cargoEduc.Id, RegimePrevidenciario.Rgps, "0002", "15350946056");

        var folha = FolhaDePagamento.Abrir(TenantA, Competencia.De(2026, 6));
        folha.AdicionarEvento(servSaude.Id.Value, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(8000m), 8000m, RegimePrevidenciario.Rpps);
        folha.AdicionarEvento(servSaude.Id.Value, Rubrica.De("RPPS"), TipoEvento.Desconto, BaseCalculo.De(8000m), 1120m, RegimePrevidenciario.Rpps);
        folha.AdicionarEvento(servEduc.Id.Value, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(6000m), 6000m, RegimePrevidenciario.Rgps);
        folha.AdicionarEvento(servEduc.Id.Value, Rubrica.De("INSS"), TipoEvento.Desconto, BaseCalculo.De(6000m), 700m, RegimePrevidenciario.Rgps);

        await using var contexto = CriarContexto(TenantA);
        contexto.Cargos.AddRange(cargoSaude, cargoEduc);
        contexto.Servidores.AddRange(servSaude, servEduc);
        contexto.FolhasDePagamento.Add(folha);
        await contexto.SaveChangesAsync();

        var consulta = new RelatoriosFolhaConsulta(contexto, new ParametrosFolhaProviderFake());
        var view = await consulta.ObterFolhaPorSecretariaAsync(2026, 6, default);

        view.Should().NotBeNull();
        view!.TotalProventos.Should().Be(14000m);
        view.TotalDescontos.Should().Be(1820m);
        view.TotalLiquido.Should().Be(12180m);
        view.QuantidadeServidores.Should().Be(2);

        var saude = view.PorSecretaria.Single(l => l.Unidade == "Secretaria de Saude");
        saude.TotalProventos.Should().Be(8000m);
        saude.TotalDescontos.Should().Be(1120m);
        saude.TotalLiquido.Should().Be(6880m);
        saude.QuantidadeServidores.Should().Be(1);

        // Quebra por fonte (regime): RPPS x RGPS.
        view.PorFonte.Single(f => f.Fonte == "Rpps").TotalProventos.Should().Be(8000m);
        view.PorFonte.Single(f => f.Fonte == "Rgps").TotalProventos.Should().Be(6000m);
    }

    [Fact] // Evolucao mensal: serie ordenada por competencia, acumulado e media batem.
    public async Task Evolucao_despesa_acumula_e_calcula_media_por_mes_com_folha()
    {
        var maio = FolhaDePagamento.Abrir(TenantA, Competencia.De(2026, 5));
        maio.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(10000m), 10000m, RegimePrevidenciario.Rpps);

        var junho = FolhaDePagamento.Abrir(TenantA, Competencia.De(2026, 6));
        junho.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(12000m), 12000m, RegimePrevidenciario.Rpps);

        await using var contexto = CriarContexto(TenantA);
        contexto.FolhasDePagamento.AddRange(maio, junho);
        await contexto.SaveChangesAsync();

        var consulta = new RelatoriosFolhaConsulta(contexto, new ParametrosFolhaProviderFake());
        var view = await consulta.ObterEvolucaoDespesaAsync(2026, 5, 2026, 6, default);

        view.MesesComFolha.Should().Be(2);
        view.DespesaBrutaAcumulada.Should().Be(22000m);
        view.DespesaBrutaMediaMensal.Should().Be(11000m);
        view.Serie.Should().HaveCount(2);
        view.Serie[0].Competencia.Should().Be("2026-05", "serie ordenada por competencia crescente");
        view.Serie[1].Competencia.Should().Be("2026-06");
        view.Serie[1].DespesaBruta.Should().Be(12000m);
    }

    [Fact] // Mapa de cargos: ocupados x vagos por cargo, totalizadores por tipo e geral.
    public async Task Mapa_de_cargos_soma_vagas_ocupadas_e_disponiveis()
    {
        var efetivo = NovoCargo("Auxiliar Administrativo", TipoCargo.Efetivo, "Secretaria de Administracao", 3000m, 10);
        efetivo.Prover();
        efetivo.Prover();
        efetivo.Prover(); // 3 ocupadas de 10 => 7 vagas.

        var comissionado = NovoCargo("Assessor", TipoCargo.Comissionado, "Gabinete", 5000m, 4);
        comissionado.Prover(); // 1 de 4 => 3 vagas.

        await using var contexto = CriarContexto(TenantA);
        contexto.Cargos.AddRange(efetivo, comissionado);
        await contexto.SaveChangesAsync();

        var consulta = new RelatoriosFolhaConsulta(contexto, new ParametrosFolhaProviderFake());
        var view = await consulta.ObterMapaCargosAsync(default);

        view.TotalVagasAutorizadas.Should().Be(14);
        view.TotalVagasOcupadas.Should().Be(4);
        view.TotalVagasDisponiveis.Should().Be(10);

        var linhaEfetivo = view.Cargos.Single(c => c.Denominacao == "Auxiliar Administrativo");
        linhaEfetivo.VagasOcupadas.Should().Be(3);
        linhaEfetivo.VagasDisponiveis.Should().Be(7);

        var tipoEfetivo = view.PorTipo.Single(t => t.Tipo == "Efetivo");
        tipoEfetivo.VagasAutorizadas.Should().Be(10);
        tipoEfetivo.VagasOcupadas.Should().Be(3);
    }

    [Fact] // Demonstrativo TCE: contribuicao do segurado vem das rubricas previdenciarias parametrizadas (nao hardcoded).
    public async Task Demonstrativo_tce_apura_contribuicao_do_segurado_pelas_rubricas_parametrizadas()
    {
        var folha = FolhaDePagamento.Abrir(TenantA, Competencia.De(2026, 6));
        var servidor = Guid.NewGuid();
        folha.AdicionarEvento(servidor, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(8000m), 8000m, RegimePrevidenciario.Rpps);
        // Rubrica de desconto previdenciario do segurado (codigo padrao "RPPS" nos parametros).
        folha.AdicionarEvento(servidor, Rubrica.De("RPPS"), TipoEvento.Desconto, BaseCalculo.De(8000m), 1120m, RegimePrevidenciario.Rpps);
        // Desconto NAO previdenciario (IRRF) — nao deve compor a contribuicao do segurado.
        folha.AdicionarEvento(servidor, Rubrica.De("IRRF"), TipoEvento.Desconto, BaseCalculo.De(8000m), 500m, RegimePrevidenciario.Rpps);

        await using var contexto = CriarContexto(TenantA);
        contexto.FolhasDePagamento.Add(folha);
        await contexto.SaveChangesAsync();

        var consulta = new RelatoriosFolhaConsulta(contexto, new ParametrosFolhaProviderFake());
        var view = await consulta.ObterDemonstrativoTceAsync(2026, 6, default);

        view.Should().NotBeNull();
        view!.TotalProventos.Should().Be(8000m);
        view.TotalDescontos.Should().Be(1620m);
        view.ContribuicaoPrevidenciariaSegurado.Should().Be(1120m,
            "so a rubrica previdenciaria (RPPS) compoe a contribuicao do segurado — IRRF nao entra");
    }

    [Fact] // Sem folha na competencia => relatorios da competencia retornam null (nao inventam dado).
    public async Task Relatorios_sem_folha_na_competencia_retornam_null()
    {
        await using var contexto = CriarContexto(TenantA);
        var consulta = new RelatoriosFolhaConsulta(contexto, new ParametrosFolhaProviderFake());

        (await consulta.ObterFolhaPorSecretariaAsync(2030, 1, default)).Should().BeNull();
        (await consulta.ObterDemonstrativoTceAsync(2030, 1, default)).Should().BeNull();
    }
}
