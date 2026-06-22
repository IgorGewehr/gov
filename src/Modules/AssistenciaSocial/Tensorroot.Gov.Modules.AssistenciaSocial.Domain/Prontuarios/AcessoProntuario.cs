using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

/// <summary>Identificador forte de um <see cref="AcessoProntuario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AcessoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AcessoId"/>.</returns>
    public static AcessoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro <b>imutavel</b> da trilha de acesso ao prontuario (quem leu, quando, por que) —
/// entidade-filha <b>append-only</b> do <see cref="ProntuarioSuas"/>, exigivel pelo controle
/// social/Tribunal de Contas (I-7, I-8). Nunca e atualizado nem removido.
/// </summary>
public sealed class AcessoProntuario : Entity<AcessoId>
{
    private AcessoProntuario()
    {
    }

    private AcessoProntuario(
        AcessoId id,
        Guid usuarioId,
        string motivoAcesso,
        DateTime dataHoraAcessoUtc)
        : base(id)
    {
        UsuarioId = usuarioId;
        MotivoAcesso = motivoAcesso;
        DataHoraAcessoUtc = dataHoraAcessoUtc;
    }

    /// <summary>Usuario que acessou o prontuario.</summary>
    public Guid UsuarioId { get; private set; }

    /// <summary>Justificativa obrigatoria da leitura do conteudo sigiloso.</summary>
    public string MotivoAcesso { get; private set; } = default!;

    /// <summary>Momento (UTC) do acesso.</summary>
    public DateTime DataHoraAcessoUtc { get; private set; }

    /// <summary>Registra (append-only) um acesso ao prontuario na trilha imutavel.</summary>
    /// <param name="usuarioId">Usuario que acessou.</param>
    /// <param name="motivoAcesso">Justificativa do acesso.</param>
    /// <param name="dataHoraAcessoUtc">Momento (UTC) do acesso.</param>
    /// <returns>Novo <see cref="AcessoProntuario"/>.</returns>
    /// <exception cref="ArgumentException">Se o motivo de acesso for vazio.</exception>
    internal static AcessoProntuario Registrar(
        Guid usuarioId,
        string motivoAcesso,
        DateTime dataHoraAcessoUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivoAcesso);
        return new AcessoProntuario(AcessoId.New(), usuarioId, motivoAcesso, dataHoraAcessoUtc);
    }
}
