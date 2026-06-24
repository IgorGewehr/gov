namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;

/// <summary>Identificador forte da entidade-filha <see cref="DespesaPessoalMensalSnapshot"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DespesaPessoalMensalSnapshotId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DespesaPessoalMensalSnapshotId"/>.</returns>
    public static DespesaPessoalMensalSnapshotId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha materializada da Despesa com Pessoal (base LRF) de UMA competência mensal <c>(Ano, Mes)</c>,
/// alimentada por <c>DespesaPessoalApuradaIntegrationEvent</c> (RH). É a granularidade que permite compor
/// a <b>Despesa Total com Pessoal por janela móvel de 12 meses</b> (LC 101/2000 art. 18 §2º — mês de
/// referência + 11 meses anteriores), em vez do acumulado do exercício (que zera em janeiro e produz
/// indicador falso-verde no início do ano). Owned do <see cref="IndicadorMunicipioSnapshot"/> do exercício
/// igual a <see cref="Ano"/>; o <see cref="Valor"/> é ACUMULADOR da competência (somam-se as folhas do
/// mês — mensal, 13º, férias, rescisão — pois todas compõem a base de pessoal daquele mês).
/// </summary>
public sealed class DespesaPessoalMensalSnapshot
{
    private DespesaPessoalMensalSnapshot()
    {
    }

    private DespesaPessoalMensalSnapshot(DespesaPessoalMensalSnapshotId id, int ano, int mes, decimal valor)
    {
        Id = id;
        Ano = ano;
        Mes = mes;
        Valor = valor;
    }

    /// <summary>Identidade da linha.</summary>
    public DespesaPessoalMensalSnapshotId Id { get; private init; }

    /// <summary>Ano (exercício) da competência.</summary>
    public int Ano { get; private set; }

    /// <summary>Mês da competência (1-12).</summary>
    public int Mes { get; private set; }

    /// <summary>Despesa com pessoal (base LRF) acumulada da competência (&gt;= 0).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Cria a linha de uma competência com o valor inicial.</summary>
    /// <param name="ano">Ano (exercício) da competência.</param>
    /// <param name="mes">Mês da competência (1-12).</param>
    /// <param name="valor">Despesa de pessoal inicial da competência (&gt;= 0).</param>
    /// <returns>Nova linha mensal.</returns>
    public static DespesaPessoalMensalSnapshot Criar(int ano, int mes, decimal valor)
    {
        if (ano < 1988)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), "Ano inválido.");
        }

        if (mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mes), "Mês deve estar entre 1 e 12.");
        }

        if (valor < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor não pode ser negativo.");
        }

        return new DespesaPessoalMensalSnapshot(DespesaPessoalMensalSnapshotId.New(), ano, mes, valor);
    }

    /// <summary>Acumula mais despesa de pessoal na mesma competência (ex.: folha de 13º após a mensal).</summary>
    /// <param name="valor">Valor adicional (&gt;= 0).</param>
    internal void Acumular(decimal valor)
    {
        if (valor < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor não pode ser negativo.");
        }

        Valor += valor;
    }
}
