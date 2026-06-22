using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

/// <summary>Identificador forte de uma <see cref="Alergia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AlergiaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AlergiaId"/>.</returns>
    public static AlergiaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Reacao alergica/intolerancia registrada do paciente (entidade do agregado).
/// Dado pessoal sensivel (LGPD art. 11).
/// </summary>
public sealed class Alergia : Entity<AlergiaId>
{
    /// <summary>Comprimento maximo da substancia.</summary>
    public const int ComprimentoSubstancia = 120;

    private Alergia()
    {
    }

    private Alergia(AlergiaId id, string substancia, string gravidade, DateOnly dataRegistro)
        : base(id)
    {
        Substancia = substancia;
        Gravidade = gravidade;
        DataRegistro = dataRegistro;
    }

    /// <summary>Substancia/agente da alergia — dado clinico sensivel, redigido na trilha (LG-3).</summary>
    [CampoSensivelLgpd]
    public string Substancia { get; private set; } = default!;

    /// <summary>Gravidade da reacao — dado clinico sensivel, redigido na trilha (LG-3).</summary>
    [CampoSensivelLgpd]
    public string Gravidade { get; private set; } = default!;

    /// <summary>Data do registro da alergia.</summary>
    public DateOnly DataRegistro { get; private set; }

    /// <summary>Registra uma alergia (substancia nao vazia e data nao futura).</summary>
    /// <param name="substancia">Substancia/agente (obrigatorio).</param>
    /// <param name="gravidade">Gravidade da reacao.</param>
    /// <param name="dataRegistro">Data do registro (nao futura).</param>
    /// <param name="hoje">Data corrente para a guarda de data nao futura.</param>
    /// <returns>Nova <see cref="Alergia"/>.</returns>
    /// <exception cref="ArgumentException">Se a substancia for vazia ou exceder o limite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a data de registro for futura.</exception>
    public static Alergia Registrar(string substancia, string gravidade, DateOnly dataRegistro, DateOnly hoje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(substancia);
        ArgumentNullException.ThrowIfNull(gravidade);
        var normalizada = substancia.Trim();
        if (normalizada.Length > ComprimentoSubstancia)
        {
            throw new ArgumentException($"Substancia da alergia excede {ComprimentoSubstancia} caracteres.", nameof(substancia));
        }

        if (dataRegistro > hoje)
        {
            throw new ArgumentOutOfRangeException(nameof(dataRegistro), "Data de registro nao pode ser futura.");
        }

        return new Alergia(AlergiaId.New(), normalizada, gravidade.Trim(), dataRegistro);
    }
}
