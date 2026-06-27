using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Natureza de um lancamento no <see cref="BancoDeHoras"/>.</summary>
public enum TipoLancamentoBancoHoras
{
    /// <summary>Credito (horas extras a favor do servidor).</summary>
    Credito = 1,

    /// <summary>Debito (faltas/atrasos ou compensacao formal).</summary>
    Debito = 2,

    /// <summary>Prescricao (perda de creditos nao compensados na janela).</summary>
    Prescricao = 3,
}

/// <summary>
/// Lancamento (linha do livro-razao) do <see cref="BancoDeHoras"/>: credito/debito/prescricao de minutos
/// numa data-base, com referencia de negocio idempotente e descricao. Entidade-filha owned do agregado;
/// nasce valida via <see cref="Criar"/>.
/// </summary>
public sealed class LancamentoBancoHoras : Entity<LancamentoBancoHorasId>
{
    /// <summary>Comprimento maximo da referencia de negocio.</summary>
    public const int ComprimentoMaximoReferencia = 100;

    /// <summary>Comprimento maximo da descricao.</summary>
    public const int ComprimentoMaximoDescricao = 200;

    private LancamentoBancoHoras()
    {
    }

    private LancamentoBancoHoras(
        LancamentoBancoHorasId id,
        TipoLancamentoBancoHoras tipo,
        int minutos,
        DateOnly data,
        string referencia,
        string descricao)
        : base(id)
    {
        Tipo = tipo;
        Minutos = minutos;
        Data = data;
        Referencia = referencia;
        Descricao = descricao;
    }

    /// <summary>Natureza do lancamento.</summary>
    public TipoLancamentoBancoHoras Tipo { get; private set; }

    /// <summary>Minutos do lancamento (sempre positivo; o sinal vem do <see cref="Tipo"/>).</summary>
    public int Minutos { get; private set; }

    /// <summary>Data-base do lancamento (competencia de origem; base da prescricao).</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Referencia de negocio (idempotencia do lancamento).</summary>
    public string Referencia { get; private set; } = default!;

    /// <summary>Descricao legivel do lancamento.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Cria um lancamento, normalizando e validando os campos.</summary>
    /// <param name="tipo">Natureza do lancamento.</param>
    /// <param name="minutos">Minutos (&gt; 0).</param>
    /// <param name="data">Data-base.</param>
    /// <param name="referencia">Referencia de negocio (nao vazia, ate 100 caracteres).</param>
    /// <param name="descricao">Descricao (nao vazia, ate 200 caracteres).</param>
    /// <returns>Novo <see cref="LancamentoBancoHoras"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="minutos"/> nao for positivo.</exception>
    /// <exception cref="ArgumentException">Se referencia/descricao forem vazias ou excederem o limite.</exception>
    public static LancamentoBancoHoras Criar(
        TipoLancamentoBancoHoras tipo,
        int minutos,
        DateOnly data,
        string referencia,
        string descricao)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutos);
        ArgumentException.ThrowIfNullOrWhiteSpace(referencia);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        var referenciaNormalizada = referencia.Trim();
        var descricaoNormalizada = descricao.Trim();
        if (referenciaNormalizada.Length > ComprimentoMaximoReferencia)
        {
            throw new ArgumentException($"Referencia excede {ComprimentoMaximoReferencia} caracteres.", nameof(referencia));
        }

        if (descricaoNormalizada.Length > ComprimentoMaximoDescricao)
        {
            throw new ArgumentException($"Descricao excede {ComprimentoMaximoDescricao} caracteres.", nameof(descricao));
        }

        return new LancamentoBancoHoras(LancamentoBancoHorasId.New(), tipo, minutos, data, referenciaNormalizada, descricaoNormalizada);
    }
}

/// <summary>Identificador forte de <see cref="LancamentoBancoHoras"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LancamentoBancoHorasId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LancamentoBancoHorasId"/>.</returns>
    public static LancamentoBancoHorasId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
