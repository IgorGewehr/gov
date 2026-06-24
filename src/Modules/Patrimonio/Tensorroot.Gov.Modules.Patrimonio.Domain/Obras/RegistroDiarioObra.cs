using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>Identificador forte da entidade filha <see cref="RegistroDiarioObra"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroDiarioObraId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroDiarioObraId"/>.</returns>
    public static RegistroDiarioObraId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Relatório Diário de Obra (RDO) — item de fiscalização contínua (entidade filha do agregado
/// <see cref="Obra"/>). Único por dia/obra (I-9). Os RDOs do período embasam a medição (I-8): só
/// medição com RDOs cobrindo o período pode ser aprovada. Nasce válido.
/// </summary>
public sealed class RegistroDiarioObra : Entity<RegistroDiarioObraId>
{
    private RegistroDiarioObra()
    {
    }

    private RegistroDiarioObra(
        RegistroDiarioObraId id,
        DateOnly data,
        string condicaoTempo,
        int efetivoMaoDeObra,
        string equipamentosMobilizados,
        string atividadesExecutadas,
        string? ocorrencias,
        Guid responsavelTecnicoId)
        : base(id)
    {
        Data = data;
        CondicaoTempo = condicaoTempo;
        EfetivoMaoDeObra = efetivoMaoDeObra;
        EquipamentosMobilizados = equipamentosMobilizados;
        AtividadesExecutadas = atividadesExecutadas;
        Ocorrencias = ocorrencias;
        ResponsavelTecnicoId = responsavelTecnicoId;
    }

    /// <summary>Data do registro (única por obra — I-9).</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Condição de tempo/clima do dia (ex.: "Bom", "Chuvoso").</summary>
    public string CondicaoTempo { get; private set; } = default!;

    /// <summary>Efetivo de mão de obra no dia.</summary>
    public int EfetivoMaoDeObra { get; private set; }

    /// <summary>Equipamentos mobilizados no dia.</summary>
    public string EquipamentosMobilizados { get; private set; } = default!;

    /// <summary>Atividades executadas no dia.</summary>
    public string AtividadesExecutadas { get; private set; } = default!;

    /// <summary>Ocorrências do dia (opcional).</summary>
    public string? Ocorrencias { get; private set; }

    /// <summary>Responsável técnico que registrou o RDO.</summary>
    public Guid ResponsavelTecnicoId { get; private set; }

    /// <summary>Cria um RDO validando os campos obrigatórios.</summary>
    /// <param name="data">Data do registro.</param>
    /// <param name="condicaoTempo">Condição de tempo (obrigatória).</param>
    /// <param name="efetivoMaoDeObra">Efetivo de mão de obra (&gt;= 0).</param>
    /// <param name="equipamentosMobilizados">Equipamentos mobilizados (obrigatório).</param>
    /// <param name="atividadesExecutadas">Atividades executadas (obrigatório).</param>
    /// <param name="responsavelTecnicoId">Responsável técnico (obrigatório).</param>
    /// <param name="ocorrencias">Ocorrências (opcional).</param>
    /// <returns>Novo RDO.</returns>
    /// <exception cref="ArgumentException">Se algum campo obrigatório for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o efetivo for negativo ou o responsável vazio.</exception>
    public static RegistroDiarioObra Criar(
        DateOnly data,
        string condicaoTempo,
        int efetivoMaoDeObra,
        string equipamentosMobilizados,
        string atividadesExecutadas,
        Guid responsavelTecnicoId,
        string? ocorrencias = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(condicaoTempo);
        ArgumentException.ThrowIfNullOrWhiteSpace(equipamentosMobilizados);
        ArgumentException.ThrowIfNullOrWhiteSpace(atividadesExecutadas);
        ArgumentOutOfRangeException.ThrowIfNegative(efetivoMaoDeObra);
        if (responsavelTecnicoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(responsavelTecnicoId), "Responsável técnico é obrigatório.");
        }

        return new RegistroDiarioObra(
            RegistroDiarioObraId.New(),
            data,
            condicaoTempo.Trim(),
            efetivoMaoDeObra,
            equipamentosMobilizados.Trim(),
            atividadesExecutadas.Trim(),
            string.IsNullOrWhiteSpace(ocorrencias) ? null : ocorrencias.Trim(),
            responsavelTecnicoId);
    }
}
