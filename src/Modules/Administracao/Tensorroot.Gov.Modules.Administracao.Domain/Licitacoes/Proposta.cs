using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

/// <summary>Identificador forte de uma <see cref="Proposta"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PropostaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PropostaId"/>.</returns>
    public static PropostaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Oferta apresentada por um licitante a um lote do certame.</summary>
public sealed class Proposta : Entity<PropostaId>
{
    private Proposta()
    {
    }

    private Proposta(PropostaId id, Guid fornecedorId, LoteId loteId, ValorMonetario valor)
        : base(id)
    {
        FornecedorId = fornecedorId;
        LoteId = loteId;
        Valor = valor;
        Situacao = SituacaoProposta.Recebida;
    }

    /// <summary>Licitante/proponente (outra raiz; referencia por Id).</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Lote ao qual a proposta se refere.</summary>
    public LoteId LoteId { get; private set; }

    /// <summary>Valor ofertado.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Classificacao na disputa (nulo enquanto nao julgada).</summary>
    public int? Classificacao { get; private set; }

    /// <summary>Situacao atual da proposta.</summary>
    public SituacaoProposta Situacao { get; private set; }

    /// <summary>Registra uma nova proposta recebida.</summary>
    /// <param name="fornecedorId">Licitante proponente.</param>
    /// <param name="loteId">Lote disputado.</param>
    /// <param name="valor">Valor ofertado.</param>
    /// <returns>Nova <see cref="Proposta"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o fornecedor nao for informado.</exception>
    public static Proposta Registrar(Guid fornecedorId, LoteId loteId, ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (fornecedorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorId), "Fornecedor e obrigatorio na proposta.");
        }

        return new Proposta(PropostaId.New(), fornecedorId, loteId, valor);
    }

    /// <summary>Classifica a proposta na posicao informada.</summary>
    /// <param name="classificacao">Posicao na ordenacao (maior que zero).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a classificacao nao for positiva.</exception>
    public void Classificar(int classificacao)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(classificacao);
        Classificacao = classificacao;
        Situacao = SituacaoProposta.Classificada;
    }

    /// <summary>Desclassifica a proposta.</summary>
    public void Desclassificar()
    {
        Situacao = SituacaoProposta.Desclassificada;
    }

    /// <summary>Marca a proposta como vencedora.</summary>
    public void MarcarVencedora()
    {
        Situacao = SituacaoProposta.Vencedora;
    }
}
