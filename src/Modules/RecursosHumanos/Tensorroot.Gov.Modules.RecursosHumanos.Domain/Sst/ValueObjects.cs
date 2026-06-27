using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

/// <summary>
/// Medico responsavel pelo ASO/PCMSO (grupos <c>medico</c>/<c>respMonit</c> do S-2220). Identificado pelo
/// CRM/UF e nome. Value Object — comparado pelos componentes, nasce valido.
/// </summary>
public sealed class MedicoResponsavel : ValueObject
{
    /// <summary>Comprimento maximo do numero do conselho.</summary>
    public const int ComprimentoMaximoConselho = 14;

    /// <summary>Comprimento maximo do nome.</summary>
    public const int ComprimentoMaximoNome = 70;

    private MedicoResponsavel(string nome, string nrConselho, string ufConselho)
    {
        Nome = nome;
        NrConselho = nrConselho;
        UfConselho = ufConselho;
    }

    /// <summary>Nome civil do medico.</summary>
    public string Nome { get; }

    /// <summary>Numero de inscricao no conselho (CRM).</summary>
    public string NrConselho { get; }

    /// <summary>UF do conselho (sigla de 2 letras).</summary>
    public string UfConselho { get; }

    /// <summary>Cria o medico responsavel, normalizando e validando os campos.</summary>
    /// <param name="nome">Nome civil (nao vazio, ate 70 caracteres).</param>
    /// <param name="nrConselho">Numero do CRM (nao vazio, ate 14 caracteres).</param>
    /// <param name="ufConselho">UF do conselho (2 letras).</param>
    /// <returns>Instancia de <see cref="MedicoResponsavel"/>.</returns>
    /// <exception cref="ArgumentException">Se algum campo for vazio/invalido.</exception>
    public static MedicoResponsavel Criar(string nome, string nrConselho, string ufConselho)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(nrConselho);
        ArgumentException.ThrowIfNullOrWhiteSpace(ufConselho);

        var nomeNormalizado = nome.Trim();
        var conselhoNormalizado = nrConselho.Trim();
        var ufNormalizada = ufConselho.Trim().ToUpperInvariant();

        if (nomeNormalizado.Length > ComprimentoMaximoNome)
        {
            throw new ArgumentException($"Nome do medico excede {ComprimentoMaximoNome} caracteres.", nameof(nome));
        }

        if (conselhoNormalizado.Length > ComprimentoMaximoConselho)
        {
            throw new ArgumentException($"Numero do conselho excede {ComprimentoMaximoConselho} caracteres.", nameof(nrConselho));
        }

        if (ufNormalizada.Length != 2)
        {
            throw new ArgumentException("UF do conselho deve ter 2 letras.", nameof(ufConselho));
        }

        return new MedicoResponsavel(nomeNormalizado, conselhoNormalizado, ufNormalizada);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Nome;
        yield return NrConselho;
        yield return UfConselho;
    }
}

/// <summary>
/// Agente nocivo (fisico/quimico/biologico/ergonomico) ao qual o servidor esta exposto — item do grupo
/// <c>agNoc</c> do S-2240 e do Perfil Profissiografico Previdenciario (PPP — IN INSS 128/2022). Carrega o
/// codigo do agente (Tabela 23 do eSocial), a intensidade/concentracao medida e a tecnica/EPC/EPI. Value
/// Object — nasce valido. // TODO(validar-oficial): dominio exato de codAgNoc/tpAval no XSD travado.
/// </summary>
public sealed class AgenteNocivo : ValueObject
{
    /// <summary>Codigo "ausencia de agente nocivo" (Tabela 23) — exposicao sem agente a declarar.</summary>
    public const string CodigoAusenciaAgente = "09.01.001";

    /// <summary>Comprimento maximo do codigo do agente.</summary>
    public const int ComprimentoMaximoCodigo = 12;

    /// <summary>Comprimento maximo da descricao livre.</summary>
    public const int ComprimentoMaximoDescricao = 999;

    private AgenteNocivo(string codigo, string descricao, decimal? intensidade, string? unidadeMedida, bool utilizaEpc, bool utilizaEpi)
    {
        Codigo = codigo;
        Descricao = descricao;
        Intensidade = intensidade;
        UnidadeMedida = unidadeMedida;
        UtilizaEpc = utilizaEpc;
        UtilizaEpi = utilizaEpi;
    }

    /// <summary>Codigo do agente nocivo (Tabela 23 do eSocial).</summary>
    public string Codigo { get; }

    /// <summary>Descricao do agente/atividade (texto livre do PPP/S-2240).</summary>
    public string Descricao { get; }

    /// <summary>Intensidade/concentracao medida (quando quantificavel); nula para qualitativos.</summary>
    public decimal? Intensidade { get; }

    /// <summary>Unidade de medida da intensidade (ex.: dB(A), mg/m3); nula para qualitativos.</summary>
    public string? UnidadeMedida { get; }

    /// <summary>Indica uso de EPC (Equipamento de Protecao Coletiva) eficaz.</summary>
    public bool UtilizaEpc { get; }

    /// <summary>Indica uso de EPI (Equipamento de Protecao Individual) eficaz.</summary>
    public bool UtilizaEpi { get; }

    /// <summary>Indica se este item representa AUSENCIA de agente nocivo (codigo padrao da Tabela 23).</summary>
    public bool EhAusenciaDeAgente => Codigo == CodigoAusenciaAgente;

    /// <summary>Cria o agente nocivo, normalizando e validando os campos.</summary>
    /// <param name="codigo">Codigo do agente (Tabela 23; nao vazio).</param>
    /// <param name="descricao">Descricao/atividade (nao vazia).</param>
    /// <param name="intensidade">Intensidade medida (opcional; deve ser positiva quando informada).</param>
    /// <param name="unidadeMedida">Unidade da intensidade (opcional).</param>
    /// <param name="utilizaEpc">Uso de EPC eficaz.</param>
    /// <param name="utilizaEpi">Uso de EPI eficaz.</param>
    /// <returns>Instancia de <see cref="AgenteNocivo"/>.</returns>
    /// <exception cref="ArgumentException">Se codigo/descricao forem vazios ou excederem o limite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a intensidade informada for negativa.</exception>
    public static AgenteNocivo Criar(
        string codigo,
        string descricao,
        decimal? intensidade = null,
        string? unidadeMedida = null,
        bool utilizaEpc = false,
        bool utilizaEpi = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        var codigoNormalizado = codigo.Trim();
        var descricaoNormalizada = descricao.Trim();
        if (codigoNormalizado.Length > ComprimentoMaximoCodigo)
        {
            throw new ArgumentException($"Codigo do agente excede {ComprimentoMaximoCodigo} caracteres.", nameof(codigo));
        }

        if (descricaoNormalizada.Length > ComprimentoMaximoDescricao)
        {
            throw new ArgumentException($"Descricao do agente excede {ComprimentoMaximoDescricao} caracteres.", nameof(descricao));
        }

        if (intensidade is { } valor)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(valor);
        }

        var unidade = string.IsNullOrWhiteSpace(unidadeMedida) ? null : unidadeMedida.Trim();
        return new AgenteNocivo(codigoNormalizado, descricaoNormalizada, intensidade, unidade, utilizaEpc, utilizaEpi);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Codigo;
        yield return Descricao;
        yield return Intensidade;
        yield return UnidadeMedida;
        yield return UtilizaEpc;
        yield return UtilizaEpi;
    }
}
