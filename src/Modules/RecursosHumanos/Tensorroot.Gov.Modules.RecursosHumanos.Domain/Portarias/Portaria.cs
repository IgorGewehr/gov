using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

/// <summary>Identificador forte do agregado <see cref="Portaria"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PortariaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PortariaId"/>.</returns>
    public static PortariaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Portaria / ato de pessoal: documento administrativo que formaliza uma decisao sobre o vinculo de um
/// servidor (nomeacao, exoneracao, designacao de funcao, concessao de licenca/beneficio). Numeracao
/// SEQUENCIAL por exercicio/tenant (<see cref="NumeroPortaria"/>), texto/ementa do ato, vinculo opcional
/// ao servidor e ciclo de vida Emitida -> Revogada. Raiz de agregado, nasce valida via <see cref="Emitir"/>.
/// </summary>
public sealed class Portaria : AggregateRoot<PortariaId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo da ementa (resumo do ato).</summary>
    public const int ComprimentoMaximoEmenta = 500;

    /// <summary>Comprimento maximo do corpo (texto integral do ato).</summary>
    public const int ComprimentoMaximoTexto = 20_000;

    private Portaria()
    {
    }

    private Portaria(
        PortariaId id,
        Guid tenantId,
        NumeroPortaria numero,
        TipoPortaria tipo,
        DateOnly dataAto,
        string ementa,
        string texto,
        ServidorId? servidorId)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = numero.Exercicio;
        Sequencial = numero.Sequencial;
        Tipo = tipo;
        DataAto = dataAto;
        Ementa = ementa;
        Texto = texto;
        ServidorId = servidorId;
        Situacao = SituacaoPortaria.Emitida;
        RaiseDomainEvent(new PortariaEmitida(id, numero, tipo, servidorId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercicio (ano civil) da numeracao oficial — persistido para indexar/ordenar.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Sequencial da numeracao oficial dentro do exercicio — persistido para indexar/ordenar.</summary>
    public int Sequencial { get; private set; }

    /// <summary>
    /// Numeracao oficial (VO de dominio) reconstruida do par <see cref="Exercicio"/>/<see cref="Sequencial"/>
    /// persistido. Propriedade calculada (sem coluna propria — EF mapeia os escalares).
    /// </summary>
    public NumeroPortaria Numero => NumeroPortaria.De(Exercicio, Sequencial);

    /// <summary>Natureza do ato de pessoal.</summary>
    public TipoPortaria Tipo { get; private set; }

    /// <summary>Data do ato (data civil do tenant, fornecida pela borda).</summary>
    public DateOnly DataAto { get; private set; }

    /// <summary>Ementa/resumo do ato.</summary>
    public string Ementa { get; private set; } = default!;

    /// <summary>Texto integral do ato.</summary>
    public string Texto { get; private set; } = default!;

    /// <summary>Servidor vinculado ao ato (opcional — atos coletivos ou gerais podem nao ter vinculo).</summary>
    public ServidorId? ServidorId { get; private set; }

    /// <summary>Situacao (estado) atual da portaria.</summary>
    public SituacaoPortaria Situacao { get; private set; }

    /// <summary>Fundamento da revogacao, quando revogada; nulo enquanto vigente.</summary>
    public string? MotivoRevogacao { get; private set; }

    /// <summary>
    /// Emite uma portaria de pessoal com a numeracao ja apurada na borda (sequencial do exercicio).
    /// Nasce em <see cref="SituacaoPortaria.Emitida"/> e emite <see cref="PortariaEmitida"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="numero">Numeracao oficial atribuida (sequencial/exercicio).</param>
    /// <param name="tipo">Natureza do ato de pessoal.</param>
    /// <param name="dataAto">Data do ato.</param>
    /// <param name="ementa">Ementa/resumo (nao vazia, ate 500 caracteres).</param>
    /// <param name="texto">Texto integral (nao vazio, ate 20.000 caracteres).</param>
    /// <param name="servidorId">Servidor vinculado (opcional).</param>
    /// <returns>Nova <see cref="Portaria"/> em situacao <see cref="SituacaoPortaria.Emitida"/>.</returns>
    /// <exception cref="ArgumentNullException">Se a numeracao for nula.</exception>
    /// <exception cref="ArgumentException">Se a ementa ou o texto forem vazios ou excederem o limite.</exception>
    public static Portaria Emitir(
        Guid tenantId,
        NumeroPortaria numero,
        TipoPortaria tipo,
        DateOnly dataAto,
        string ementa,
        string texto,
        ServidorId? servidorId = null)
    {
        ArgumentNullException.ThrowIfNull(numero);
        ArgumentException.ThrowIfNullOrWhiteSpace(ementa);
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);

        var ementaNormalizada = ementa.Trim();
        var textoNormalizado = texto.Trim();
        if (ementaNormalizada.Length > ComprimentoMaximoEmenta)
        {
            throw new ArgumentException($"Ementa excede {ComprimentoMaximoEmenta} caracteres.", nameof(ementa));
        }

        if (textoNormalizado.Length > ComprimentoMaximoTexto)
        {
            throw new ArgumentException($"Texto da portaria excede {ComprimentoMaximoTexto} caracteres.", nameof(texto));
        }

        return new Portaria(
            PortariaId.New(),
            tenantId,
            numero,
            tipo,
            dataAto,
            ementaNormalizada,
            textoNormalizado,
            servidorId);
    }

    /// <summary>
    /// Revoga a portaria (torna sem efeito). Estado terminal: a numeracao consumida e' preservada
    /// (a sequencia do exercicio NAO retrocede). Emite <see cref="PortariaRevogada"/>.
    /// </summary>
    /// <param name="motivo">Fundamento da revogacao (nao vazio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a portaria ja estiver revogada.</exception>
    public void Revogar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoPortaria.Revogada)
        {
            throw new InvalidOperationException("Portaria ja revogada; estado terminal.");
        }

        Situacao = SituacaoPortaria.Revogada;
        MotivoRevogacao = motivo.Trim();
        RaiseDomainEvent(new PortariaRevogada(Id, MotivoRevogacao));
    }
}
