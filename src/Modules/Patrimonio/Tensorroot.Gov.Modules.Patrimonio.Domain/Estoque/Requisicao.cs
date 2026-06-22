using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

/// <summary>Identificador forte da entidade <see cref="Requisicao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RequisicaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RequisicaoId"/>.</returns>
    public static RequisicaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação de uma requisição de material.</summary>
public enum SituacaoRequisicao
{
    /// <summary>Pendente de atendimento.</summary>
    Pendente = 1,

    /// <summary>Atendida (baixa de saída efetivada).</summary>
    Atendida = 2,
}

/// <summary>
/// Requisição de material: pedido de saída de itens do almoxarifado.
/// Registra o solicitante (LGPD) e a quantidade pedida.
/// </summary>
public sealed class Requisicao : Entity<RequisicaoId>
{
    private Requisicao()
    {
    }

    private Requisicao(
        RequisicaoId id,
        Guid solicitanteId,
        decimal quantidade,
        DateOnly data)
        : base(id)
    {
        SolicitanteId = solicitanteId;
        Quantidade = quantidade;
        Data = data;
        Situacao = SituacaoRequisicao.Pendente;
    }

    /// <summary>Solicitante (servidor/setor que pediu o material).</summary>
    public Guid SolicitanteId { get; private set; }

    /// <summary>Quantidade requisitada (estritamente positiva, I-11).</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Data da requisição.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Situação atual da requisição.</summary>
    public SituacaoRequisicao Situacao { get; private set; }

    /// <summary>Abre uma requisição de material com o identificador informado.</summary>
    /// <param name="id">Identificador da requisição (externo).</param>
    /// <param name="solicitanteId">Solicitante.</param>
    /// <param name="quantidade">Quantidade pedida (estritamente positiva).</param>
    /// <param name="data">Data da requisição.</param>
    /// <returns>Nova <see cref="Requisicao"/> pendente.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    public static Requisicao Abrir(RequisicaoId id, Guid solicitanteId, decimal quantidade, DateOnly data)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        return new Requisicao(id, solicitanteId, quantidade, data);
    }

    /// <summary>Marca a requisição como atendida (após a baixa de saída).</summary>
    /// <exception cref="InvalidOperationException">Se a requisição já estiver atendida.</exception>
    public void Atender()
    {
        if (Situacao == SituacaoRequisicao.Atendida)
        {
            throw new InvalidOperationException("Requisição já atendida.");
        }

        Situacao = SituacaoRequisicao.Atendida;
    }
}
