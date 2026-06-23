using Tensorroot.Gov.SharedKernel.Primitives;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;

/// <summary>
/// Registro de uma dose de imunobiologico aplicada ao paciente: vacina, numero/tipo da dose, lote,
/// aplicador (profissional), data e o aprazamento calculado da proxima dose. Entidade filha da
/// <see cref="CarteiraVacinacao"/> — toda aplicacao passa pela raiz (que evita duplicidade de dose).
/// </summary>
public sealed class DoseAplicada : Entity<DoseAplicadaId>
{
    private DoseAplicada()
    {
    }

    private DoseAplicada(
        DoseAplicadaId id,
        ImunobiologicoId imunobiologicoId,
        TipoDose tipoDose,
        int numeroDose,
        string lote,
        ProfissionalId aplicadorId,
        DateOnly dataAplicacao,
        DateOnly? proximaDoseAprazada)
        : base(id)
    {
        ImunobiologicoId = imunobiologicoId;
        TipoDose = tipoDose;
        NumeroDose = numeroDose;
        Lote = lote;
        AplicadorId = aplicadorId;
        DataAplicacao = dataAplicacao;
        ProximaDoseAprazada = proximaDoseAprazada;
    }

    /// <summary>Imunobiologico aplicado (referencia por Id ao catalogo).</summary>
    public ImunobiologicoId ImunobiologicoId { get; private set; }

    /// <summary>Tipo da dose no esquema (D1/D2/reforco/unica).</summary>
    public TipoDose TipoDose { get; private set; }

    /// <summary>Numero sequencial da dose no esquema (1..TotalDoses).</summary>
    public int NumeroDose { get; private set; }

    /// <summary>Lote do imunobiologico aplicado (rastreabilidade — evento adverso/recall).</summary>
    public string Lote { get; private set; } = default!;

    /// <summary>Profissional aplicador (referencia por Id).</summary>
    public ProfissionalId AplicadorId { get; private set; }

    /// <summary>Data de aplicacao.</summary>
    public DateOnly DataAplicacao { get; private set; }

    /// <summary>Data aprazada para a proxima dose (nula quando completa o esquema ou dose unica).</summary>
    public DateOnly? ProximaDoseAprazada { get; private set; }

    /// <summary>Cria o registro de uma dose aplicada (com o aprazamento ja calculado pela raiz).</summary>
    /// <param name="imunobiologicoId">Imunobiologico aplicado.</param>
    /// <param name="tipoDose">Tipo da dose.</param>
    /// <param name="numeroDose">Numero da dose (>= 1).</param>
    /// <param name="lote">Lote aplicado.</param>
    /// <param name="aplicadorId">Profissional aplicador.</param>
    /// <param name="dataAplicacao">Data de aplicacao.</param>
    /// <param name="proximaDoseAprazada">Aprazamento da proxima dose (opcional).</param>
    /// <returns>Nova <see cref="DoseAplicada"/>.</returns>
    /// <exception cref="ArgumentException">Se o lote for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o numero da dose for &lt; 1.</exception>
    internal static DoseAplicada Registrar(
        ImunobiologicoId imunobiologicoId,
        TipoDose tipoDose,
        int numeroDose,
        string lote,
        ProfissionalId aplicadorId,
        DateOnly dataAplicacao,
        DateOnly? proximaDoseAprazada)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lote);
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroDose, 1);
        return new DoseAplicada(DoseAplicadaId.New(), imunobiologicoId, tipoDose, numeroDose, lote.Trim(), aplicadorId, dataAplicacao, proximaDoseAprazada);
    }
}
