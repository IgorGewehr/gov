using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>Consolida os itens e monta o pacote de remessa ao TCE-RS (nasce em <c>Gerada</c>).</summary>
/// <param name="Exercicio">Ano de exercício (>= 1900).</param>
/// <param name="TipoPeriodo">Tipo do período (mensal/bimestre/quadrimestre/anual).</param>
/// <param name="NumeroPeriodo">Número da competência dentro do exercício.</param>
/// <param name="LeiauteCodigo">Código do leiaute (ex.: "SIAPC").</param>
/// <param name="LeiauteVersao">Versão do leiaute (ex.: "2026").</param>
public sealed record GerarRemessaTceCommand(
    int Exercicio,
    TipoPeriodo TipoPeriodo,
    int NumeroPeriodo,
    string LeiauteCodigo,
    string LeiauteVersao) : ICommand<Guid>;

/// <summary>Regras de validação da geração de remessa ao TCE-RS.</summary>
public sealed class GerarRemessaTceValidator : AbstractValidator<GerarRemessaTceCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarRemessaTceValidator()
    {
        RuleFor(comando => comando.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(comando => comando.TipoPeriodo).IsInEnum();
        RuleFor(comando => comando.NumeroPeriodo).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.LeiauteCodigo).NotEmpty().MaximumLength(40);
        RuleFor(comando => comando.LeiauteVersao).NotEmpty().MaximumLength(20);
    }
}

/// <summary>
/// Handler da geração de remessa ao TCE-RS: resolve a grade posicional versionada, emite os <c>.TXT</c>
/// posicionais (ISO-8859-1, cabeçalho + corpo + finalizador por arquivo) via
/// <see cref="EmissorRegistroSiapc"/>, e nomeia o pacote conforme a convenção do ZIP.
/// </summary>
public sealed class GerarRemessaTceHandler(
    IPublicacaoTransparenciaRepository publicacoes,
    IRemessaTceRepository remessas,
    ILeiauteCatalogo leiauteCatalogo,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<GerarRemessaTceCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarRemessaTceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var periodo = Periodo.De(request.Exercicio, request.TipoPeriodo, request.NumeroPeriodo);
        var leiaute = Leiaute.De(request.LeiauteCodigo, request.LeiauteVersao);

        if (!await leiauteCatalogo.SuportaAsync(leiaute, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Leiaute/versão não suportado pelo catálogo vigente.");
        }

        // Grade posicional versionada por exercício (dirigida por dados — nunca hardcoded em C#).
        var leiauteSiapc = await leiauteCatalogo.ResolverAsync(leiaute, cancellationToken).ConfigureAwait(false);

        var linhas = await publicacoes
            .ObterLinhasConsolidadasAsync(periodo, leiauteSiapc, cancellationToken)
            .ConfigureAwait(false);
        if (linhas.Count == 0)
        {
            // CB-4: sem itens consolidados no período.
            throw new InvalidOperationException("Não há itens consolidados para o período.");
        }

        var identificacao = await leiauteCatalogo
            .ObterIdentificacaoEnteAsync(periodo, cancellationToken)
            .ConfigureAwait(false);

        var dataLimite = await leiauteCatalogo.ObterDataLimiteAsync(periodo, cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

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

    /// <summary>
    /// Para cada <see cref="RegistroLeiauteDef"/> do leiaute, agrupa as linhas consolidadas, emite o corpo
    /// posicional e prefixa cabeçalho + sufixa finalizador, em ISO-8859-1.
    /// </summary>
    private static List<ArquivoMontado> MontarArquivos(
        LeiauteSiapc leiaute,
        IdentificacaoEnteRemessa identificacao,
        DateOnly dataGeracao,
        IReadOnlyList<LinhaConsolidada> linhas)
    {
        var arquivos = new List<ArquivoMontado>();

        foreach (var definicao in leiaute.Registros)
        {
            var doArquivo = linhas
                .Where(linha => string.Equals(linha.NomeArquivo, definicao.NomeArquivo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (doArquivo.Count == 0)
            {
                continue;
            }

            var valoresPorLinha = doArquivo
                .Select(linha => (IReadOnlyDictionary<string, ValorCampo>)linha.Valores.ToDictionary(valor => valor.NomeCampo))
                .ToList();

            var corpo = EmissorRegistroSiapc.SerializarCorpo(definicao, valoresPorLinha);
            var cabecalho = MontarCabecalho(identificacao, dataGeracao);
            var finalizador = MontarFinalizador(valoresPorLinha.Count);

            var conteudo = Concatenar(cabecalho, corpo, finalizador);
            arquivos.Add(new ArquivoMontado(definicao, valoresPorLinha, conteudo));
        }

        if (arquivos.Count == 0)
        {
            throw new InvalidOperationException("Nenhum arquivo do leiaute foi alimentado pelos itens consolidados.");
        }

        return arquivos;
    }

    // Cabeçalho (1ª linha): CNPJ + Setor de Governo + datas + Código da Remessa. Formato exato do MT 2026.
    // TODO(validar-leiaute-MT-2026): posições/tamanhos exatos do cabeçalho (CNPJ, datas, Código da Remessa
    // em 119-130) conforme o MT SIAPC 2026; abaixo um cabeçalho textual delimitado provisório (não oficial).
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

    // Finalizador: "FINALIZADOR" + qtd de registros (Numérico 10). CONFIANÇA: ALTA (verificação e-validador 4.2).
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
        // Consolida o conteúdo de TODOS os arquivos (ordem do leiaute) para o hash do pacote.
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
