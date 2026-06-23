using System.Globalization;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

/// <summary>
/// Protocolo do pedido e-SIC, unico por (tenant, ano): formato <c>AAAA/NNNNNN</c> (ano + sequencial
/// zero-padded). Gerado a partir do sequencial do tenant no ano — nunca digitado pelo cidadao.
/// </summary>
public sealed class ProtocoloSic : ValueObject
{
    private ProtocoloSic(int ano, int sequencial, string valor)
    {
        Ano = ano;
        Sequencial = sequencial;
        Valor = valor;
    }

    /// <summary>Ano do protocolo.</summary>
    public int Ano { get; }

    /// <summary>Sequencial dentro do ano/tenant.</summary>
    public int Sequencial { get; }

    /// <summary>Representacao textual <c>AAAA/NNNNNN</c>.</summary>
    public string Valor { get; }

    /// <summary>Gera um protocolo a partir do ano e do sequencial do tenant.</summary>
    /// <param name="ano">Ano de abertura.</param>
    /// <param name="sequencial">Sequencial (&gt;= 1) do tenant no ano.</param>
    /// <returns>Novo <see cref="ProtocoloSic"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o sequencial for menor que 1.</exception>
    public static ProtocoloSic Gerar(int ano, int sequencial)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sequencial, 1);
        var valor = string.Create(CultureInfo.InvariantCulture, $"{ano:0000}/{sequencial:000000}");
        return new ProtocoloSic(ano, sequencial, valor);
    }

    /// <inheritdoc />
    public override string ToString() => Valor;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Ano;
        yield return Sequencial;
    }
}

/// <summary>
/// Solicitante do pedido e-SIC. Dados pessoais do REQUERENTE: protegidos por LGPD e pela propria LAI
/// (art. 10 §3o veda exigir motivacao; o requerente nao pode ser exposto a terceiros). O
/// <see cref="Documento"/> e o <see cref="Contato"/> sao marcados <see cref="CampoSensivelLgpdAttribute"/>
/// (redacao na trilha) e SO aparecem na superficie INTERNA autenticada — nunca na consulta publica do
/// protocolo. A identificacao minima e exigida pelo Dec. 7.724/2012 art. 12.
/// </summary>
public sealed class Solicitante : ValueObject
{
    private Solicitante(string nome, string? documento, string? contato, bool anonimo)
    {
        Nome = nome;
        Documento = documento;
        Contato = contato;
        Anonimo = anonimo;
    }

    /// <summary>Nome informado pelo solicitante.</summary>
    [CampoSensivelLgpd]
    public string Nome { get; } = default!;

    /// <summary>Documento (CPF/CNPJ) do solicitante, sem mascara — dado pessoal sensivel.</summary>
    [CampoSensivelLgpd]
    public string? Documento { get; }

    /// <summary>E-mail/telefone de contato do solicitante — dado pessoal sensivel.</summary>
    [CampoSensivelLgpd]
    public string? Contato { get; }

    /// <summary>Indica pedido sem identificacao plena (o ente pode exigir cadastro conforme regulamento).</summary>
    public bool Anonimo { get; }

    /// <summary>Cria um solicitante identificado.</summary>
    /// <param name="nome">Nome (obrigatorio).</param>
    /// <param name="documento">Documento (opcional).</param>
    /// <param name="contato">Contato para a resposta (opcional, mas necessario para resposta eletronica).</param>
    /// <returns>Novo <see cref="Solicitante"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    public static Solicitante Criar(string nome, string? documento, string? contato)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return new Solicitante(
            nome.Trim(),
            string.IsNullOrWhiteSpace(documento) ? null : documento.Trim(),
            string.IsNullOrWhiteSpace(contato) ? null : contato.Trim(),
            anonimo: false);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Nome;
        yield return Documento;
        yield return Contato;
        yield return Anonimo;
    }
}

/// <summary>Resposta do orgao ao pedido (texto + referencia opcional a anexo). LAI art. 11.</summary>
public sealed class RespostaSic : ValueObject
{
    private RespostaSic(string texto, string? referenciaAnexo, DateOnly data)
    {
        Texto = texto;
        ReferenciaAnexo = referenciaAnexo;
        Data = data;
    }

