using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasFolha;

/// <summary>
/// Monta a REMESSA DE FOLHA ao TCE-RS (Resolucao 1099/2018 / SIAPC Vol. V) a partir do snapshot
/// <see cref="ResumoFolhaTce"/> consumido do RH: emite os tres arquivos posicionais TCE_4810/4820/4960
/// (ISO-8859-1, largura fixa) via <see cref="EmissorRegistroSiapc"/> e cria a <see cref="RemessaTce"/>
/// (nasce <c>Gerada</c>). NAO transmite — o TCE-RS nao tem API; a transmissao e ato humano via PAD.
/// </summary>
/// <param name="Exercicio">Ano de exercicio (>= 1900).</param>
/// <param name="Mes">Mes da competencia (1..12) — folha tem periodicidade mensal.</param>
/// <param name="LeiauteVersao">Versao do leiaute de folha (ex.: "1099").</param>
public sealed record GerarRemessaFolhaTceCommand(int Exercicio, int Mes, string LeiauteVersao) : ICommand<Guid>;

/// <summary>Regras de validacao da geracao de remessa de folha.</summary>
public sealed class GerarRemessaFolhaTceValidator : AbstractValidator<GerarRemessaFolhaTceCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarRemessaFolhaTceValidator()
    {
        RuleFor(comando => comando.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(comando => comando.Mes).InclusiveBetween(1, 12);
        RuleFor(comando => comando.LeiauteVersao).NotEmpty().MaximumLength(20);
    }
}

/// <summary>Handler da geracao da remessa de folha (resumo -> 3 .TXT posicionais -> RemessaTce Gerada).</summary>
public sealed class GerarRemessaFolhaTceHandler(
    IResumoFolhaTceRepository resumos,
    IRemessaTceRepository remessas,
    ILeiauteCatalogo leiauteCatalogo,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<GerarRemessaFolhaTceCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarRemessaFolhaTceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var periodo = Periodo.De(request.Exercicio, TipoPeriodo.Mensal, request.Mes);
        var leiaute = Leiaute.De(LeiauteFolhaTceCodigo, request.LeiauteVersao);

        if (!await leiauteCatalogo.SuportaAsync(leiaute, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Leiaute/versao de folha nao suportado pelo catalogo vigente.");
        }

        var resumo = await resumos
            .ObterPorCompetenciaAsync(request.Exercicio, request.Mes, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                "Nao ha resumo de folha consolidado para a competencia (a folha do RH precisa estar fechada).");

        var leiauteSiapc = await leiauteCatalogo.ResolverAsync(leiaute, cancellationToken).ConfigureAwait(false);
        var identificacao = await leiauteCatalogo
            .ObterIdentificacaoEnteAsync(periodo, cancellationToken)
            .ConfigureAwait(false);
        var dataLimite = await leiauteCatalogo.ObterDataLimiteAsync(periodo, cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Mapeia o snapshot para as linhas posicionais de cada arquivo (TCE_4810/4820/4960).
        var linhas = MapeadorRemessaFolhaTce.Montar(resumo, leiauteSiapc);
        if (linhas.Count == 0)
        {
            throw new InvalidOperationException("O resumo de folha nao produziu linhas para a remessa.");
        }

        var arquivos = MontarArquivos(leiauteSiapc, identificacao, hoje, linhas);
        var conteudoPacote = ConsolidarConteudo(arquivos);

        var remessa = RemessaTce.GerarRemessa(
            tenant.TenantId,
            periodo,
            leiaute,
            dataLimite,
            hoje,
            arquivos.Select(MapearParaArquivoRemessa).ToList(),
            conteudoPacote);

        remessas.Adicionar(remessa);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return remessa.Id.Value;
    }

    /// <summary>Codigo do leiaute de folha ao TCE-RS (distinto do SIAPC contabil do M4).</summary>
    public const string LeiauteFolhaTceCodigo = "FOLHA-TCE";

    private static List<ArquivoMontado> MontarArquivos(
        LeiauteSiapc leiaute,
        IdentificacaoEnteRemessa identificacao,
        DateOnly dataGeracao,
        IReadOnlyList<LinhaFolhaMontada> linhas)
    {
        var arquivos = new List<ArquivoMontado>();

        foreach (var definicao in leiaute.Registros)
        {
            var doArquivo = linhas
                .Where(linha => string.Equals(linha.NomeArquivo, definicao.NomeArquivo, StringComparison.OrdinalIgnoreCase))
                .Select(linha => linha.Valores)
                .ToList();

            if (doArquivo.Count == 0)
            {
                continue;
            }

            var corpo = EmissorRegistroSiapc.SerializarCorpo(definicao, doArquivo);
            var cabecalho = MontarCabecalho(identificacao, dataGeracao);
            var finalizador = MontarFinalizador(doArquivo.Count);

            var conteudo = Concatenar(cabecalho, corpo, finalizador);
            arquivos.Add(new ArquivoMontado(definicao, doArquivo, conteudo));
        }

        if (arquivos.Count == 0)
        {
            throw new InvalidOperationException("Nenhum arquivo do leiaute de folha foi alimentado pelo resumo.");
        }

        return arquivos;
    }

    // Cabecalho (1a linha): CNPJ + Setor de Governo + datas + Codigo da Remessa. Mesmo padrao do M4.
    // TODO(validar-leiaute-folha-1099): posicoes/tamanhos exatos do cabecalho da remessa de folha.
    private static byte[] MontarCabecalho(IdentificacaoEnteRemessa identificacao, DateOnly dataGeracao)
    {
        var texto = string.Join(
            ';',
            identificacao.Cnpj,
            identificacao.NomeSetorGoverno,
            $"{identificacao.DataInicioPeriodo:ddMMyyyy}",
            $"{identificacao.DataFimPeriodo:ddMMyyyy}",
            $"{dataGeracao:ddMMyyyy}",
            identificacao.CodigoRemessa.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(12, '0'));
        return EmissorRegistroSiapc.Codificar(texto + EmissorRegistroSiapc.TerminadorLinha);
    }

    private static byte[] MontarFinalizador(int quantidadeRegistros)
    {
        var qtd = quantidadeRegistros.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(10, '0');
        return EmissorRegistroSiapc.Codificar($"FINALIZADOR{qtd}" + EmissorRegistroSiapc.TerminadorLinha);
    }

    private static ReadOnlyMemory<byte> Concatenar(byte[] cabecalho, byte[] corpo, byte[] finalizador)
    {
        var total = new byte[cabecalho.Length + corpo.Length + finalizador.Length];
        cabecalho.CopyTo(total, 0);
        corpo.CopyTo(total, cabecalho.Length);
        finalizador.CopyTo(total, cabecalho.Length + corpo.Length);
        return total;
    }

    private static ReadOnlyMemory<byte> ConsolidarConteudo(IReadOnlyList<ArquivoMontado> arquivos)
    {
        var tamanho = arquivos.Sum(arquivo => arquivo.Conteudo.Length);
        var total = new byte[tamanho];
        var offset = 0;
        foreach (var arquivo in arquivos)
        {
            arquivo.Conteudo.Span.CopyTo(total.AsSpan(offset));
            offset += arquivo.Conteudo.Length;
        }

        return total;
    }

    private static ArquivoRemessa MapearParaArquivoRemessa(ArquivoMontado arquivo)
    {
        var registros = arquivo.Linhas.Select(
            (_, indice) => RegistroLeiaute.Criar(
                arquivo.Definicao.CodigoRegistro,
                $"linha-{indice + 1}"));
        return ArquivoRemessa.Criar(arquivo.Definicao.NomeArquivo, arquivo.Conteudo, registros);
    }
}
