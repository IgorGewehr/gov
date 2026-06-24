using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Certidao de regularidade da OSC (fiscal/trabalhista/FGTS/CRF), com validade. A habilitacao (B-INV-3)
/// exige certidoes validas na data da celebracao.
/// </summary>
/// <param name="Tipo">Tipo da certidao (ex.: "CND-Federal", "FGTS", "CNDT").</param>
/// <param name="ValidaAte">Data ate a qual a certidao e valida.</param>
public readonly record struct CertidaoRegularidade(string Tipo, DateOnly ValidaAte)
{
    /// <summary>Verdadeiro se a certidao esta valida em <paramref name="data"/> (<c>data &lt;= ValidaAte</c>).</summary>
    /// <param name="data">Data de referencia.</param>
    /// <returns><c>true</c> se valida.</returns>
    public bool ValidaEm(DateOnly data) => data <= ValidaAte;
}

/// <summary>
/// Organizacao da Sociedade Civil (OSC) parceira (fluxo B — MROSC). Carrega os requisitos de habilitacao
/// dos arts. 33-34 da Lei 13.019/2014 (<see cref="ExperienciaPrevia"/>/<see cref="CapacidadeTecnica"/>) e as
/// certidoes de regularidade com validade. A invariante de habilitacao (B-INV-3) e aferida pelo agregado.
/// </summary>
public sealed class Osc : ValueObject
{
    private readonly List<CertidaoRegularidade> _certidoes;

    private Osc(
        Cnpj cnpj,
        string razaoSocial,
        string naturezaJuridica,
        bool experienciaPrevia,
        bool capacidadeTecnica,
        IReadOnlyList<CertidaoRegularidade> certidoes)
    {
        Cnpj = cnpj;
        RazaoSocial = razaoSocial;
        NaturezaJuridica = naturezaJuridica;
        ExperienciaPrevia = experienciaPrevia;
        CapacidadeTecnica = capacidadeTecnica;
        _certidoes = [.. certidoes];
    }

    /// <summary>CNPJ da OSC.</summary>
    public Cnpj Cnpj { get; }

    /// <summary>Razao social da OSC.</summary>
    public string RazaoSocial { get; }

    /// <summary>Natureza juridica (ex.: associacao, fundacao, OSCIP).</summary>
    public string NaturezaJuridica { get; }

    /// <summary>Experiencia previa comprovada (art. 33, V, "b").</summary>
    public bool ExperienciaPrevia { get; }

    /// <summary>Capacidade tecnica e operacional comprovada (art. 33, V, "c"/art. 34).</summary>
    public bool CapacidadeTecnica { get; }

    /// <summary>Certidoes de regularidade com validade.</summary>
    public IReadOnlyList<CertidaoRegularidade> Certidoes => _certidoes;

    /// <summary>Cria a OSC a partir de dados validados.</summary>
    /// <param name="cnpj">CNPJ.</param>
    /// <param name="razaoSocial">Razao social (obrigatoria).</param>
    /// <param name="naturezaJuridica">Natureza juridica (obrigatoria).</param>
    /// <param name="experienciaPrevia">Experiencia previa comprovada (art. 33).</param>
    /// <param name="capacidadeTecnica">Capacidade tecnica/operacional comprovada (art. 33-34).</param>
    /// <param name="certidoes">Certidoes de regularidade (podem ser vazias).</param>
    /// <returns>Nova <see cref="Osc"/>.</returns>
    /// <exception cref="ArgumentException">Se razao social ou natureza juridica forem vazias.</exception>
    public static Osc Criar(
        Cnpj cnpj,
        string razaoSocial,
        string naturezaJuridica,
        bool experienciaPrevia,
        bool capacidadeTecnica,
        IReadOnlyList<CertidaoRegularidade>? certidoes = null)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        ArgumentException.ThrowIfNullOrWhiteSpace(razaoSocial);
        ArgumentException.ThrowIfNullOrWhiteSpace(naturezaJuridica);
        return new Osc(cnpj, razaoSocial.Trim(), naturezaJuridica.Trim(), experienciaPrevia, capacidadeTecnica, certidoes ?? []);
    }

    /// <summary>
    /// Verdadeiro se a OSC esta habilitada em <paramref name="data"/> (B-INV-3): requisitos dos arts. 33-34
    /// atendidos (experiencia previa E capacidade tecnica) E todas as certidoes informadas validas na data.
    /// Fail-closed: se NAO houver certidoes informadas, a habilitacao e negada.
    /// </summary>
    /// <param name="data">Data de referencia (data da celebracao).</param>
    /// <returns><c>true</c> se habilitada.</returns>
    public bool Habilitada(DateOnly data)
    {
        if (!ExperienciaPrevia || !CapacidadeTecnica)
        {
            return false;
        }

        if (_certidoes.Count == 0)
        {
            return false;
        }

        return _certidoes.TrueForAll(certidao => certidao.ValidaEm(data));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Cnpj;
        yield return RazaoSocial;
        yield return NaturezaJuridica;
        yield return ExperienciaPrevia;
        yield return CapacidadeTecnica;
        foreach (var certidao in _certidoes)
        {
            yield return certidao;
        }
    }
}
