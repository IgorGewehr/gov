using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>Identificador forte de uma <see cref="EvolucaoSOAP"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EvolucaoSOAPId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EvolucaoSOAPId"/>.</returns>
    public static EvolucaoSOAPId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Nota clinica estruturada no padrao SOAP (Subjetivo, Objetivo, Avaliacao, Plano) de um atendimento.
/// E append-only: uma evolucao assinada nao se edita — alteracoes apenas por adendo datado/reassinado.
/// </summary>
public sealed class EvolucaoSOAP : Entity<EvolucaoSOAPId>
{
    private EvolucaoSOAP()
    {
    }

    private EvolucaoSOAP(
        EvolucaoSOAPId id,
        string subjetivo,
        string objetivo,
        string avaliacao,
        string plano,
        DateTimeOffset dataHora,
        bool assinada,
        bool ehAdendo,
        Guid? evolucaoReferenciadaId)
        : base(id)
    {
        Subjetivo = subjetivo;
        Objetivo = objetivo;
        Avaliacao = avaliacao;
        Plano = plano;
        DataHora = dataHora;
        Assinada = assinada;
        EhAdendo = ehAdendo;
        EvolucaoReferenciadaId = evolucaoReferenciadaId;
    }

    /// <summary>Componente Subjetivo (relato do paciente).</summary>
    public string Subjetivo { get; private set; } = default!;

    /// <summary>Componente Objetivo (exame fisico/achados).</summary>
    public string Objetivo { get; private set; } = default!;

    /// <summary>Componente Avaliacao (diagnostico/hipoteses).</summary>
    public string Avaliacao { get; private set; } = default!;

    /// <summary>Componente Plano (conduta/orientacoes).</summary>
    public string Plano { get; private set; } = default!;

    /// <summary>Data/hora do registro da nota.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Indica se a evolucao foi assinada (imutavel a partir dai).</summary>
    public bool Assinada { get; private set; }

    /// <summary>Indica se a nota e um adendo a uma evolucao ja assinada.</summary>
    public bool EhAdendo { get; private set; }

    /// <summary>Quando adendo, identificador da evolucao complementada.</summary>
    public Guid? EvolucaoReferenciadaId { get; private set; }

    /// <summary>Cria uma nova nota SOAP (nao assinada) de atendimento em andamento.</summary>
    /// <param name="subjetivo">Componente Subjetivo.</param>
    /// <param name="objetivo">Componente Objetivo.</param>
    /// <param name="avaliacao">Componente Avaliacao.</param>
    /// <param name="plano">Componente Plano.</param>
    /// <param name="dataHora">Data/hora do registro.</param>
    /// <returns>Nova <see cref="EvolucaoSOAP"/>.</returns>
    /// <exception cref="ArgumentException">Se todos os campos SOAP estiverem vazios.</exception>
    public static EvolucaoSOAP Registrar(
        string? subjetivo,
        string? objetivo,
        string? avaliacao,
        string? plano,
        DateTimeOffset dataHora)
    {
        if (string.IsNullOrWhiteSpace(subjetivo)
            && string.IsNullOrWhiteSpace(objetivo)
            && string.IsNullOrWhiteSpace(avaliacao)
            && string.IsNullOrWhiteSpace(plano))
        {
            throw new ArgumentException("Informe ao menos um campo SOAP.", nameof(subjetivo));
        }

        return new EvolucaoSOAP(
            EvolucaoSOAPId.New(),
            subjetivo ?? string.Empty,
            objetivo ?? string.Empty,
            avaliacao ?? string.Empty,
            plano ?? string.Empty,
            dataHora,
            assinada: false,
            ehAdendo: false,
            evolucaoReferenciadaId: null);
    }

    /// <summary>Cria um adendo (assinado por definicao) que complementa uma evolucao ja assinada.</summary>
    /// <param name="evolucaoReferenciadaId">Evolucao assinada complementada.</param>
    /// <param name="texto">Texto do adendo (registrado no componente Plano).</param>
    /// <param name="dataHora">Data/hora do adendo.</param>
    /// <returns>Nova <see cref="EvolucaoSOAP"/> marcada como adendo e assinada.</returns>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    public static EvolucaoSOAP RegistrarAdendo(Guid evolucaoReferenciadaId, string texto, DateTimeOffset dataHora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        return new EvolucaoSOAP(
            EvolucaoSOAPId.New(),
            subjetivo: string.Empty,
            objetivo: string.Empty,
            avaliacao: string.Empty,
            plano: texto.Trim(),
            dataHora,
            assinada: true,
            ehAdendo: true,
            evolucaoReferenciadaId: evolucaoReferenciadaId);
    }

    /// <summary>Marca a evolucao como assinada (chamado pela raiz ao assinar o atendimento).</summary>
    internal void MarcarAssinada() => Assinada = true;
}
