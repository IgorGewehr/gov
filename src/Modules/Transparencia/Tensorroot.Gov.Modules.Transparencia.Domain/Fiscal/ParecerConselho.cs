using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="ParecerConselho"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ParecerConselhoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ParecerConselhoId"/>.</returns>
    public static ParecerConselhoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Conselho de controle social que emite o parecer.</summary>
public enum TipoConselho
{
    /// <summary>Conselho Municipal de Saúde.</summary>
    Cms = 1,

    /// <summary>Conselho Municipal de Educação.</summary>
    Cme = 2,

    /// <summary>Conselho de Acompanhamento e Controle Social do FUNDEB.</summary>
    CacsFundeb = 3,

    /// <summary>Conselho Municipal de Assistência Social.</summary>
    Cmas = 4,
}

/// <summary>Resultado do parecer do conselho sobre as contas/repasses.</summary>
public enum ResultadoParecer
{
    /// <summary>Aprovado.</summary>
    Aprovado = 1,

    /// <summary>Aprovado com ressalvas.</summary>
    AprovadoComRessalva = 2,

    /// <summary>Rejeitado.</summary>
    Rejeitado = 3,
}

/// <summary>
/// <b>M7.0.2 — ParecerConselho.</b> Registro imutável do parecer de um conselho de controle social
/// (CMS/CME/CACS-FUNDEB/CMAS) sobre as contas/repasses de um setor no exercício: data, número da
/// resolução, resultado (aprovado/ressalva/rejeitado) e observação. Evidência de prestação para o
/// Tribunal de Contas — a trilha de auditoria imutável (hash-chain) e o Outbox vêm de graça da base.
/// </summary>
public sealed class ParecerConselho : AggregateRoot<ParecerConselhoId>, IMustHaveTenant
{
    private ParecerConselho()
    {
    }

    private ParecerConselho(
        ParecerConselhoId id,
        Guid tenantId,
        TipoConselho conselho,
        SetorMinimo setor,
        int exercicio,
        DateOnly dataParecer,
        string numeroResolucao,
        ResultadoParecer resultado,
        string? observacao)
        : base(id)
    {
        TenantId = tenantId;
        Conselho = conselho;
        Setor = setor;
        Exercicio = exercicio;
        DataParecer = dataParecer;
        NumeroResolucao = numeroResolucao;
        Resultado = resultado;
        Observacao = observacao;
    }

    /// <summary>Tenant (município) dono do parecer.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Conselho que emitiu o parecer.</summary>
    public TipoConselho Conselho { get; private set; }

    /// <summary>Setor das contas avaliadas.</summary>
    public SetorMinimo Setor { get; private set; }

    /// <summary>Exercício avaliado.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Data do parecer.</summary>
    public DateOnly DataParecer { get; private set; }

    /// <summary>Número da resolução/ata do conselho.</summary>
    public string NumeroResolucao { get; private set; } = default!;

    /// <summary>Resultado do parecer.</summary>
    public ResultadoParecer Resultado { get; private set; }

    /// <summary>Observação/ressalvas registradas.</summary>
    public string? Observacao { get; private set; }

    /// <summary>Registra o parecer do conselho.</summary>
    /// <param name="tenantId">Tenant dono do parecer.</param>
    /// <param name="conselho">Conselho emissor.</param>
    /// <param name="setor">Setor avaliado.</param>
    /// <param name="exercicio">Exercício avaliado.</param>
    /// <param name="dataParecer">Data do parecer.</param>
    /// <param name="numeroResolucao">Número da resolução.</param>
    /// <param name="resultado">Resultado do parecer.</param>
    /// <param name="observacao">Observação/ressalvas, opcional.</param>
    /// <returns>Novo <see cref="ParecerConselho"/>.</returns>
    public static ParecerConselho Registrar(
        Guid tenantId,
        TipoConselho conselho,
        SetorMinimo setor,
        int exercicio,
        DateOnly dataParecer,
        string numeroResolucao,
        ResultadoParecer resultado,
        string? observacao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroResolucao);
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);

        return new ParecerConselho(
            ParecerConselhoId.New(),
            tenantId,
            conselho,
            setor,
            exercicio,
            dataParecer,
            numeroResolucao.Trim(),
            resultado,
            observacao);
    }
}
