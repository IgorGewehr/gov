using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;

/// <summary>Identificador forte do agregado <see cref="Alvara"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AlvaraId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AlvaraId"/>.</returns>
    public static AlvaraId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Espécie do alvará (ato administrativo de polícia).</summary>
public enum EspecieAlvara
{
    /// <summary>Localização e funcionamento de estabelecimento.</summary>
    LocalizacaoFuncionamento = 1,

    /// <summary>Licença sanitária.</summary>
    Sanitario = 2,

    /// <summary>Licença ambiental.</summary>
    Ambiental = 3,

    /// <summary>Alvará de obras / construção.</summary>
    Obras = 4,
}

/// <summary>Situação do alvará.</summary>
public enum SituacaoAlvara
{
    /// <summary>Emitido e dentro da vigência.</summary>
    Ativo = 1,

    /// <summary>Vigência encerrada (aguardando renovação).</summary>
    Vencido = 2,

    /// <summary>Cassado/cancelado pelo município (ato de polícia).</summary>
    Cancelado = 3,
}

/// <summary>
/// Alvará: ATO ADMINISTRATIVO de polícia (licença) vinculado a um estabelecimento/contribuinte e,
/// quando aplicável, a um imóvel. O alvará NÃO é o tributo — o tributo correlato é a Taxa de Licença de
/// Localização/Funcionamento (TLL), cobrada pelo exercício do poder de polícia, gerada à parte a partir
/// da <see cref="Taxas.TabelaTaxa"/> de licença. Modela vigência e RENOVAÇÃO anual. Ver M6-DESIGN §3.3.
/// </summary>
public sealed class Alvara : AggregateRoot<AlvaraId>, IMustHaveTenant
{
    private Alvara()
    {
    }

    private Alvara(
        AlvaraId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        ImovelId? imovelId,
        EspecieAlvara especie,
        string nomeEstabelecimento,
        string atividadeCnae,
        DateOnly inicioVigencia,
        DateOnly fimVigencia)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        ImovelId = imovelId;
        Especie = especie;
        NomeEstabelecimento = nomeEstabelecimento;
        AtividadeCnae = atividadeCnae;
        InicioVigencia = inicioVigencia;
        FimVigencia = fimVigencia;
        Situacao = SituacaoAlvara.Ativo;
        RaiseDomainEvent(new AlvaraEmitido(id, tenantId, contribuinteId, especie));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte titular do estabelecimento.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Imóvel onde o estabelecimento se localiza (opcional).</summary>
    public ImovelId? ImovelId { get; private set; }

    /// <summary>Espécie do alvará.</summary>
    public EspecieAlvara Especie { get; private set; }

    /// <summary>Nome/razão social do estabelecimento.</summary>
    public string NomeEstabelecimento { get; private set; } = default!;

    /// <summary>
    /// Atividade econômica (CNAE) — elemento da TLL (não o capital, vedado pelo CTN art. 80).
    /// // TODO(validar-oficial): classes de risco/atividades conforme o CTM de Maximiliano de Almeida/RS.
    /// </summary>
    public string AtividadeCnae { get; private set; } = default!;

    /// <summary>Início da vigência.</summary>
    public DateOnly InicioVigencia { get; private set; }

    /// <summary>Fim da vigência (renovável).</summary>
    public DateOnly FimVigencia { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoAlvara Situacao { get; private set; }

    /// <summary>
    /// Emite o alvará (ato de polícia). O fato gerador tributário (TLL) é lançado à parte pela aplicação.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte titular.</param>
    /// <param name="imovelId">Imóvel do estabelecimento (opcional).</param>
    /// <param name="especie">Espécie do alvará.</param>
    /// <param name="nomeEstabelecimento">Nome/razão social.</param>
    /// <param name="atividadeCnae">Atividade (CNAE).</param>
    /// <param name="inicioVigencia">Início da vigência.</param>
    /// <param name="fimVigencia">Fim da vigência.</param>
    /// <returns>Novo <see cref="Alvara"/> ativo.</returns>
    public static Alvara Emitir(
        Guid tenantId,
        ContribuinteId contribuinteId,
        ImovelId? imovelId,
        EspecieAlvara especie,
        string nomeEstabelecimento,
        string atividadeCnae,
        DateOnly inicioVigencia,
        DateOnly fimVigencia)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeEstabelecimento);
        ArgumentException.ThrowIfNullOrWhiteSpace(atividadeCnae);
        if (!Enum.IsDefined(especie))
        {
            throw new ArgumentOutOfRangeException(nameof(especie), especie, "Espécie de alvará inválida.");
        }

        if (contribuinteId.Value == Guid.Empty)
        {
            throw new ArgumentException("O alvará deve ter um contribuinte titular.", nameof(contribuinteId));
        }

        if (fimVigencia < inicioVigencia)
        {
            throw new ArgumentException("O fim da vigência não pode ser anterior ao início.", nameof(fimVigencia));
        }

        return new Alvara(AlvaraId.New(), tenantId, contribuinteId, imovelId, especie, nomeEstabelecimento.Trim(), atividadeCnae.Trim(), inicioVigencia, fimVigencia);
    }

    /// <summary>
    /// Renova o alvará por um novo período (mantém a TLL de renovação a cargo da aplicação). Vedado em
    /// alvará cancelado.
    /// </summary>
    /// <param name="novoInicioVigencia">Início da nova vigência.</param>
    /// <param name="novoFimVigencia">Fim da nova vigência.</param>
    /// <exception cref="InvalidOperationException">Se o alvará estiver cancelado.</exception>
    public void Renovar(DateOnly novoInicioVigencia, DateOnly novoFimVigencia)
    {
        if (Situacao == SituacaoAlvara.Cancelado)
        {
            throw new InvalidOperationException("Não é possível renovar um alvará cancelado.");
        }

        if (novoFimVigencia < novoInicioVigencia)
        {
            throw new ArgumentException("O fim da vigência não pode ser anterior ao início.", nameof(novoFimVigencia));
        }

        InicioVigencia = novoInicioVigencia;
        FimVigencia = novoFimVigencia;
        Situacao = SituacaoAlvara.Ativo;
        RaiseDomainEvent(new AlvaraRenovado(Id, TenantId, novoFimVigencia));
    }

    /// <summary>Marca o alvará como vencido (vigência encerrada), se ativo e já vencido na data informada.</summary>
    /// <param name="hoje">Data de referência.</param>
    public void Vencer(DateOnly hoje)
    {
        if (Situacao == SituacaoAlvara.Ativo && hoje > FimVigencia)
        {
            Situacao = SituacaoAlvara.Vencido;
            RaiseDomainEvent(new AlvaraVencido(Id, TenantId));
        }
    }

    /// <summary>Cancela/cassa o alvará (ato de polícia).</summary>
    /// <exception cref="InvalidOperationException">Se já estiver cancelado.</exception>
    public void Cancelar()
    {
        if (Situacao == SituacaoAlvara.Cancelado)
        {
            throw new InvalidOperationException("O alvará já está cancelado.");
        }

        Situacao = SituacaoAlvara.Cancelado;
        RaiseDomainEvent(new AlvaraCancelado(Id, TenantId));
    }
}
