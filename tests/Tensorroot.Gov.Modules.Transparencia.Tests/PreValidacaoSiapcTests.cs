using System.Text;
using FluentAssertions;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Cobertura da pré-validação LOCAL (e-Validador/RDI): regras estruturais/obrigatórios/somatórios que
/// BLOQUEIAM. A remessa só fica apta (sem erro) quando todas as críticas bloqueantes passam.
/// </summary>
public sealed class PreValidacaoSiapcTests
{
    private static LeiauteSiapc LeiauteExemplo()
        => LeiauteSiapc.Definir(
            "SIAPC",
            "2026",
            'P',
            [
                RegistroLeiauteDef.Definir(
                    "10",
                    "BALANCETE.TXT",
                    [
                        CampoLeiaute.Definir("Conta", 1, 10, TipoCampoLeiaute.Caractere, true),
                        CampoLeiaute.Definir("Valor", 11, 8, TipoCampoLeiaute.Valor, true),
                    ]),
            ]);

    private static ArquivoMontado Arquivo(LeiauteSiapc leiaute, params (string conta, decimal valor, bool contaVazia)[] linhas)
    {
        var definicao = leiaute.Registros[0];
        var valoresPorLinha = linhas
            .Select(linha => (IReadOnlyDictionary<string, ValorCampo>)new Dictionary<string, ValorCampo>
            {
                ["Conta"] = ValorCampo.Caractere("Conta", linha.contaVazia ? null : linha.conta),
                ["Valor"] = ValorCampo.Monetario("Valor", linha.valor),
            })
            .ToList();

        return new ArquivoMontado(definicao, valoresPorLinha, ReadOnlyMemory<byte>.Empty);
    }

    [Fact] // Sem erros estruturais ⇒ remessa apta (RDI limpo).
    public void Remessa_estruturalmente_valida_nao_tem_erro()
    {
        var leiaute = LeiauteExemplo();
        var arquivos = new List<ArquivoMontado> { Arquivo(leiaute, ("111110100", 100m, false)) };

        var ocorrencias = MotorPreValidacaoSiapc.Validar(leiaute, arquivos);

        ocorrencias.Should().BeEmpty();
    }

    [Fact] // Campo obrigatorio vazio ⇒ ERRO que bloqueia.
    public void Campo_obrigatorio_vazio_bloqueia()
    {
        var leiaute = LeiauteExemplo();
        var arquivos = new List<ArquivoMontado> { Arquivo(leiaute, ("ignorada", 100m, contaVazia: true)) };

        var ocorrencias = MotorPreValidacaoSiapc.Validar(leiaute, arquivos);

        ocorrencias.Should().Contain(o => o.Severidade == SeveridadeOcorrencia.Erro);
    }

    [Fact] // Arquivo sem linhas ⇒ ERRO que bloqueia.
    public void Arquivo_sem_linhas_bloqueia()
    {
        var leiaute = LeiauteExemplo();
        var arquivos = new List<ArquivoMontado> { Arquivo(leiaute) };

        var ocorrencias = MotorPreValidacaoSiapc.Validar(leiaute, arquivos);

        ocorrencias.Should().Contain(o => o.Severidade == SeveridadeOcorrencia.Erro);
    }

    [Fact] // RDI: ao menos um erro ⇒ a remessa fica Rejeitada (envio/empacotamento bloqueado).
    public void Rdi_com_erro_rejeita_a_remessa()
    {
        var leiaute = LeiauteExemplo();
        var arquivos = new List<ArquivoMontado> { Arquivo(leiaute, ("x", 100m, contaVazia: true)) };
        var ocorrencias = MotorPreValidacaoSiapc.Validar(leiaute, arquivos);

        var rdi = ResultadoValidacao.Criar("2026", DateTimeOffset.UtcNow, ocorrencias);
        var remessa = RemessaTceFactory.NovaGerada();

        remessa.RegistrarResultadoValidacao(rdi);

        rdi.PossuiErro.Should().BeTrue();
        remessa.Situacao.Should().Be(SituacaoRemessaTce.Rejeitada);
    }

