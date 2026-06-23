namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

/// <summary>
/// Item de um lote: o id e o XML JA ASSINADO de um evento que sera empacotado no envio.
/// </summary>
/// <param name="EventoId">Identificador do <see cref="EventoESocial"/>.</param>
/// <param name="IdEvento">Atributo Id do evento no XML.</param>
/// <param name="XmlAssinado">XML assinado (UTF-8) do evento.</param>
public sealed record ItemLoteEvento(EventoESocialId EventoId, string IdEvento, byte[] XmlAssinado);

/// <summary>
/// LOTE de eventos eSocial para o envio (estrutura <c>envioLoteEventos</c> do schema
/// <c>EnvioLoteEventos</c> — ESOCIAL-SPEC §2.3). Garante as DUAS restricoes CONFIRMADAS do MOS
/// Desenvolvedor v1.15: no maximo <see cref="MaxEventosPorLote"/> eventos (50) E mensagem SOAP
/// &lt;= <see cref="MaxBytesPorLote"/> (5 MB, erro 612). Imutavel; o conteudo XML do envelope e
/// montado pelo empacotador na Infraestrutura (// TODO(validar-oficial: XSD do lote)).
/// </summary>
public sealed class LoteEventosESocial
{
    /// <summary>Maximo de eventos por lote (MOS v1.15 §7.5 — CONFIRMADO).</summary>
    public const int MaxEventosPorLote = 50;

    /// <summary>Tamanho maximo da mensagem do lote em bytes (5 MB; erro 612 — MOS v1.15 — CONFIRMADO).</summary>
    public const int MaxBytesPorLote = 5 * 1024 * 1024;

    private readonly List<ItemLoteEvento> _itens;

    private LoteEventosESocial(TipoInscricao tpInscEmpregador, string nrInscEmpregador, AmbienteESocial ambiente, IReadOnlyList<ItemLoteEvento> itens)
    {
        TpInscEmpregador = tpInscEmpregador;
        NrInscEmpregador = nrInscEmpregador;
        Ambiente = ambiente;
        _itens = [.. itens];
    }

    /// <summary>Tipo de inscricao do empregador/declarante (<c>ideEmpregador/tpInsc</c>).</summary>
    public TipoInscricao TpInscEmpregador { get; }

    /// <summary>Inscricao do empregador/declarante (<c>ideEmpregador/nrInsc</c>).</summary>
    public string NrInscEmpregador { get; }

    /// <summary>Ambiente do envio.</summary>
    public AmbienteESocial Ambiente { get; }

    /// <summary>Eventos assinados que compoem o lote (somente leitura).</summary>
    public IReadOnlyList<ItemLoteEvento> Itens => _itens;

    /// <summary>Soma dos bytes dos XMLs assinados do lote (proxy do tamanho da mensagem).</summary>
    public long TamanhoBytes => _itens.Sum(i => (long)i.XmlAssinado.Length);

    /// <summary>
    /// Monta UM lote respeitando os limites (50 eventos e 5 MB). Use
    /// <see cref="Particionar(TipoInscricao, string, AmbienteESocial, IReadOnlyList{ItemLoteEvento})"/>
    /// para fatiar uma fila grande em multiplos lotes validos.
    /// </summary>
    /// <param name="tpInscEmpregador">Tipo de inscricao do empregador.</param>
    /// <param name="nrInscEmpregador">Inscricao do empregador (nao vazia).</param>
    /// <param name="ambiente">Ambiente do envio.</param>
    /// <param name="itens">Eventos assinados (1..50, soma &lt;= 5 MB).</param>
    /// <returns>Lote valido.</returns>
    /// <exception cref="ArgumentException">Se a inscricao for vazia ou nao houver itens.</exception>
    /// <exception cref="InvalidOperationException">Se exceder 50 eventos ou 5 MB.</exception>
    public static LoteEventosESocial Montar(
        TipoInscricao tpInscEmpregador,
        string nrInscEmpregador,
        AmbienteESocial ambiente,
        IReadOnlyList<ItemLoteEvento> itens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nrInscEmpregador);
        ArgumentNullException.ThrowIfNull(itens);
        if (itens.Count == 0)
        {
            throw new ArgumentException("Um lote deve conter ao menos um evento.", nameof(itens));
        }

        // CONFIRMADO (MOS v1.15): as duas restricoes coexistem; o lote barra ANTES de gastar cota.
        if (itens.Count > MaxEventosPorLote)
        {
            throw new InvalidOperationException(
                $"Lote excede o maximo de {MaxEventosPorLote} eventos ({itens.Count}).");
        }

        var tamanho = itens.Sum(i => (long)(i.XmlAssinado?.Length ?? 0));
        if (tamanho > MaxBytesPorLote)
        {
            throw new InvalidOperationException(
                $"Lote excede o tamanho maximo de {MaxBytesPorLote} bytes ({tamanho}). Erro 612 do eSocial.");
        }

        return new LoteEventosESocial(tpInscEmpregador, nrInscEmpregador.Trim(), ambiente, itens);
    }

    /// <summary>
    /// Particiona uma fila de eventos assinados em multiplos lotes validos, fechando cada lote ao
    /// atingir 50 eventos OU ~5 MB — o que vier primeiro (ESOCIAL-SPEC §2.3). Um evento isolado maior
    /// que 5 MB e enviado sozinho (sera barrado/erro 612 no envio — falha visivel, nao silenciosa).
    /// </summary>
    /// <param name="tpInscEmpregador">Tipo de inscricao do empregador.</param>
    /// <param name="nrInscEmpregador">Inscricao do empregador.</param>
    /// <param name="ambiente">Ambiente do envio.</param>
    /// <param name="fila">Eventos assinados a empacotar.</param>
    /// <returns>Lista de lotes validos.</returns>
    /// <exception cref="ArgumentException">Se a inscricao for vazia.</exception>
    public static IReadOnlyList<LoteEventosESocial> Particionar(
        TipoInscricao tpInscEmpregador,
        string nrInscEmpregador,
        AmbienteESocial ambiente,
        IReadOnlyList<ItemLoteEvento> fila)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nrInscEmpregador);
        ArgumentNullException.ThrowIfNull(fila);

        var lotes = new List<LoteEventosESocial>();
        var atual = new List<ItemLoteEvento>();
        long tamanhoAtual = 0;

        foreach (var item in fila)
        {
            var bytes = (long)(item.XmlAssinado?.Length ?? 0);
            var estouraTamanho = atual.Count > 0 && tamanhoAtual + bytes > MaxBytesPorLote;
            var estouraQuantidade = atual.Count >= MaxEventosPorLote;
            if (estouraTamanho || estouraQuantidade)
            {
                lotes.Add(new LoteEventosESocial(tpInscEmpregador, nrInscEmpregador.Trim(), ambiente, atual));
                atual = [];
                tamanhoAtual = 0;
            }

            atual.Add(item);
            tamanhoAtual += bytes;
        }

        if (atual.Count > 0)
        {
            lotes.Add(new LoteEventosESocial(tpInscEmpregador, nrInscEmpregador.Trim(), ambiente, atual));
        }

        return lotes;
    }
}
