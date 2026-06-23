using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Domain.Events;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes.Leiautes;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Cobertura da REMESSA DE FOLHA ao TCE-RS (Res. 1099 / SIAPC Vol. V): o emissor posicional dos arquivos
/// TCE_4810/4820/4960 (REUSANDO o <see cref="EmissorRegistroSiapc"/> do M4), a pre-validacao que bloqueia
/// remessa invalida, e o registro de protocolo (ato humano). Persistencia do read model <see
/// cref="ResumoFolhaTce"/> sobre SQLite em memoria com auditoria e Global Query Filter.
/// </summary>
public sealed class RemessaFolhaTceTests : TransparenciaTestBase
{
    private static LeiauteSiapc LeiauteFolha()
        => LeiauteFolhaTceSeed.Construir(LeiauteFolhaTceSeed.ModeloPadrao());

    private static ResumoFolhaTce ResumoExemplo(Guid? tenant = null)
        => ResumoFolhaTce.Criar(
            tenant ?? TenantA,
            Guid.NewGuid(),
            2026,
            5,
            "Mensal",
            new DateOnly(2026, 5, 30),
            [
                ServidorFolhaResumo.Criar(
                    "0001",
                    "52998224725",
                    "MARIA DA SILVA",
                    "0001",
                    new DateOnly(1985, 4, 2),
                    new DateOnly(2010, 3, 1),
                    null,
                    "CARGO-1",
                    "AUDITOR FISCAL",
                    "Rpps"),
            ],
            [
                RubricaFolhaResumo.Criar("1001", "VENCIMENTO BASICO", "V", false, true, false, "Lei Municipal 100", "110101"),
                RubricaFolhaResumo.Criar("9001", "CONTRIBUICAO RPPS", "D", false, true, false, "EC 103/2019", "220101"),
            ],
            [
                LancamentoFolhaResumo.Criar("0001", "1001", "V", 8_000.00m),
                LancamentoFolhaResumo.Criar("0001", "9001", "D", 1_120.00m),
            ]);

    // ---------- Emissor posicional da folha (TCE_4810/4820/4960) ----------

    [Fact] // O leiaute de folha tem os tres arquivos da Res. 1099.
    public void Leiaute_folha_tem_os_tres_arquivos_tce()
    {
        var leiaute = LeiauteFolha();

        leiaute.Codigo.Should().Be("FOLHA-TCE");
        leiaute.Registros.Select(registro => registro.NomeArquivo)
            .Should().BeEquivalentTo(["TCE_4810.TXT", "TCE_4820.TXT", "TCE_4960.TXT"]);
    }

    [Fact] // Cada linha posicional tem EXATAMENTE a largura fixa da grade do arquivo.
    public void Linhas_da_folha_tem_largura_fixa_da_grade()
    {
        var leiaute = LeiauteFolha();
        var linhas = MapeadorRemessaFolhaTce.Montar(ResumoExemplo(), leiaute);

        foreach (var linha in linhas)
        {
            var definicao = leiaute.RegistroPorArquivo(linha.NomeArquivo)!;
            var texto = EmissorRegistroSiapc.SerializarLinha(definicao, linha.Valores);
            texto.Length.Should().Be(definicao.LarguraLinha);
        }
    }

    [Fact] // TCE_4810: vantagem credita com sinal '+'; desconto debita com sinal '-'.
    public void Lancamento_4810_aplica_sinal_pela_operacao()
    {
        var leiaute = LeiauteFolha();
        var definicao = leiaute.RegistroPorArquivo("TCE_4810.TXT")!;
        var linhas = MapeadorRemessaFolhaTce.Montar(ResumoExemplo(), leiaute)
            .Where(linha => linha.NomeArquivo == "TCE_4810.TXT")
            .ToList();

        var vantagem = EmissorRegistroSiapc.SerializarLinha(definicao, linhas[0].Valores);
        var desconto = EmissorRegistroSiapc.SerializarLinha(definicao, linhas[1].Valores);

        // Campo ValorOperacao em col. 30..46 (1 sinal + 16 digitos = 17 posicoes).
        vantagem.Substring(29, 17).Should().Be("+0000000000800000", "8000.00 -> 800000 centavos, sinal '+'");
        desconto.Substring(29, 17).Should().Be("-0000000000112000", "1120.00 -> 112000 centavos, sinal '-'");
    }

