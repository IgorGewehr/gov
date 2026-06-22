using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>Identificador forte de um <see cref="ResultadoValidacao"/> (RDI).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ResultadoValidacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ResultadoValidacaoId"/>.</returns>
    public static ResultadoValidacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="OcorrenciaValidacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct OcorrenciaValidacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="OcorrenciaValidacaoId"/>.</returns>
    public static OcorrenciaValidacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Ocorrência (erro/aviso) apontada pelo e-Validador em um arquivo/linha (entidade-filha do RDI).</summary>
public sealed class OcorrenciaValidacao : Entity<OcorrenciaValidacaoId>
{
    private OcorrenciaValidacao()
    {
    }

    private OcorrenciaValidacao(
        OcorrenciaValidacaoId id,
        string arquivo,
        int linha,
        SeveridadeOcorrencia severidade,
        string mensagem)
        : base(id)
    {
        Arquivo = arquivo;
        Linha = linha;
        Severidade = severidade;
        Mensagem = mensagem;
    }

    /// <summary>Arquivo onde ocorreu a apontamento.</summary>
    public string Arquivo { get; private set; } = default!;

    /// <summary>Linha do registro (0 quando não aplicável).</summary>
    public int Linha { get; private set; }

    /// <summary>Severidade da ocorrência.</summary>
    public SeveridadeOcorrencia Severidade { get; private set; }

    /// <summary>Mensagem descritiva da ocorrência.</summary>
    public string Mensagem { get; private set; } = default!;

    /// <summary>Cria uma ocorrência de validação.</summary>
    /// <param name="arquivo">Arquivo apontado (não vazio).</param>
    /// <param name="linha">Linha do registro (>= 0).</param>
    /// <param name="severidade">Severidade.</param>
    /// <param name="mensagem">Mensagem (não vazia).</param>
    /// <returns>Nova <see cref="OcorrenciaValidacao"/>.</returns>
    /// <exception cref="ArgumentException">Se arquivo ou mensagem forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a linha for negativa.</exception>
    public static OcorrenciaValidacao Criar(string arquivo, int linha, SeveridadeOcorrencia severidade, string mensagem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(arquivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensagem);
        ArgumentOutOfRangeException.ThrowIfNegative(linha);
        return new OcorrenciaValidacao(OcorrenciaValidacaoId.New(), arquivo, linha, severidade, mensagem);
    }
}

/// <summary>
/// Resultado do e-Validador (o RDI — Relatório de Dados e Informações) com erros/avisos por
/// arquivo/registro (entidade-filha). Avisos não bloqueiam; ao menos um erro torna a remessa rejeitável.
/// </summary>
public sealed class ResultadoValidacao : Entity<ResultadoValidacaoId>
{
    private readonly List<OcorrenciaValidacao> _ocorrencias = [];

    private ResultadoValidacao()
    {
    }

    private ResultadoValidacao(
        ResultadoValidacaoId id,
        bool possuiErro,
        int quantidadeErros,
        int quantidadeAvisos,
        DateTimeOffset validadoEm,
        string leiauteVersao)
        : base(id)
    {
        PossuiErro = possuiErro;
        QuantidadeErros = quantidadeErros;
        QuantidadeAvisos = quantidadeAvisos;
        ValidadoEm = validadoEm;
        LeiauteVersao = leiauteVersao;
    }

    /// <summary>Indica se o RDI apontou ao menos um erro (bloqueia o envio).</summary>
    public bool PossuiErro { get; private set; }

    /// <summary>Quantidade de erros apurados.</summary>
    public int QuantidadeErros { get; private set; }

    /// <summary>Quantidade de avisos apurados (não bloqueiam).</summary>
    public int QuantidadeAvisos { get; private set; }

    /// <summary>Momento da validação local.</summary>
    public DateTimeOffset ValidadoEm { get; private set; }

    /// <summary>Versão do leiaute usada na validação.</summary>
    public string LeiauteVersao { get; private set; } = default!;

    /// <summary>Ocorrências apontadas (linha, arquivo, severidade, mensagem).</summary>
    public IReadOnlyCollection<OcorrenciaValidacao> Ocorrencias => _ocorrencias;

    /// <summary>Cria um RDI a partir das ocorrências apuradas pelo e-Validador.</summary>
    /// <param name="leiauteVersao">Versão do leiaute usada na validação (não vazia).</param>
    /// <param name="validadoEm">Momento da validação.</param>
    /// <param name="ocorrencias">Ocorrências apuradas (erros/avisos).</param>
    /// <returns>Novo <see cref="ResultadoValidacao"/> (RDI).</returns>
    /// <exception cref="ArgumentException">Se a versão do leiaute for vazia.</exception>
    public static ResultadoValidacao Criar(
        string leiauteVersao,
        DateTimeOffset validadoEm,
        IEnumerable<OcorrenciaValidacao> ocorrencias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leiauteVersao);
        ArgumentNullException.ThrowIfNull(ocorrencias);

        var lista = ocorrencias.ToList();
        var erros = lista.Count(o => o.Severidade == SeveridadeOcorrencia.Erro);
        var avisos = lista.Count(o => o.Severidade == SeveridadeOcorrencia.Aviso);

        var rdi = new ResultadoValidacao(
            ResultadoValidacaoId.New(),
            erros > 0,
            erros,
            avisos,
            validadoEm,
            leiauteVersao);
        rdi._ocorrencias.AddRange(lista);
        return rdi;
    }
}
