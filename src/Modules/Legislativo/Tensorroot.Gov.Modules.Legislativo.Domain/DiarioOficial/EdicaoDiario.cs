using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

/// <summary>Identificador forte do agregado <see cref="EdicaoDiario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EdicaoDiarioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EdicaoDiarioId"/>.</returns>
    public static EdicaoDiarioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Edicao do Diario Oficial Eletronico da Camara: agrupa materias (normas, atas, editais, portarias,
/// extratos) e tem a publicacao como marco legal de eficacia dos atos. Nasce em rascunho, recebe
/// materias, e publicada com data/numero imutaveis e fica disponivel para consulta publica (LAI).
/// Edicao publicada e imutavel; correcao so via nova edicao de retificacao (D-1..D-6).
/// </summary>
public sealed class EdicaoDiario : AggregateRoot<EdicaoDiarioId>, IMustHaveTenant
{
    private readonly List<MateriaDiario> _materias = [];

    private EdicaoDiario()
    {
    }

    private EdicaoDiario(EdicaoDiarioId id, Guid tenantId, int numero, int ano, EdicaoDiarioId? edicaoOriginalId)
        : base(id)
    {
        TenantId = tenantId;
        Numero = numero;
        Ano = ano;
        EdicaoOriginalId = edicaoOriginalId;
        Situacao = SituacaoEdicao.Rascunho;
    }

    /// <summary>Tenant (Camara Municipal) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Numero sequencial da edicao por (tenant, ano).</summary>
    public int Numero { get; private set; }

    /// <summary>Ano da edicao.</summary>
    public int Ano { get; private set; }

    /// <summary>Situacao da edicao (rascunho/publicada).</summary>
    public SituacaoEdicao Situacao { get; private set; }

    /// <summary>Momento oficial da publicacao (imutavel apos publicar).</summary>
    public DateTimeOffset? DataPublicacao { get; private set; }

    /// <summary>Hash de integridade do conteudo/PDF da edicao (opcional).</summary>
    public string? HashConteudo { get; private set; }

    /// <summary>Edicao original retificada por esta (quando esta e uma retificacao).</summary>
    public EdicaoDiarioId? EdicaoOriginalId { get; private set; }

    /// <summary>Materias publicadas (ordenadas).</summary>
    public IReadOnlyList<MateriaDiario> Materias => _materias;

    /// <summary>Indica se a edicao ja foi publicada (imutavel).</summary>
    public bool Publicada => Situacao == SituacaoEdicao.Publicada;

    /// <summary>Abre uma nova edicao em <see cref="SituacaoEdicao.Rascunho"/> sem data — D-1.</summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="numero">Numero sequencial (positivo; unicidade garantida no handler).</param>
    /// <param name="ano">Ano da edicao (plausivel).</param>
    /// <returns>Nova <see cref="EdicaoDiario"/> em rascunho.</returns>
    /// <exception cref="ArgumentException">Se numero ou ano forem invalidos.</exception>
    public static EdicaoDiario Abrir(Guid tenantId, int numero, int ano)
    {
        ValidarNumeroAno(numero, ano);
        return new EdicaoDiario(EdicaoDiarioId.New(), tenantId, numero, ano, edicaoOriginalId: null);
    }

    /// <summary>
    /// Abre uma edicao de retificacao que referencia a edicao original (D-4: a original nao e mutada).
    /// </summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="numero">Numero sequencial da nova edicao.</param>
    /// <param name="ano">Ano da nova edicao.</param>
    /// <param name="edicaoOriginalId">Edicao original a retificar.</param>
    /// <returns>Nova edicao (rascunho) que referencia a original.</returns>
    /// <exception cref="ArgumentException">Se numero ou ano forem invalidos.</exception>
    public static EdicaoDiario Retificar(Guid tenantId, int numero, int ano, EdicaoDiarioId edicaoOriginalId)
    {
        ValidarNumeroAno(numero, ano);
        return new EdicaoDiario(EdicaoDiarioId.New(), tenantId, numero, ano, edicaoOriginalId);
    }

    /// <summary>Adiciona uma materia (so com edicao em rascunho); atribui ordem sequencial — D-2, D-4.</summary>
    /// <param name="tipo">Especie da materia.</param>
    /// <param name="titulo">Titulo (obrigatorio).</param>
    /// <param name="conteudo">Conteudo bruto (opcional).</param>
    /// <param name="referenciaId">Referencia a entidade de origem (opcional).</param>
    /// <returns>Identificador da materia adicionada.</returns>
    /// <exception cref="InvalidOperationException">Se a edicao ja estiver publicada.</exception>
    public MateriaDiarioId AdicionarMateria(TipoMateria tipo, string titulo, string? conteudo, Guid? referenciaId)
    {
        if (Publicada)
        {
            throw new InvalidOperationException("Edicao publicada e imutavel: nao admite novas materias.");
        }

        var ordem = _materias.Count + 1;
        var materia = MateriaDiario.Criar(tipo, titulo, conteudo, referenciaId, ordem);
        _materias.Add(materia);
        return materia.Id;
    }

    /// <summary>
    /// Publica a edicao (a partir de <see cref="SituacaoEdicao.Rascunho"/>, exige >= 1 materia); define
    /// data/hash, passa a <see cref="SituacaoEdicao.Publicada"/> e emite <see cref="DiarioPublicado"/>.
    /// Idempotente: republicar e no-op — D-3, D-6.
    /// </summary>
    /// <param name="dataPublicacao">Momento oficial da publicacao (nao futuro).</param>
    /// <param name="agora">Relogio do servidor (para validar a nao-futuridade).</param>
    /// <param name="hashConteudo">Hash de integridade (opcional).</param>
    /// <exception cref="InvalidOperationException">Se nao houver materias.</exception>
    /// <exception cref="ArgumentException">Se a data de publicacao for futura.</exception>
    public void Publicar(DateTimeOffset dataPublicacao, DateTimeOffset agora, string? hashConteudo = null)
    {
        if (Publicada)
        {
            return; // D-3: idempotente.
        }

        if (_materias.Count == 0)
        {
            throw new InvalidOperationException("Edicao sem materias nao pode ser publicada.");
        }

        if (dataPublicacao > agora)
        {
            throw new ArgumentException("Data de publicacao nao pode ser futura.", nameof(dataPublicacao));
        }

        Situacao = SituacaoEdicao.Publicada;
        DataPublicacao = dataPublicacao;
        HashConteudo = string.IsNullOrWhiteSpace(hashConteudo) ? null : hashConteudo.Trim();
        RaiseDomainEvent(new DiarioPublicado(Id, Numero, Ano, dataPublicacao));
    }

    private static void ValidarNumeroAno(int numero, int ano)
    {
        const int AnoMinimo = 1900;
        const int AnoMaximo = 2100;
        if (numero <= 0)
        {
            throw new ArgumentException("Numero da edicao deve ser positivo.", nameof(numero));
        }

        if (ano is < AnoMinimo or > AnoMaximo)
        {
            throw new ArgumentException("Ano da edicao implausivel.", nameof(ano));
        }
    }
}
