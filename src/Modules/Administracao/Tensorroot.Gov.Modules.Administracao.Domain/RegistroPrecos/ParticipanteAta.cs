using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Identificador forte de um <see cref="ParticipanteAta"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ParticipanteAtaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ParticipanteAtaId"/>.</returns>
    public static ParticipanteAtaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Orgao PARTICIPANTE de uma Ata de Registro de Precos (art. 86, §1º, Lei 14.133/2021; Dec. 11.462/2023):
/// integrou o planejamento da contratacao (Intencao de Registro de Precos — IRP) e teve sua estimativa
/// somada ao quantitativo registrado. Diferencia-se do orgao NAO participante ("carona"), que adere sem
/// planejar e fica sujeito aos limites de adesao. Entidade filha da <see cref="Ata"/>.
/// </summary>
public sealed class ParticipanteAta : Entity<ParticipanteAtaId>
{
    private ParticipanteAta()
    {
    }

    private ParticipanteAta(ParticipanteAtaId id, string cnpjOrgao, string nomeOrgao, TipoOrgaoSrp tipo)
        : base(id)
    {
        CnpjOrgao = cnpjOrgao;
        NomeOrgao = nomeOrgao;
        Tipo = tipo;
    }

    /// <summary>CNPJ do orgao gerenciador/participante (identificacao unica na ata).</summary>
    public string CnpjOrgao { get; private set; } = default!;

    /// <summary>Nome do orgao gerenciador/participante.</summary>
    public string NomeOrgao { get; private set; } = default!;

    /// <summary>Papel do orgao no SRP (gerenciador ou participante).</summary>
    public TipoOrgaoSrp Tipo { get; private set; }

    /// <summary>Registra um orgao gerenciador/participante da ata.</summary>
    /// <param name="cnpjOrgao">CNPJ do orgao (obrigatorio).</param>
    /// <param name="nomeOrgao">Nome do orgao (obrigatorio).</param>
    /// <param name="tipo">Papel no SRP (Gerenciador ou Participante).</param>
    /// <returns>Novo <see cref="ParticipanteAta"/>.</returns>
    /// <exception cref="ArgumentException">Se CNPJ ou nome forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o tipo nao for Gerenciador nem Participante.</exception>
    public static ParticipanteAta Registrar(string cnpjOrgao, string nomeOrgao, TipoOrgaoSrp tipo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjOrgao);
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeOrgao);
        if (tipo is not (TipoOrgaoSrp.Gerenciador or TipoOrgaoSrp.Participante))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), "Participante da ata deve ser Gerenciador ou Participante.");
        }

        return new ParticipanteAta(ParticipanteAtaId.New(), cnpjOrgao.Trim(), nomeOrgao.Trim(), tipo);
    }
}
