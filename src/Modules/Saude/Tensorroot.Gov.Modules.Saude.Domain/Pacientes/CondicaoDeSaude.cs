using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

/// <summary>Identificador forte de uma <see cref="CondicaoDeSaude"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CondicaoDeSaudeId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CondicaoDeSaudeId"/>.</returns>
    public static CondicaoDeSaudeId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Problema/condicao de saude do paciente (entidade do agregado), codificado por
/// CID-10 ou CIAP-2. Dado pessoal sensivel (LGPD art. 11).
/// </summary>
public sealed class CondicaoDeSaude : Entity<CondicaoDeSaudeId>
{
    /// <summary>Comprimento maximo do codigo CID-10/CIAP-2.</summary>
    public const int ComprimentoCodigo = 10;

    private CondicaoDeSaude()
    {
    }

    private CondicaoDeSaude(CondicaoDeSaudeId id, string codigo, string descricao, DateOnly dataRegistro)
        : base(id)
    {
        Codigo = codigo;
        Descricao = descricao;
        DataRegistro = dataRegistro;
        Ativa = true;
    }

    /// <summary>Codigo CID-10 ou CIAP-2.</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Descricao da condicao.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Data do registro da condicao.</summary>
    public DateOnly DataRegistro { get; private set; }

    /// <summary>Indica se a condicao esta ativa.</summary>
    public bool Ativa { get; private set; }

    /// <summary>Registra uma condicao de saude (codigo nao vazio e data nao futura).</summary>
    /// <param name="codigo">Codigo CID-10/CIAP-2 (obrigatorio).</param>
    /// <param name="descricao">Descricao da condicao.</param>
    /// <param name="dataRegistro">Data do registro (nao futura).</param>
    /// <param name="hoje">Data corrente para a guarda de data nao futura.</param>
    /// <returns>Nova <see cref="CondicaoDeSaude"/>.</returns>
    /// <exception cref="ArgumentException">Se o codigo for vazio ou exceder o limite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a data de registro for futura.</exception>
    public static CondicaoDeSaude Registrar(string codigo, string descricao, DateOnly dataRegistro, DateOnly hoje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentNullException.ThrowIfNull(descricao);
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (normalizado.Length > ComprimentoCodigo)
        {
            throw new ArgumentException($"Codigo CID-10/CIAP-2 excede {ComprimentoCodigo} caracteres.", nameof(codigo));
        }

        if (dataRegistro > hoje)
        {
            throw new ArgumentOutOfRangeException(nameof(dataRegistro), "Data de registro nao pode ser futura.");
        }

        return new CondicaoDeSaude(CondicaoDeSaudeId.New(), normalizado, descricao.Trim(), dataRegistro);
    }
}
