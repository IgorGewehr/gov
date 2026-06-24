using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Orgao/entidade concedente do convenio federal recebido (fluxo A): a Uniao (ou Estado) que repassa
/// recursos ao municipio convenente. O <see cref="NumeroConvenioTransferegov"/> e nulo ate a celebracao
/// (so existe apos o registro/numeracao no Transferegov.br).
/// </summary>
public sealed class OrgaoConcedente : ValueObject
{
    private OrgaoConcedente(
        Cnpj cnpj,
        string nome,
        EsferaConcedente esfera,
        SistemaOrigemConvenio sistemaOrigem,
        string? numeroConvenioTransferegov)
    {
        Cnpj = cnpj;
        Nome = nome;
        Esfera = esfera;
        SistemaOrigem = sistemaOrigem;
        NumeroConvenioTransferegov = numeroConvenioTransferegov;
    }

    /// <summary>CNPJ do orgao concedente.</summary>
    public Cnpj Cnpj { get; }

    /// <summary>Nome do orgao/entidade concedente.</summary>
    public string Nome { get; }

    /// <summary>Esfera (Uniao/Estado).</summary>
    public EsferaConcedente Esfera { get; }

    /// <summary>Sistema de origem do cadastro (Transferegov/SICONV-legado/Outro).</summary>
    public SistemaOrigemConvenio SistemaOrigem { get; }

    /// <summary>Numero do convenio no Transferegov.br (nulo ate a celebracao/numeracao).</summary>
    public string? NumeroConvenioTransferegov { get; }

    /// <summary>Cria o concedente a partir de dados validados.</summary>
    /// <param name="cnpj">CNPJ do concedente.</param>
    /// <param name="nome">Nome do orgao (obrigatorio).</param>
    /// <param name="esfera">Esfera (Uniao/Estado).</param>
    /// <param name="sistemaOrigem">Sistema de origem.</param>
    /// <param name="numeroConvenioTransferegov">Numero no Transferegov (opcional ate a celebracao).</param>
    /// <returns>Novo <see cref="OrgaoConcedente"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    public static OrgaoConcedente Criar(
        Cnpj cnpj,
        string nome,
        EsferaConcedente esfera,
        SistemaOrigemConvenio sistemaOrigem,
        string? numeroConvenioTransferegov = null)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return new OrgaoConcedente(
            cnpj,
            nome.Trim(),
            esfera,
            sistemaOrigem,
            string.IsNullOrWhiteSpace(numeroConvenioTransferegov) ? null : numeroConvenioTransferegov.Trim());
    }

    /// <summary>Devolve uma copia com o numero do Transferegov atribuido (na celebracao).</summary>
    /// <param name="numero">Numero do convenio no Transferegov.br.</param>
    /// <returns>Novo concedente com o numero atribuido.</returns>
    /// <exception cref="ArgumentException">Se o numero for vazio.</exception>
    public OrgaoConcedente ComNumeroTransferegov(string numero)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        return new OrgaoConcedente(Cnpj, Nome, Esfera, SistemaOrigem, numero.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Cnpj;
        yield return Nome;
        yield return Esfera;
        yield return SistemaOrigem;
        yield return NumeroConvenioTransferegov;
    }
}
