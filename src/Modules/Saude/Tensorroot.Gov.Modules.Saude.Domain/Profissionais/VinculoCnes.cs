using Tensorroot.Gov.SharedKernel.Primitives;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Profissionais;

/// <summary>Identificador forte de um <see cref="VinculoCnes"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct VinculoCnesId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="VinculoCnesId"/>.</returns>
    public static VinculoCnesId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Vinculo de lotacao do profissional num estabelecimento (CNES) exercendo uma ocupacao (CBO), com
/// periodo de vigencia. Entidade-filha do agregado <see cref="Profissional"/>. Um vinculo esta
/// ativo numa data quando ja iniciou e ainda nao foi encerrado.
/// </summary>
public sealed class VinculoCnes : Entity<VinculoCnesId>
{
    private VinculoCnes()
    {
    }

    private VinculoCnes(VinculoCnesId id, EstabelecimentoId estabelecimentoId, Cbo cbo, DateOnly inicio)
        : base(id)
    {
        EstabelecimentoId = estabelecimentoId;
        Cbo = cbo;
        DataInicio = inicio;
        DataFim = null;
    }

    /// <summary>Estabelecimento (CNES) do vinculo (referencia por Id).</summary>
    public EstabelecimentoId EstabelecimentoId { get; private set; }

    /// <summary>Ocupacao (CBO) exercida no vinculo.</summary>
    public Cbo Cbo { get; private set; }

    /// <summary>Data de inicio do vinculo.</summary>
    public DateOnly DataInicio { get; private set; }

    /// <summary>Data de encerramento do vinculo (nula enquanto vigente).</summary>
    public DateOnly? DataFim { get; private set; }

    /// <summary>Indica se o vinculo esta encerrado.</summary>
    public bool Encerrado => DataFim is not null;

    /// <summary>Abre um novo vinculo CNES/CBO vigente a partir de <paramref name="inicio"/>.</summary>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="cbo">Ocupacao (CBO).</param>
    /// <param name="inicio">Data de inicio do vinculo.</param>
    /// <returns>Novo <see cref="VinculoCnes"/> vigente.</returns>
    /// <exception cref="ArgumentException">Se o estabelecimento for vazio.</exception>
    public static VinculoCnes Abrir(EstabelecimentoId estabelecimentoId, Cbo cbo, DateOnly inicio)
    {
        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento (CNES) e obrigatorio no vinculo.", nameof(estabelecimentoId));
        }

        return new VinculoCnes(VinculoCnesId.New(), estabelecimentoId, cbo, inicio);
    }

    /// <summary>Encerra o vinculo em <paramref name="fim"/> (nao anterior ao inicio).</summary>
    /// <param name="fim">Data de encerramento.</param>
    /// <exception cref="InvalidOperationException">Se o vinculo ja estiver encerrado.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a data de fim for anterior ao inicio.</exception>
    public void Encerrar(DateOnly fim)
    {
        if (Encerrado)
        {
            throw new InvalidOperationException("Vinculo ja encerrado.");
        }

        if (fim < DataInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fim), "Data de fim nao pode ser anterior ao inicio do vinculo.");
        }

        DataFim = fim;
    }

    /// <summary>Indica se o vinculo esta ativo na data informada (iniciado e nao encerrado).</summary>
    /// <param name="data">Data de referencia.</param>
    /// <returns><c>true</c> se vigente em <paramref name="data"/>.</returns>
    public bool VigenteEm(DateOnly data)
        => data >= DataInicio && (DataFim is null || data <= DataFim.Value);
}
