using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;

/// <summary>Identificador forte do agregado <see cref="PlanoCarreira"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PlanoCarreiraId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PlanoCarreiraId"/>.</returns>
    public static PlanoCarreiraId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Plano de Cargos, Carreiras e Salarios (PCCS) de uma carreira do ente publico: a MATRIZ salarial
/// (grade CLASSE x REFERENCIA) e suas regras de movimentacao. O vencimento de cada celula e' DERIVADO
/// deterministicamente do vencimento-base do plano, do percentual de cada step horizontal (entre
/// referencias) e do percentual de cada classe vertical (entre classes) — toda a aritmetica e'
/// parametrizada pela lei do plano, sem numero magico (CLAUDE.md §7). O servidor e' ENQUADRADO numa
/// posicao e movimenta-se por PROGRESSAO horizontal (referencia, por tempo/avaliacao) ou PROMOCAO
/// vertical (classe). O efeito no vencimento e' aplicado no agregado <c>Cargo</c> (cross-aggregate por
/// Id, mesmo modulo), reusando <c>Cargo.AlterarVencimento</c> — auditado pelo interceptor.
/// Raiz de agregado, nasce valida via <see cref="Instituir"/>.
/// </summary>
public sealed class PlanoCarreira : AggregateRoot<PlanoCarreiraId>, IMustHaveTenant
{
    /// <summary>Numero minimo de classes/referencias de uma grade (ao menos uma celula).</summary>
    public const int DimensaoMinima = 1;

    /// <summary>Comprimento maximo da denominacao da carreira.</summary>
    public const int ComprimentoMaximoDenominacao = 200;

    private PlanoCarreira()
    {
    }

