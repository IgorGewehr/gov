using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

/// <summary>Identificador forte do agregado <see cref="Fornecedor"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FornecedorId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FornecedorId"/>.</returns>
    public static FornecedorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Pessoa juridica/fisica apta a contratar com a Administracao, identificada por CNPJ valido,
/// com nivel cadastral no SICAF (art. 87) e historico de sancoes (art. 156). Um fornecedor com
/// sancao impeditiva vigente (impedimento/inidoneidade) nao pode ser habilitado nem contratado
/// (art. 14/156). Raiz de agregado, isolada por tenant (Lei 14.133/2021).
/// </summary>
public sealed class Fornecedor : AggregateRoot<FornecedorId>, IMustHaveTenant
{
    private readonly List<Sancao> _sancoes = [];

    private Fornecedor()
    {
    }

    private Fornecedor(FornecedorId id, Guid tenantId, Cnpj cnpj, string razaoSocial)
        : base(id)
    {
        TenantId = tenantId;
        Cnpj = cnpj;
        RazaoSocial = razaoSocial;
        NivelCadastralSICAF = NivelCadastralSICAF.NaoCadastrado;
        Situacao = SituacaoFornecedor.Ativo;
        RaiseDomainEvent(new FornecedorCadastrado(id, cnpj));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>CNPJ validado (sem mascara).</summary>
    public Cnpj Cnpj { get; private set; } = default!;

    /// <summary>Denominacao do fornecedor.</summary>
    public string RazaoSocial { get; private set; } = default!;

    /// <summary>Nivel cadastral no SICAF (art. 87).</summary>
    public NivelCadastralSICAF NivelCadastralSICAF { get; private set; }

    /// <summary>Situacao cadastral atual.</summary>
    public SituacaoFornecedor Situacao { get; private set; }

    /// <summary>Historico de sancoes administrativas aplicadas.</summary>
    public IReadOnlyCollection<Sancao> Sancoes => _sancoes;

    /// <summary>
    /// Cadastra um novo fornecedor (CNPJ ja validado/consultado na Receita pelo handler).
    /// Nasce em <see cref="SituacaoFornecedor.Ativo"/> com nivel <see cref="NivelCadastralSICAF.NaoCadastrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cnpj">CNPJ validado (Value Object do SharedKernel).</param>
    /// <param name="razaoSocial">Razao social (obrigatoria).</param>
    /// <returns>Novo <see cref="Fornecedor"/> em situacao <c>Ativo</c> (I-5).</returns>
    /// <exception cref="ArgumentNullException">Se o CNPJ for nulo (I-1).</exception>
    /// <exception cref="ArgumentException">Se a razao social for vazia (I-3).</exception>
    public static Fornecedor Cadastrar(Guid tenantId, Cnpj cnpj, string razaoSocial)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        ArgumentException.ThrowIfNullOrWhiteSpace(razaoSocial);
        return new Fornecedor(FornecedorId.New(), tenantId, cnpj, razaoSocial);
    }

    /// <summary>Atualiza o nivel cadastral consultado no SICAF (I-15). Nao altera situacao nem sancoes.</summary>
    /// <param name="nivel">Novo nivel cadastral (valor valido do enum).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o nivel nao for um valor valido do enum.</exception>
    public void AtualizarNivelSicaf(NivelCadastralSICAF nivel)
    {
        if (!Enum.IsDefined(nivel))
        {
            throw new ArgumentOutOfRangeException(nameof(nivel), "Nivel cadastral SICAF invalido.");
        }

        NivelCadastralSICAF = nivel;
    }

    /// <summary>
    /// Aplica uma sancao administrativa (art. 156). Sempre emite <see cref="FornecedorSancionado"/>;
    /// quando o tipo e impeditivo ({Impedimento, Inidoneidade}) e vigente, a situacao passa a
    /// <see cref="SituacaoFornecedor.Sancionado"/> (I-6).
    /// </summary>
    /// <param name="tipo">Tipo da sancao.</param>
    /// <param name="dataInicio">Inicio da vigencia.</param>
    /// <param name="dataFim">Termo final (opcional).</param>
    /// <param name="processoAdministrativo">Processo administrativo (obrigatorio, I-10).</param>
    /// <param name="fundamentacao">Fundamentacao do ato (obrigatorio, I-10).</param>
    /// <param name="valorMulta">Valor da multa (positivo) quando <see cref="TipoSancao.Multa"/> (I-9).</param>
    /// <param name="hoje">Data de referencia para apurar a vigencia impeditiva.</param>
    /// <returns>Identificador da sancao registrada.</returns>
    /// <exception cref="ArgumentException">Se processo/fundamentacao forem vazios (I-10).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a multa exigir valor positivo (I-9) ou DataFim for anterior ao inicio.</exception>
    public SancaoId AplicarSancao(
        TipoSancao tipo,
        DateOnly dataInicio,
        DateOnly? dataFim,
        string processoAdministrativo,
        string fundamentacao,
        ValorMonetario? valorMulta,
        DateOnly hoje)
    {
        var sancao = Sancao.Registrar(tipo, dataInicio, dataFim, processoAdministrativo, fundamentacao, valorMulta);
        _sancoes.Add(sancao);

        // I-6: situacao passa a Sancionado apenas quando a sancao e impeditiva e vigente.
        if (sancao.EImpeditiva && sancao.EstaVigente(hoje) && Situacao != SituacaoFornecedor.Inativo)
        {
            Situacao = SituacaoFornecedor.Sancionado;
        }

        RaiseDomainEvent(new FornecedorSancionado(Id, sancao.Id.Value, tipo, dataInicio, dataFim));
        return sancao.Id;
    }

    /// <summary>
    /// Reabilita o fornecedor apos cumprimento/encerramento das sancoes impeditivas, restabelecendo
    /// a aptidao (volta a <see cref="SituacaoFornecedor.Ativo"/>) (I-11).
    /// </summary>
    /// <param name="hoje">Data de referencia para apurar sancao impeditiva vigente.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver <c>Sancionado</c> ou houver sancao impeditiva vigente (I-11).</exception>
    public void Reabilitar(DateOnly hoje)
    {
        if (Situacao != SituacaoFornecedor.Sancionado)
        {
            throw new InvalidOperationException($"Reabilitacao so e permitida para fornecedor Sancionado. Situacao atual: {Situacao}.");
        }

        if (EstaImpedido(hoje))
        {
            throw new InvalidOperationException("Reabilitacao bloqueada: ha sancao impeditiva vigente.");
        }

        Situacao = SituacaoFornecedor.Ativo;
        RaiseDomainEvent(new FornecedorReabilitado(Id));
    }

    /// <summary>Inativa o cadastro do fornecedor (passa a <see cref="SituacaoFornecedor.Inativo"/>).</summary>
    /// <exception cref="InvalidOperationException">Se o fornecedor ja estiver <c>Inativo</c>.</exception>
    public void Inativar()
    {
        if (Situacao == SituacaoFornecedor.Inativo)
        {
            throw new InvalidOperationException("Fornecedor ja esta Inativo.");
        }

        Situacao = SituacaoFornecedor.Inativo;
        RaiseDomainEvent(new FornecedorInativado(Id));
    }

    /// <summary>
    /// Indica se o fornecedor esta impedido de licitar/contratar na data informada: verdadeiro se
    /// existe ao menos uma sancao impeditiva vigente (I-12).
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se houver sancao impeditiva vigente.</returns>
    public bool EstaImpedido(DateOnly hoje)
        => _sancoes.Any(sancao => sancao.EImpeditiva && sancao.EstaVigente(hoje));
}
