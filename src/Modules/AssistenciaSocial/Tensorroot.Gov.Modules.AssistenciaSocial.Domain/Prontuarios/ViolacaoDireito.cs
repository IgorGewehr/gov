using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

/// <summary>Identificador forte de uma <see cref="ViolacaoDireito"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ViolacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ViolacaoId"/>.</returns>
    public static ViolacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro de um direito violado (entidade-filha do <see cref="ProntuarioSuas"/>). Quando
/// <see cref="EnvolveCriancaAdolescente"/> e verdadeiro, constitui dado sensivel reforcado
/// (art. 11 LGPD), sujeito a minimizacao, finalidade e acesso sempre auditado (I-10).
/// </summary>
public sealed class ViolacaoDireito : Entity<ViolacaoId>
{
    private ViolacaoDireito()
    {
    }

    private ViolacaoDireito(
        ViolacaoId id,
        TipoViolacaoDireito tipoViolacao,
        bool envolveCriancaAdolescente,
        DateOnly dataIdentificacao)
        : base(id)
    {
        TipoViolacao = tipoViolacao;
        EnvolveCriancaAdolescente = envolveCriancaAdolescente;
        DataIdentificacao = dataIdentificacao;
    }

    /// <summary>Tipo da violacao de direito identificada.</summary>
    public TipoViolacaoDireito TipoViolacao { get; private set; }

    /// <summary>Indica se a violacao envolve crianca/adolescente — dado sensivel reforcado (art. 11 LGPD).</summary>
    public bool EnvolveCriancaAdolescente { get; private set; }

    /// <summary>Data de identificacao da violacao.</summary>
    public DateOnly DataIdentificacao { get; private set; }

    /// <summary>Registra uma violacao de direito.</summary>
    /// <param name="tipoViolacao">Tipo da violacao.</param>
    /// <param name="envolveCriancaAdolescente">Se envolve crianca/adolescente.</param>
    /// <param name="dataIdentificacao">Data de identificacao.</param>
    /// <returns>Nova <see cref="ViolacaoDireito"/>.</returns>
    internal static ViolacaoDireito Registrar(
        TipoViolacaoDireito tipoViolacao,
        bool envolveCriancaAdolescente,
        DateOnly dataIdentificacao)
        => new(ViolacaoId.New(), tipoViolacao, envolveCriancaAdolescente, dataIdentificacao);
}
