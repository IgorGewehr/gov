using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

/// <summary>
/// Programa do PPA: agrupa ações orientadas a um objetivo, com público-alvo e indicador
/// (linha-base/meta). Entidade-filha do agregado <see cref="PlanoPlurianual"/>.
/// </summary>
public sealed class Programa : Entity<ProgramaId>
{
    private readonly List<AcaoPpa> _acoes = [];

    private Programa()
    {
    }

    private Programa(
        ProgramaId id,
        PpaId ppaId,
        string codigo,
        string nome,
        string objetivo,
        string publicoAlvo,
        string indicador,
        decimal indicadorLinhaBase,
        decimal indicadorMeta)
        : base(id)
    {
        PpaId = ppaId;
        Codigo = codigo;
        Nome = nome;
        Objetivo = objetivo;
        PublicoAlvo = publicoAlvo;
        Indicador = indicador;
        IndicadorLinhaBase = indicadorLinhaBase;
        IndicadorMeta = indicadorMeta;
    }

    /// <summary>PPA ao qual o programa pertence.</summary>
    public PpaId PpaId { get; private set; }

    /// <summary>Código do programa (ex.: "0002").</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Nome do programa.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Objetivo do programa.</summary>
    public string Objetivo { get; private set; } = default!;

    /// <summary>Público-alvo.</summary>
    public string PublicoAlvo { get; private set; } = default!;

    /// <summary>Indicador de resultado (nome/descrição).</summary>
    public string Indicador { get; private set; } = default!;

    /// <summary>Linha-base do indicador.</summary>
    public decimal IndicadorLinhaBase { get; private set; }

    /// <summary>Meta do indicador ao fim do quadriênio.</summary>
    public decimal IndicadorMeta { get; private set; }

    /// <summary>Ações do programa.</summary>
    public IReadOnlyCollection<AcaoPpa> Acoes => _acoes.AsReadOnly();

    /// <summary>Cria um programa válido.</summary>
    /// <param name="ppaId">PPA dono do programa.</param>
    /// <param name="codigo">Código.</param>
    /// <param name="nome">Nome.</param>
    /// <param name="objetivo">Objetivo.</param>
    /// <param name="publicoAlvo">Público-alvo.</param>
    /// <param name="indicador">Indicador.</param>
    /// <param name="indicadorLinhaBase">Linha-base do indicador.</param>
    /// <param name="indicadorMeta">Meta do indicador.</param>
    /// <returns>Novo <see cref="Programa"/>.</returns>
    internal static Programa Criar(
        PpaId ppaId,
        string codigo,
        string nome,
        string objetivo,
        string publicoAlvo,
        string indicador,
        decimal indicadorLinhaBase,
        decimal indicadorMeta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(objetivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicoAlvo);
        ArgumentException.ThrowIfNullOrWhiteSpace(indicador);

        return new Programa(
            ProgramaId.New(),
            ppaId,
            codigo.Trim(),
            nome.Trim(),
            objetivo.Trim(),
            publicoAlvo.Trim(),
            indicador.Trim(),
            indicadorLinhaBase,
            indicadorMeta);
    }

    /// <summary>Adiciona uma ação ao programa (código único dentro do programa).</summary>
    /// <param name="codigo">Código da ação.</param>
    /// <param name="nome">Nome.</param>
    /// <param name="tipo">Tipo de ação.</param>
    /// <param name="funcionalProgramatica">Funcional-programática.</param>
    /// <param name="produto">Produto.</param>
    /// <param name="unidadeMedida">Unidade de medida.</param>
    /// <returns>A ação criada.</returns>
    /// <exception cref="InvalidOperationException">Se o código já existir no programa.</exception>
    internal AcaoPpa AdicionarAcao(
        string codigo,
        string nome,
        TipoAcao tipo,
        string funcionalProgramatica,
        string produto,
        string unidadeMedida)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        if (_acoes.Exists(a => string.Equals(a.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Acao com codigo {codigo} ja existe no programa {Codigo}.");
        }

        var acao = AcaoPpa.Criar(Id, codigo, nome, tipo, funcionalProgramatica, produto, unidadeMedida);
        _acoes.Add(acao);
        return acao;
    }

    /// <summary>Localiza uma ação por identificador dentro do programa.</summary>
    /// <param name="acaoId">Identificador da ação.</param>
    /// <returns>A ação, ou <c>null</c>.</returns>
    internal AcaoPpa? LocalizarAcao(AcaoPpaId acaoId) => _acoes.Find(a => a.Id == acaoId);

    /// <summary>Indica se o programa tem ao menos uma ação com ao menos uma meta.</summary>
    /// <returns><c>true</c> se houver ação com meta.</returns>
    internal bool PossuiAcaoComMeta() => _acoes.Exists(a => a.PossuiMeta());
}
