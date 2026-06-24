using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>Identificador forte da entidade filha <see cref="DesignacaoFiscal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DesignacaoFiscalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DesignacaoFiscalId"/>.</returns>
    public static DesignacaoFiscalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Designação de fiscal/gestor do contrato de obra (Lei 14.133/2021, art. 117). Histórico imutável de
/// quem fiscaliza a obra e desde quando, com o ato de designação. Entidade filha do agregado <see cref="Obra"/>.
/// </summary>
public sealed class DesignacaoFiscal : Entity<DesignacaoFiscalId>
{
    private DesignacaoFiscal()
    {
    }

    private DesignacaoFiscal(DesignacaoFiscalId id, Guid fiscalId, DateOnly desde, string atoDesignacao)
        : base(id)
    {
        FiscalId = fiscalId;
        Desde = desde;
        AtoDesignacao = atoDesignacao;
    }

    /// <summary>Servidor designado como fiscal.</summary>
    public Guid FiscalId { get; private set; }

    /// <summary>Data a partir da qual o fiscal está vigente.</summary>
    public DateOnly Desde { get; private set; }

    /// <summary>Ato administrativo de designação (portaria/ofício).</summary>
    public string AtoDesignacao { get; private set; } = default!;

    /// <summary>Cria uma designação de fiscal validando os campos.</summary>
    /// <param name="fiscalId">Servidor designado (obrigatório).</param>
    /// <param name="desde">Data de início da vigência.</param>
    /// <param name="atoDesignacao">Ato de designação (obrigatório).</param>
    /// <returns>Nova designação.</returns>
    /// <exception cref="ArgumentException">Se o ato for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o fiscal for vazio.</exception>
    public static DesignacaoFiscal Criar(Guid fiscalId, DateOnly desde, string atoDesignacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(atoDesignacao);
        if (fiscalId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fiscalId), "Fiscal é obrigatório.");
        }

        return new DesignacaoFiscal(DesignacaoFiscalId.New(), fiscalId, desde, atoDesignacao.Trim());
    }
}

/// <summary>Identificador forte da entidade filha <see cref="OcorrenciaFiscalizacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct OcorrenciaFiscalizacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="OcorrenciaFiscalizacaoId"/>.</returns>
    public static OcorrenciaFiscalizacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Ocorrência de fiscalização registrada na obra (notificação/advertência/registro técnico — art. 117).
/// Entidade filha imutável do agregado <see cref="Obra"/>.
/// </summary>
public sealed class OcorrenciaFiscalizacao : Entity<OcorrenciaFiscalizacaoId>
{
    private OcorrenciaFiscalizacao()
    {
    }

    private OcorrenciaFiscalizacao(
        OcorrenciaFiscalizacaoId id,
        DateOnly data,
        TipoOcorrenciaFiscalizacao tipo,
        string descricao,
        Guid registradaPorId)
        : base(id)
    {
        Data = data;
        Tipo = tipo;
        Descricao = descricao;
        RegistradaPorId = registradaPorId;
    }

    /// <summary>Data da ocorrência.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Tipo da ocorrência.</summary>
    public TipoOcorrenciaFiscalizacao Tipo { get; private set; }

    /// <summary>Descrição do fato registrado.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Servidor que registrou a ocorrência.</summary>
    public Guid RegistradaPorId { get; private set; }

    /// <summary>Cria uma ocorrência de fiscalização validando os campos.</summary>
    /// <param name="data">Data da ocorrência.</param>
    /// <param name="tipo">Tipo da ocorrência.</param>
    /// <param name="descricao">Descrição (obrigatória).</param>
    /// <param name="registradaPorId">Servidor que registra (obrigatório).</param>
    /// <returns>Nova ocorrência.</returns>
    /// <exception cref="ArgumentException">Se a descrição for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o registrador for vazio.</exception>
    public static OcorrenciaFiscalizacao Criar(
        DateOnly data,
        TipoOcorrenciaFiscalizacao tipo,
        string descricao,
        Guid registradaPorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        if (registradaPorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(registradaPorId), "Registrador é obrigatório.");
        }

        return new OcorrenciaFiscalizacao(OcorrenciaFiscalizacaoId.New(), data, tipo, descricao.Trim(), registradaPorId);
    }
}

/// <summary>Identificador forte da entidade filha <see cref="EventoParalisacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EventoParalisacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EventoParalisacaoId"/>.</returns>
    public static EventoParalisacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Evento de paralisação/reinício da obra (entidade filha do agregado <see cref="Obra"/>). Registra o
/// motivo e a data da paralisação e, quando aplicável, a data de reinício. O período de paralisação
/// suspende a contagem do relógio do art. 94 §3 quando o tenant assim parametrizar (I-14/I-15).
/// </summary>
public sealed class EventoParalisacao : Entity<EventoParalisacaoId>
{
    private EventoParalisacao()
    {
    }

    private EventoParalisacao(EventoParalisacaoId id, MotivoParalisacao motivo, DateOnly dataParalisacao)
        : base(id)
    {
        Motivo = motivo;
        DataParalisacao = dataParalisacao;
    }

    /// <summary>Motivo da paralisação.</summary>
    public MotivoParalisacao Motivo { get; private set; }

    /// <summary>Data da paralisação.</summary>
    public DateOnly DataParalisacao { get; private set; }

    /// <summary>Data do reinício (nula enquanto a obra permanecer paralisada).</summary>
    public DateOnly? DataReinicio { get; private set; }

    /// <summary>Indica se a paralisação está em aberto (sem reinício registrado).</summary>
    public bool EmAberto => DataReinicio is null;

    /// <summary>Cria um evento de paralisação em aberto.</summary>
    /// <param name="motivo">Motivo da paralisação.</param>
    /// <param name="dataParalisacao">Data da paralisação.</param>
    /// <returns>Novo evento de paralisação em aberto.</returns>
    public static EventoParalisacao Abrir(MotivoParalisacao motivo, DateOnly dataParalisacao)
        => new(EventoParalisacaoId.New(), motivo, dataParalisacao);

    /// <summary>Registra o reinício da obra, encerrando este evento de paralisação.</summary>
    /// <param name="dataReinicio">Data do reinício (&gt;= data da paralisação).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o reinício anteceder a paralisação.</exception>
    /// <exception cref="InvalidOperationException">Se o evento já estiver encerrado.</exception>
    internal void Reiniciar(DateOnly dataReinicio)
    {
        if (!EmAberto)
        {
            throw new InvalidOperationException("Paralisação já reiniciada.");
        }

        if (dataReinicio < DataParalisacao)
        {
            throw new ArgumentOutOfRangeException(nameof(dataReinicio), "Reinício não pode anteceder a paralisação.");
        }

        DataReinicio = dataReinicio;
    }
}