    [Fact] // Somatorio divergente ⇒ ERRO.
    public void Somatorio_divergente_gera_erro()
    {
        var ocorrencia = MotorPreValidacaoSiapc.ConferirSomatorio("BALANCETE.TXT", "debito x credito", 100m, 90m);

        ocorrencia.Should().NotBeNull();
        ocorrencia!.Severidade.Should().Be(SeveridadeOcorrencia.Erro);
    }

    [Fact] // Somatorio que confere ⇒ sem ocorrencia.
    public void Somatorio_conferente_nao_gera_ocorrencia()
    {
        MotorPreValidacaoSiapc.ConferirSomatorio("BALANCETE.TXT", "debito x credito", 100m, 100m)
            .Should().BeNull();
    }

    [Fact] // O e-Validador LOCAL real bloqueia uma remessa com conteudo posicional malformado.
    public async Task Validador_local_bloqueia_remessa_com_conteudo_malformado()
    {
        // Conteudo com largura de linha errada e finalizador inconsistente (proposital).
        var conteudoRuim = Encoding.Latin1.GetBytes("CABECALHO\r\nlinha-curta\r\nFINALIZADOR0000000009\r\n");
        var arquivo = ArquivoRemessa.Criar("BALANCETE.TXT", conteudoRuim, [RegistroLeiaute.Criar("10", "l")]);
        var remessa = RemessaTce.GerarRemessa(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Periodo.De(2026, TipoPeriodo.Bimestre, 3),
            Leiaute.De("SIAPC", "2026"),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 7, 1),
            [arquivo],
            conteudoRuim);

        var validador = new EValidadorLocalSiapc(new CatalogoFake(), TimeProvider.System);
        var rdi = await validador.ValidarAsync(remessa, CancellationToken.None);

        rdi.PossuiErro.Should().BeTrue();
        remessa.RegistrarResultadoValidacao(rdi);
        remessa.Situacao.Should().Be(SituacaoRemessaTce.Rejeitada);
    }

    // Catalogo de teste: resolve um leiaute BALANCETE.TXT com largura conhecida (10 + 8 = 18).
    private sealed class CatalogoFake : ILeiauteCatalogo
    {
        public Task<bool> SuportaAsync(
            Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiaute leiaute, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<LeiauteSiapc> ResolverAsync(
            Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiaute leiaute, CancellationToken cancellationToken)
            => Task.FromResult(LeiauteSiapc.Definir(
                "SIAPC",
                "2026",
                'P',
                [
                    RegistroLeiauteDef.Definir(
                        "10",
                        "BALANCETE.TXT",
                        [
                            CampoLeiaute.Definir("Conta", 1, 10, TipoCampoLeiaute.Caractere, true),
                            CampoLeiaute.Definir("Valor", 11, 8, TipoCampoLeiaute.Valor, true),
                        ]),
                ]));

        public Task<DateOnly> ObterDataLimiteAsync(Periodo periodo, CancellationToken cancellationToken)
            => Task.FromResult(new DateOnly(2026, 8, 31));

        public Task<IdentificacaoEnteRemessa> ObterIdentificacaoEnteAsync(Periodo periodo, CancellationToken cancellationToken)
            => Task.FromResult(new IdentificacaoEnteRemessa(
                "99999999000199", "PREFEITURA", new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30), 1));
    }
}

/// <summary>Fábrica auxiliar de remessa Gerada para os testes de pré-validação.</summary>
internal static class RemessaTceFactory
{
    public static RemessaTce NovaGerada()
    {
        var conteudo = System.Text.Encoding.Latin1.GetBytes("conteudo");
        var arquivo = ArquivoRemessa.Criar("BALANCETE.TXT", conteudo, [RegistroLeiaute.Criar("10", "linha")]);
        return RemessaTce.GerarRemessa(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Periodo.De(2026, TipoPeriodo.Bimestre, 3),
            Leiaute.De("SIAPC", "2026"),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 7, 1),
            [arquivo],
            conteudo);
    }
}