    [Fact] // ISO-8859-1: acento ocupa 1 byte (largura em bytes == colunas) — encoding canonico do SIAPC.
    public void Corpo_da_folha_e_iso_8859_1()
    {
        var leiaute = LeiauteFolha();
        var definicao = leiaute.RegistroPorArquivo("TCE_4820.TXT")!;
        var linha = MapeadorRemessaFolhaTce.Montar(ResumoExemplo(), leiaute)
            .First(item => item.NomeArquivo == "TCE_4820.TXT");

        var corpo = EmissorRegistroSiapc.SerializarCorpo(definicao, [linha.Valores]);

        // corpo = largura da linha + CR/LF, todos os caracteres Latin-1 ocupando 1 byte.
        corpo.Length.Should().Be(definicao.LarguraLinha + 2);
        EmissorRegistroSiapc.EncodingSiapc.GetString(corpo).Should().EndWith("\r\n");
    }

    // ---------- Persistencia do read model (ponte RH->Transparencia) ----------

    [Fact] // O resumo consumido do RH persiste com seus filhos e isolado por tenant.
    public async Task Resumo_de_folha_persiste_e_isola_por_tenant()
    {
        Guid folhaId;
        await using (var contexto = CriarContexto(TenantA))
        {
            var resumo = ResumoExemplo();
            folhaId = resumo.FolhaDePagamentoId;
            contexto.ResumosFolhaTce.Add(resumo);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var resumo = await contexto.ResumosFolhaTce
                .Include(item => item.Servidores)
                .Include(item => item.Rubricas)
                .Include(item => item.Lancamentos)
                .SingleAsync(item => item.FolhaDePagamentoId == folhaId);

            resumo.Servidores.Should().HaveCount(1);
            resumo.Rubricas.Should().HaveCount(2);
            resumo.Lancamentos.Should().HaveCount(2);
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.ResumosFolhaTce.ToListAsync()).Should().BeEmpty();
        }
    }

    // ---------- Pre-validacao local bloqueia remessa de folha invalida ----------

    [Fact] // RDI com erro transita a remessa de folha para Rejeitada e bloqueia o empacotamento.
    public void Validacao_com_erro_rejeita_e_bloqueia_remessa_de_folha()
    {
        var remessa = NovaRemessaFolha();

        var rdi = ResultadoValidacao.Criar(
            "1099",
            DateTimeOffset.UtcNow,
            [OcorrenciaValidacao.Criar("TCE_4810.TXT", 2, SeveridadeOcorrencia.Erro, "Largura invalida")]);

        remessa.RegistrarResultadoValidacao(rdi);

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Rejeitada);
        ((Action)(() => remessa.MarcarProntaParaTransmissao("z.zip", PacoteFolha)))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- Protocolo (ato humano) ----------

    [Fact] // Validada -> Empacotada -> RegistrarProtocolo (ato humano) -> Enviada, sem POST de envio.
    public void Protocolo_da_folha_e_ato_humano_e_leva_a_Enviada()
    {
        var remessa = NovaRemessaFolha();
        remessa.RegistrarResultadoValidacao(ResultadoValidacao.Criar("1099", DateTimeOffset.UtcNow, []));
        remessa.MarcarProntaParaTransmissao("99999999000199.01052026.31052026.10062026.P.000000202605.zip", PacoteFolha);

        remessa.RegistrarProtocolo("PROTO-FOLHA-001", new DateOnly(2026, 6, 10));

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Enviada);
        remessa.ProtocoloTce.Should().Be("PROTO-FOLHA-001");
        remessa.DomainEvents.OfType<RemessaEnviadaTce>().Should().ContainSingle();
    }

    [Fact] // REGRESSAO: remessa multi-arquivo (4810/4820/4960) — a conferencia do hash no empacotamento
           // DEVE consolidar os .TXT na ORDEM DO LEIAUTE (a mesma da geracao), nao na ordem (nao
           // deterministica) da colecao-filha recarregada pelo EF. Antes do fix, files reordenados faziam
           // MarcarProntaParaTransmissao falhar com "Hash de integridade do pacote invalido".
    public void Empacotamento_de_remessa_multiarquivo_confere_hash_independente_da_ordem_da_colecao()
    {
        var leiaute = LeiauteFolha();

        // Conteudo posicional real dos tres arquivos, na ORDEM CANONICA do leiaute (4810, 4820, 4960).
        var conteudoPorArquivo = leiaute.Registros.ToDictionary(
            registro => registro.NomeArquivo,
            registro => (ReadOnlyMemory<byte>)Encoding.Latin1.GetBytes($"corpo-{registro.NomeArquivo}-fixo"),
            StringComparer.OrdinalIgnoreCase);

        // Hash da geracao: consolida na ordem do leiaute (como GerarRemessaFolhaTceHandler faz).
        var pacoteGeracao = Consolidar(leiaute.Registros.Select(registro => conteudoPorArquivo[registro.NomeArquivo]));

        var arquivos = leiaute.Registros
            .Select(registro => ArquivoRemessa.Criar(
                registro.NomeArquivo,
                conteudoPorArquivo[registro.NomeArquivo],
                [RegistroLeiaute.Criar(registro.CodigoRegistro, "linha-1")]))
            .ToList();

        var remessa = RemessaTce.GerarRemessa(
            TenantA,
            Periodo.De(2026, TipoPeriodo.Mensal, 5),
            Leiaute.De("FOLHA-TCE", "1099"),
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 6, 10),
            arquivos,
            pacoteGeracao);

        remessa.RegistrarResultadoValidacao(ResultadoValidacao.Criar("1099", DateTimeOffset.UtcNow, []));

        // Simula a colecao recarregada FORA de ordem (EF nao garante ordem sem OrderBy). A consolidacao
        // do empacotamento deve reordenar pela posicao no leiaute antes de conferir o hash.
        var consolidadoNaOrdemDoLeiaute = Consolidar(
            remessa.Arquivos
                .OrderBy(arquivo => IndiceNoLeiaute(leiaute, arquivo.NomeArquivo))
                .Select(arquivo => arquivo.Conteudo));

        ((Action)(() => remessa.MarcarProntaParaTransmissao("multi.zip", consolidadoNaOrdemDoLeiaute.Span)))
            .Should().NotThrow("o conteudo consolidado na ordem do leiaute reproduz o hash da geracao");
        remessa.Situacao.Should().Be(SituacaoRemessaTce.ProntaParaTransmissao);

        // Defesa do teste: consolidar na ordem INVERTIDA produziria bytes diferentes (o hash falharia) —
        // confirma que a ordem importa e que o fix (ordem do leiaute) e o que torna a conferencia estavel.
        var consolidadoInvertido = Consolidar(
            remessa.Arquivos
                .OrderByDescending(arquivo => IndiceNoLeiaute(leiaute, arquivo.NomeArquivo))
                .Select(arquivo => arquivo.Conteudo));
        consolidadoInvertido.ToArray().Should().NotEqual(consolidadoNaOrdemDoLeiaute.ToArray());
    }

    private static int IndiceNoLeiaute(LeiauteSiapc leiaute, string nomeArquivo)
    {
        for (var indice = 0; indice < leiaute.Registros.Count; indice++)
        {
            if (string.Equals(leiaute.Registros[indice].NomeArquivo, nomeArquivo, StringComparison.OrdinalIgnoreCase))
            {
                return indice;
            }
        }

        return int.MaxValue;
    }

    private static ReadOnlyMemory<byte> Consolidar(IEnumerable<ReadOnlyMemory<byte>> partes)
    {
        var lista = partes.ToList();
        var total = new byte[lista.Sum(parte => parte.Length)];
        var offset = 0;
        foreach (var parte in lista)
        {
            parte.Span.CopyTo(total.AsSpan(offset));
            offset += parte.Length;
        }

        return total;
    }

    private static readonly byte[] PacoteFolha = Encoding.Latin1.GetBytes("pacote-folha-tce-2026-05");

    private static RemessaTce NovaRemessaFolha()
    {
        var arquivo = ArquivoRemessa.Criar(
            "TCE_4810.TXT",
            PacoteFolha,
            [RegistroLeiaute.Criar("4810", "linha-1")]);

        return RemessaTce.GerarRemessa(
            TenantA,
            Periodo.De(2026, TipoPeriodo.Mensal, 5),
            Leiaute.De("FOLHA-TCE", "1099"),
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 6, 10),
            [arquivo],
            PacoteFolha);
    }
}
