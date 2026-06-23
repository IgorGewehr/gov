using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;

/// <summary>
/// Anexo da LDO exigido pela LRF (AMF/ARF — LC 101/2000 art. 4º). Entidade-filha de
/// <see cref="LeiDiretrizes"/>. O conteúdo aponta para documento (GED/Protocolo) ou texto.
/// </summary>
public sealed class AnexoLdo : Entity<AnexoLdoId>
{
    private AnexoLdo()
    {
    }

    private AnexoLdo(AnexoLdoId id, LdoId ldoId, TipoAnexoLdo tipo, string referenciaDocumento, bool obrigatorio)
        : base(id)
    {
        LdoId = ldoId;
        Tipo = tipo;
        ReferenciaDocumento = referenciaDocumento;
        Obrigatorio = obrigatorio;
    }

    /// <summary>LDO à qual o anexo pertence.</summary>
    public LdoId LdoId { get; private set; }

    /// <summary>Tipo do anexo (AMF/ARF).</summary>
    public TipoAnexoLdo Tipo { get; private set; }

    /// <summary>Referência ao documento (id do GED/Protocolo) ou conteúdo textual resumido.</summary>
    public string ReferenciaDocumento { get; private set; } = default!;

    /// <summary>Indica se o anexo é obrigatório para o porte do ente (parametrizável por tenant).</summary>
    public bool Obrigatorio { get; private set; }

    /// <summary>Cria um anexo da LDO.</summary>
    /// <returns>Novo <see cref="AnexoLdo"/>.</returns>
    /// <exception cref="ArgumentException">Se o tipo for inválido.</exception>
    internal static AnexoLdo Criar(LdoId ldoId, TipoAnexoLdo tipo, string referenciaDocumento, bool obrigatorio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenciaDocumento);
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de anexo invalido.", nameof(tipo));
        }

        return new AnexoLdo(AnexoLdoId.New(), ldoId, tipo, referenciaDocumento.Trim(), obrigatorio);
    }
}
