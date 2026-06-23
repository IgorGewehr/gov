using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;

/// <summary>Identificador forte de <see cref="UnidadeSocioassistencial"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct UnidadeSocioassistencialId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="UnidadeSocioassistencialId"/>.</returns>
    public static UnidadeSocioassistencialId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de <see cref="ServicoOfertado"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ServicoOfertadoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ServicoOfertadoId"/>.</returns>
    public static ServicoOfertadoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Servico socioassistencial ofertado por uma unidade (Tipificacao Nacional — Res. CNAS 109/2009).
/// Entidade-filha (owned) do agregado <see cref="UnidadeSocioassistencial"/>: nao implementa
/// <c>IMustHaveTenant</c> — isolamento herdado do dono via FK.
/// </summary>
public sealed class ServicoOfertado : Entity<ServicoOfertadoId>
{
    private ServicoOfertado()
    {
    }

    private ServicoOfertado(ServicoOfertadoId id, UnidadeSocioassistencialId unidadeId, TipoServico servico, int capacidadeMensal)
        : base(id)
    {
        UnidadeId = unidadeId;
        Servico = servico;
        CapacidadeMensal = capacidadeMensal;
    }

    /// <summary>Unidade a qual o servico pertence.</summary>
    public UnidadeSocioassistencialId UnidadeId { get; private set; }

    /// <summary>Servico tipificado ofertado (PAIF/PAEFI/SCFV).</summary>
    public TipoServico Servico { get; private set; }

    /// <summary>Capacidade mensal de atendimento referenciada para o servico (&gt;= 0).</summary>
    public int CapacidadeMensal { get; private set; }

    internal static ServicoOfertado Criar(UnidadeSocioassistencialId unidadeId, TipoServico servico, int capacidadeMensal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacidadeMensal);
        return new ServicoOfertado(ServicoOfertadoId.New(), unidadeId, servico, capacidadeMensal);
    }

    internal void AtualizarCapacidade(int capacidadeMensal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacidadeMensal);
        CapacidadeMensal = capacidadeMensal;
    }
}

/// <summary>
/// <b>3d.2 — Unidade socioassistencial (CRAS/CREAS/Centro POP).</b> Promove a Unidade de Atendimento
/// — hoje apenas um GUID em <c>Familia</c>/RMA/Prontuario — a CADASTRO de gestao para o Censo SUAS:
/// estrutura (endereco), recursos humanos (equipe de referencia — NOB-RH/SUAS) e servicos ofertados
/// com capacidade. Raiz de agregado; os <see cref="ServicoOfertado"/> sao suas entidades-filhas.
/// O <c>Id</c> reusa o <c>UnidadeAtendimentoId</c> ja referenciado por Familia/RMA/Prontuario
/// (mesma identidade) para amarrar o consolidado do Censo aos volumes do RMA por Id.
/// // TODO(M10): envio do Censo SUAS ao SAGI/MDS (Resolucao do questionario anual) — requer credencial MDS.
/// </summary>
public sealed class UnidadeSocioassistencial : AggregateRoot<UnidadeSocioassistencialId>, IMustHaveTenant
{
    private readonly List<ServicoOfertado> _servicos = [];

    private UnidadeSocioassistencial()
    {
    }

    private UnidadeSocioassistencial(
        UnidadeSocioassistencialId id,
        Guid tenantId,
        string nome,
        TipoUnidadeAtendimento tipo,
        string territorioCobertura,
        string endereco)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Tipo = tipo;
        TerritorioCobertura = territorioCobertura;
        Endereco = endereco;
        QuantidadeProfissionais = 0;
    }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome da unidade (ex.: "CRAS Centro").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Tipo da unidade (CRAS/CREAS/Centro POP) — define os servicos esperados.</summary>
    public TipoUnidadeAtendimento Tipo { get; private set; }

    /// <summary>Territorio coberto pela unidade (chave de pertencimento da familia).</summary>
    public string TerritorioCobertura { get; private set; } = default!;

    /// <summary>Endereco da unidade.</summary>
    public string Endereco { get; private set; } = default!;

    /// <summary>Tamanho da equipe de referencia (NOB-RH/SUAS) — RH da unidade (&gt;= 0).</summary>
    public int QuantidadeProfissionais { get; private set; }

    /// <summary>Servicos tipificados ofertados pela unidade (entidades-filhas).</summary>
    public IReadOnlyCollection<ServicoOfertado> Servicos => _servicos.AsReadOnly();

    /// <summary>
    /// Cadastra uma unidade socioassistencial (sem servicos; ofertados sob demanda). Valida a
    /// compatibilidade servico↔unidade no momento de ofertar.
    /// </summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="nome">Nome da unidade.</param>
    /// <param name="tipo">Tipo (CRAS/CREAS/Centro POP).</param>
    /// <param name="territorioCobertura">Territorio coberto.</param>
    /// <param name="endereco">Endereco da unidade.</param>
    /// <returns>Nova <see cref="UnidadeSocioassistencial"/>.</returns>
    public static UnidadeSocioassistencial Cadastrar(
        Guid tenantId,
        string nome,
        TipoUnidadeAtendimento tipo,
        string territorioCobertura,
        string endereco)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(territorioCobertura);
        ArgumentException.ThrowIfNullOrWhiteSpace(endereco);
        return new UnidadeSocioassistencial(UnidadeSocioassistencialId.New(), tenantId, nome.Trim(), tipo, territorioCobertura.Trim(), endereco.Trim());
    }

    /// <summary>Define o tamanho da equipe de referencia (RH da unidade para o Censo).</summary>
    /// <param name="quantidadeProfissionais">Numero de profissionais da equipe (&gt;= 0).</param>
    public void DefinirEquipe(int quantidadeProfissionais)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidadeProfissionais);
        QuantidadeProfissionais = quantidadeProfissionais;
    }

    /// <summary>
    /// Oferta (ou atualiza a capacidade de) um servico tipificado, validando a compatibilidade
    /// servico↔unidade (C-1: PAIF so em CRAS; PAEFI so em CREAS).
    /// </summary>
    /// <param name="servico">Servico tipificado.</param>
    /// <param name="capacidadeMensal">Capacidade mensal de atendimento (&gt;= 0).</param>
    /// <exception cref="InvalidOperationException">Se o servico for incompativel com o tipo da unidade.</exception>
    public void OfertarServico(TipoServico servico, int capacidadeMensal)
    {
        GarantirServicoCompativel(servico);

        var existente = _servicos.Find(s => s.Servico == servico);
        if (existente is null)
        {
            _servicos.Add(ServicoOfertado.Criar(Id, servico, capacidadeMensal));
        }
        else
        {
            existente.AtualizarCapacidade(capacidadeMensal);
        }
    }

    private void GarantirServicoCompativel(TipoServico servico)
    {
        // C-1: PAIF e exclusivo do CRAS; PAEFI e exclusivo do CREAS (Tipificacao Nacional).
        var compativel = servico switch
        {
            TipoServico.Paif => Tipo == TipoUnidadeAtendimento.Cras,
            TipoServico.Paefi => Tipo == TipoUnidadeAtendimento.Creas,
            TipoServico.Scfv => true,
            _ => false,
        };

        if (!compativel)
        {
            throw new InvalidOperationException($"Servico {servico} incompativel com unidade do tipo {Tipo}.");
        }
    }
}
