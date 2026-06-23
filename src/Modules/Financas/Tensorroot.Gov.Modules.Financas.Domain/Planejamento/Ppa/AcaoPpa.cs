using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

/// <summary>
/// Ação do PPA (Projeto/Atividade/Operação Especial) dentro de um <see cref="Programa"/>.
/// É a unidade que a LDO prioriza e a LOA detalha em despesa fixada (vínculo da cadeia).
/// Entidade-filha do agregado <see cref="PlanoPlurianual"/>.
/// </summary>
public sealed class AcaoPpa : Entity<AcaoPpaId>
{
    private readonly List<MetaAcao> _metas = [];

    private AcaoPpa()
    {
    }

    private AcaoPpa(
        AcaoPpaId id,
        ProgramaId programaId,
        string codigo,
        string nome,
        TipoAcao tipo,
        string funcionalProgramatica,
        string produto,
        string unidadeMedida)
        : base(id)
    {
        ProgramaId = programaId;
        Codigo = codigo;
        Nome = nome;
        Tipo = tipo;
        FuncionalProgramatica = funcionalProgramatica;
        Produto = produto;
        UnidadeMedida = unidadeMedida;
    }

    /// <summary>Programa ao qual a ação pertence.</summary>
    public ProgramaId ProgramaId { get; private set; }

    /// <summary>Código da ação (ex.: "2010").</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Nome/título da ação.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Tipo de ação.</summary>
    public TipoAcao Tipo { get; private set; }

    /// <summary>
    /// Funcional-programática (função/subfunção/programa/ação) que liga à
    /// <see cref="ClassificacaoOrcamentaria.FuncionalProgramatica"/> da LOA.
    /// </summary>
    // TODO(validar-oficial): validacao fina dos digitos da funcional-programatica contra a
    // tabela vigente de funcoes/subfuncoes (STN) — hoje string livre, igual a execucao.
    public string FuncionalProgramatica { get; private set; } = default!;

    /// <summary>Produto da ação (bem/serviço entregue).</summary>
    public string Produto { get; private set; } = default!;

    /// <summary>Unidade de medida do produto.</summary>
    public string UnidadeMedida { get; private set; } = default!;

    /// <summary>Metas físicas/financeiras por ano do quadriênio.</summary>
    public IReadOnlyCollection<MetaAcao> Metas => _metas.AsReadOnly();

    /// <summary>Cria uma ação válida.</summary>
    /// <param name="programaId">Programa dono da ação.</param>
    /// <param name="codigo">Código da ação.</param>
    /// <param name="nome">Nome da ação.</param>
    /// <param name="tipo">Tipo de ação.</param>
    /// <param name="funcionalProgramatica">Funcional-programática.</param>
    /// <param name="produto">Produto.</param>
    /// <param name="unidadeMedida">Unidade de medida do produto.</param>
    /// <returns>Nova <see cref="AcaoPpa"/>.</returns>
    /// <exception cref="ArgumentException">Se o tipo de ação for inválido ou campos vazios.</exception>
    internal static AcaoPpa Criar(
        ProgramaId programaId,
        string codigo,
        string nome,
        TipoAcao tipo,
        string funcionalProgramatica,
        string produto,
        string unidadeMedida)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(funcionalProgramatica);
        ArgumentException.ThrowIfNullOrWhiteSpace(produto);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeMedida);

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de acao invalido.", nameof(tipo));
        }

        return new AcaoPpa(
            AcaoPpaId.New(),
            programaId,
            codigo.Trim(),
            nome.Trim(),
            tipo,
            funcionalProgramatica.Trim(),
            produto.Trim(),
            unidadeMedida.Trim());
    }

    /// <summary>Define (cria ou atualiza) a meta de um ano do quadriênio.</summary>
    /// <param name="ano">Ano do quadriênio.</param>
    /// <param name="metaFisica">Meta física.</param>
    /// <param name="unidadeMedida">Unidade de medida da meta física.</param>
    /// <param name="metaFinanceira">Meta financeira.</param>
    /// <param name="regiao">Região.</param>
    internal void DefinirMeta(
        int ano,
        decimal metaFisica,
        string unidadeMedida,
        ValorMonetario metaFinanceira,
        string regiao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regiao);
        var existente = _metas.Find(m => m.Ano == ano && string.Equals(m.Regiao, regiao.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existente is not null)
        {
            existente.Atualizar(metaFisica, unidadeMedida, metaFinanceira, regiao);
            return;
        }

        _metas.Add(MetaAcao.Criar(Id, ano, metaFisica, unidadeMedida, metaFinanceira, regiao));
    }

    /// <summary>Indica se a ação possui ao menos uma meta definida.</summary>
    /// <returns><c>true</c> se houver meta.</returns>
    internal bool PossuiMeta() => _metas.Count > 0;
}