    /// <summary>Texto da resposta.</summary>
    public string Texto { get; } = default!;

    /// <summary>Referencia opcional a anexo (id de documento no GED/Protocolo).</summary>
    public string? ReferenciaAnexo { get; }

    /// <summary>Data da resposta.</summary>
    public DateOnly Data { get; }

    /// <summary>Cria uma resposta.</summary>
    /// <param name="texto">Texto (obrigatorio).</param>
    /// <param name="data">Data da resposta.</param>
    /// <param name="referenciaAnexo">Referencia a anexo (opcional).</param>
    /// <returns>Nova <see cref="RespostaSic"/>.</returns>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    public static RespostaSic Criar(string texto, DateOnly data, string? referenciaAnexo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        return new RespostaSic(texto.Trim(), string.IsNullOrWhiteSpace(referenciaAnexo) ? null : referenciaAnexo.Trim(), data);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Texto;
        yield return ReferenciaAnexo;
        yield return Data;
    }
}

/// <summary>Recurso administrativo do cidadao contra a resposta/indeferimento (LAI art. 15-16).</summary>
public sealed class RecursoSic : ValueObject
{
    private RecursoSic(
        InstanciaRecurso instancia,
        string fundamento,
        DateOnly dataInterposicao,
        ResultadoRecurso? resultado,
        string? decisao,
        DateOnly? dataDecisao)
    {
        Instancia = instancia;
        Fundamento = fundamento;
        DataInterposicao = dataInterposicao;
        Resultado = resultado;
        Decisao = decisao;
        DataDecisao = dataDecisao;
    }

    /// <summary>Instancia do recurso.</summary>
    public InstanciaRecurso Instancia { get; }

    /// <summary>Fundamento/razao do recurso (informado pelo cidadao).</summary>
    public string Fundamento { get; } = default!;

    /// <summary>Data de interposicao.</summary>
    public DateOnly DataInterposicao { get; }

    /// <summary>Resultado da decisao (nulo enquanto pendente).</summary>
    public ResultadoRecurso? Resultado { get; }

    /// <summary>Texto da decisao do recurso (nulo enquanto pendente).</summary>
    public string? Decisao { get; }

    /// <summary>Data da decisao (nula enquanto pendente).</summary>
    public DateOnly? DataDecisao { get; }

    /// <summary>Interpoe um recurso (sem decisao ainda).</summary>
    /// <param name="instancia">Instancia recorrida.</param>
    /// <param name="fundamento">Fundamento do recurso (obrigatorio).</param>
    /// <param name="dataInterposicao">Data de interposicao.</param>
    /// <returns>Novo <see cref="RecursoSic"/> pendente de decisao.</returns>
    /// <exception cref="ArgumentException">Se o fundamento for vazio.</exception>
    public static RecursoSic Interpor(InstanciaRecurso instancia, string fundamento, DateOnly dataInterposicao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamento);
        return new RecursoSic(instancia, fundamento.Trim(), dataInterposicao, resultado: null, decisao: null, dataDecisao: null);
    }

    /// <summary>Decide o recurso (preserva interposicao).</summary>
    /// <param name="resultado">Resultado.</param>
    /// <param name="decisao">Texto da decisao (obrigatorio).</param>
    /// <param name="dataDecisao">Data da decisao.</param>
    /// <returns>Novo <see cref="RecursoSic"/> decidido.</returns>
    /// <exception cref="ArgumentException">Se a decisao for vazia.</exception>
    public RecursoSic Decidir(ResultadoRecurso resultado, string decisao, DateOnly dataDecisao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisao);
        return new RecursoSic(Instancia, Fundamento, DataInterposicao, resultado, decisao.Trim(), dataDecisao);
    }

    /// <summary>Indica se o recurso ja foi decidido.</summary>
    public bool Decidido => Resultado is not null;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Instancia;
        yield return Fundamento;
        yield return DataInterposicao;
        yield return Resultado;
        yield return Decisao;
        yield return DataDecisao;
    }
}