    private PlanoCarreira(
        PlanoCarreiraId id,
        Guid tenantId,
        string denominacaoCarreira,
        string leiInstituicao,
        Vencimento vencimentoBase,
        int numeroClasses,
        int numeroReferencias,
        decimal percentualEntreReferencias,
        decimal percentualEntreClasses,
        int intersticioMeses,
        decimal notaMinimaProgressao)
        : base(id)
    {
        TenantId = tenantId;
        DenominacaoCarreira = denominacaoCarreira;
        LeiInstituicao = leiInstituicao;
        VencimentoBase = vencimentoBase;
        NumeroClasses = numeroClasses;
        NumeroReferencias = numeroReferencias;
        PercentualEntreReferencias = percentualEntreReferencias;
        PercentualEntreClasses = percentualEntreClasses;
        IntersticioMeses = intersticioMeses;
        NotaMinimaProgressao = notaMinimaProgressao;
        Situacao = SituacaoPlanoCarreira.Ativo;
        RaiseDomainEvent(new PlanoCarreiraInstituido(id, denominacaoCarreira));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Denominacao da carreira regida pelo plano.</summary>
    public string DenominacaoCarreira { get; private set; } = default!;

    /// <summary>Lei municipal que instituiu o plano (referencia normativa).</summary>
    public string LeiInstituicao { get; private set; } = default!;

    /// <summary>Vencimento da celula de ingresso (classe 1, referencia 1) — base da matriz.</summary>
    public Vencimento VencimentoBase { get; private set; } = default!;

    /// <summary>Quantidade de classes (faixas verticais) da grade.</summary>
    public int NumeroClasses { get; private set; }

    /// <summary>Quantidade de referencias (steps horizontais) por classe.</summary>
    public int NumeroReferencias { get; private set; }

    /// <summary>Percentual de acrescimo entre referencias consecutivas (step horizontal; ex.: 3 = +3%).</summary>
    public decimal PercentualEntreReferencias { get; private set; }

    /// <summary>Percentual de acrescimo entre classes consecutivas (salto vertical; ex.: 5 = +5%).</summary>
    public decimal PercentualEntreClasses { get; private set; }

    /// <summary>Interstncio minimo (meses de efetivo exercicio) exigido para a progressao horizontal.</summary>
    public int IntersticioMeses { get; private set; }

    /// <summary>Nota minima de avaliacao de desempenho exigida quando o criterio inclui merecimento (0..100).</summary>
    public decimal NotaMinimaProgressao { get; private set; }

    /// <summary>Situacao (estado) do plano.</summary>
    public SituacaoPlanoCarreira Situacao { get; private set; }

    /// <summary>
    /// Institui um plano de carreira (PCCS). A grade tem <paramref name="numeroClasses"/> classes e
    /// <paramref name="numeroReferencias"/> referencias por classe; os percentuais de step e de classe
    /// e o interstncio/nota minima parametrizam as movimentacoes.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="denominacaoCarreira">Denominacao da carreira.</param>
    /// <param name="leiInstituicao">Lei municipal que institui o plano.</param>
    /// <param name="vencimentoBase">Vencimento da celula de ingresso (classe 1, referencia 1).</param>
    /// <param name="numeroClasses">Numero de classes (maior ou igual a 1).</param>
    /// <param name="numeroReferencias">Numero de referencias por classe (maior ou igual a 1).</param>
    /// <param name="percentualEntreReferencias">Percentual entre referencias consecutivas (em [0, 100)).</param>
    /// <param name="percentualEntreClasses">Percentual entre classes consecutivas (em [0, 100)).</param>
    /// <param name="intersticioMeses">Interstncio em meses para a progressao horizontal (maior ou igual a 0).</param>
    /// <param name="notaMinimaProgressao">Nota minima de avaliacao para progressao por merecimento (em [0, 100]).</param>
    /// <returns>Novo <see cref="PlanoCarreira"/> em situacao <see cref="SituacaoPlanoCarreira.Ativo"/>.</returns>
    /// <exception cref="ArgumentException">Se a denominacao/lei forem vazias ou excederem o limite.</exception>
    /// <exception cref="ArgumentNullException">Se o vencimento-base for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se dimensoes/percentuais/interstncio/nota estiverem fora da faixa.</exception>
    public static PlanoCarreira Instituir(
        Guid tenantId,
        string denominacaoCarreira,
        string leiInstituicao,
        Vencimento vencimentoBase,
        int numeroClasses,
        int numeroReferencias,
        decimal percentualEntreReferencias,
        decimal percentualEntreClasses,
        int intersticioMeses,
        decimal notaMinimaProgressao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(denominacaoCarreira);
        ArgumentException.ThrowIfNullOrWhiteSpace(leiInstituicao);
        ArgumentNullException.ThrowIfNull(vencimentoBase);
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroClasses, DimensaoMinima);
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroReferencias, DimensaoMinima);
        GarantirPercentual(percentualEntreReferencias, nameof(percentualEntreReferencias));
        GarantirPercentual(percentualEntreClasses, nameof(percentualEntreClasses));
        ArgumentOutOfRangeException.ThrowIfNegative(intersticioMeses);
        if (notaMinimaProgressao is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(notaMinimaProgressao), notaMinimaProgressao, "Nota minima de progressao deve estar em [0, 100].");
        }

        var denominacao = denominacaoCarreira.Trim();
        if (denominacao.Length > ComprimentoMaximoDenominacao)
        {
            throw new ArgumentException($"Denominacao da carreira excede {ComprimentoMaximoDenominacao} caracteres.", nameof(denominacaoCarreira));
        }

        return new PlanoCarreira(
            PlanoCarreiraId.New(),
            tenantId,
            denominacao,
            leiInstituicao.Trim(),
            vencimentoBase,
            numeroClasses,
            numeroReferencias,
            percentualEntreReferencias,
            percentualEntreClasses,
            intersticioMeses,
            notaMinimaProgressao);
    }

    /// <summary>
    /// Calcula o vencimento DERIVADO de uma posicao da matriz: base x (1 + %ref)^(referencia-1) x
    /// (1 + %classe)^(classe-1), arredondado a 2 casas. Determinismo puro — a celula de ingresso
    /// (C1-R1) devolve exatamente o vencimento-base.
    /// </summary>
    /// <param name="posicao">Posicao (classe + referencia) a avaliar.</param>
    /// <returns>Vencimento da celula.</returns>
    /// <exception cref="ArgumentNullException">Se a posicao for nula.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a posicao estiver fora da grade do plano.</exception>
    public Vencimento VencimentoDa(PosicaoCarreira posicao)
    {
        ArgumentNullException.ThrowIfNull(posicao);
        GarantirPosicaoNaGrade(posicao);

        var fatorReferencia = Pow(1m + (PercentualEntreReferencias / 100m), posicao.Referencia - 1);
        var fatorClasse = Pow(1m + (PercentualEntreClasses / 100m), posicao.Classe - 1);
        var valor = decimal.Round(VencimentoBase.Valor * fatorReferencia * fatorClasse, 2, MidpointRounding.AwayFromZero);
        return Vencimento.De(valor);
    }

    /// <summary>Indica se a posicao informada existe dentro da grade (classe e referencia validas).</summary>
    /// <param name="posicao">Posicao a verificar.</param>
    /// <returns><c>true</c> se a posicao pertence a grade.</returns>
    public bool ContemPosicao(PosicaoCarreira posicao)
    {
        ArgumentNullException.ThrowIfNull(posicao);
        return posicao.Classe <= NumeroClasses && posicao.Referencia <= NumeroReferencias;
    }

    /// <summary>Indica se ha referencia seguinte (progressao horizontal possivel) a partir da posicao.</summary>
    /// <param name="posicao">Posicao atual.</param>
    /// <returns><c>true</c> se existe referencia subsequente na mesma classe.</returns>
    public bool PermiteProgressao(PosicaoCarreira posicao)
    {
        ArgumentNullException.ThrowIfNull(posicao);
        return posicao.Referencia < NumeroReferencias;
    }

    /// <summary>Indica se ha classe seguinte (promocao vertical possivel) a partir da posicao.</summary>
    /// <param name="posicao">Posicao atual.</param>
    /// <returns><c>true</c> se existe classe subsequente.</returns>
    public bool PermitePromocao(PosicaoCarreira posicao)
    {
        ArgumentNullException.ThrowIfNull(posicao);
        return posicao.Classe < NumeroClasses;
    }

    /// <summary>Revoga o plano por lei (estado terminal). Nao admite novas movimentacoes.</summary>
    /// <param name="leiRevogacao">Lei que revoga o plano.</param>
    /// <exception cref="ArgumentException">Se a lei de revogacao for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o plano ja estiver revogado.</exception>
    public void Revogar(string leiRevogacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leiRevogacao);
        if (Situacao == SituacaoPlanoCarreira.Revogado)
        {
            throw new InvalidOperationException("Plano de carreira ja revogado; estado terminal.");
        }

        Situacao = SituacaoPlanoCarreira.Revogado;
    }

    /// <summary>Garante que o plano esta ativo (admite enquadramento/movimentacao).</summary>
    /// <exception cref="InvalidOperationException">Se o plano estiver revogado.</exception>
    public void GarantirAtivo()
    {
        if (Situacao != SituacaoPlanoCarreira.Ativo)
        {
            throw new InvalidOperationException("Plano de carreira revogado nao admite enquadramento ou movimentacao.");
        }
    }

    private void GarantirPosicaoNaGrade(PosicaoCarreira posicao)
    {
        if (!ContemPosicao(posicao))
        {
            throw new ArgumentOutOfRangeException(
                nameof(posicao),
                posicao.ToString(),
                $"Posicao fora da grade do plano ({NumeroClasses} classes x {NumeroReferencias} referencias).");
        }
    }

    private static void GarantirPercentual(decimal percentual, string nome)
    {
        if (percentual is < 0m or >= 100m)
        {
            throw new ArgumentOutOfRangeException(nome, percentual, "Percentual de avanco deve estar em [0, 100).");
        }
    }

    // Potencia inteira de um decimal (expoente >= 0), preservando precisao decimal (sem ir a double).
    private static decimal Pow(decimal baseValor, int expoente)
    {
        var resultado = 1m;
        for (var i = 0; i < expoente; i++)
        {
            resultado *= baseValor;
        }

        return resultado;
    }
}
