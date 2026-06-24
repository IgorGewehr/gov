using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Identificador forte do agregado <see cref="Ata"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AtaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AtaId"/>.</returns>
    public static AtaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Ata de Registro de Precos (ARP) — documento que registra precos, fornecedores beneficiarios e
/// condicoes para contratacoes futuras, decorrente de licitacao por Sistema de Registro de Precos
/// (art. 82-86, Lei 14.133/2021). Possui vigencia (1 ano + prorrogacao, art. 84), itens com saldo
/// e adesoes (carona, art. 86). Raiz de agregado, isolada por tenant.
/// </summary>
public sealed class Ata : AggregateRoot<AtaId>, IMustHaveTenant
{
    private readonly List<ItemAta> _itens = [];
    private readonly List<Adesao> _adesoes = [];

    private Ata()
    {
    }

    private Ata(
        AtaId id,
        Guid tenantId,
        string numero,
        Guid? licitacaoId,
        DateOnly vigenciaInicio,
        DateOnly vigenciaFim)
        : base(id)
    {
        TenantId = tenantId;
        Numero = numero;
        LicitacaoId = licitacaoId;
        VigenciaInicio = vigenciaInicio;
        VigenciaFim = vigenciaFim;
        Situacao = SituacaoAta.Vigente;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Numero/identificacao da ata (unico por tenant).</summary>
    public string Numero { get; private set; } = default!;

    /// <summary>Licitacao (SRP) que originou a ata, quando aplicavel.</summary>
    public Guid? LicitacaoId { get; private set; }

    /// <summary>Inicio da vigencia.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Termo final da vigencia.</summary>
    public DateOnly VigenciaFim { get; private set; }

    /// <summary>Situacao atual no ciclo de vida.</summary>
    public SituacaoAta Situacao { get; private set; }

    /// <summary>Itens registrados (preco + saldo por item de catalogo).</summary>
    public IReadOnlyCollection<ItemAta> Itens => _itens;

    /// <summary>Adesoes (carona) registradas.</summary>
    public IReadOnlyCollection<Adesao> Adesoes => _adesoes;

    /// <summary>
    /// Cria uma Ata de Registro de Precos (nasce <see cref="SituacaoAta.Vigente"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="numero">Numero/identificacao (obrigatorio).</param>
    /// <param name="licitacaoId">Licitacao SRP de origem (opcional).</param>
    /// <param name="vigenciaInicio">Inicio da vigencia.</param>
    /// <param name="vigenciaFim">Termo final da vigencia.</param>
    /// <returns>Nova <see cref="Ata"/>.</returns>
    /// <exception cref="ArgumentException">Se o numero for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a vigencia final nao for posterior ao inicio.</exception>
    public static Ata Registrar(Guid tenantId, string numero, Guid? licitacaoId, DateOnly vigenciaInicio, DateOnly vigenciaFim)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        if (vigenciaFim <= vigenciaInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(vigenciaFim), "Vigencia final deve ser posterior ao inicio.");
        }

        return new Ata(AtaId.New(), tenantId, numero.Trim(), licitacaoId, vigenciaInicio, vigenciaFim);
    }

    /// <summary>
    /// Adiciona um item registrado a ata vigente. Nao admite o mesmo item de catalogo para o mesmo
    /// fornecedor beneficiario duas vezes (duplicidade de preco registrado).
    /// </summary>
    /// <param name="itemCatalogoId">Item de catalogo a registrar.</param>
    /// <param name="fornecedorBeneficiarioId">Fornecedor beneficiario.</param>
    /// <param name="precoRegistrado">Preco unitario registrado.</param>
    /// <param name="quantidadeRegistrada">Quantidade maxima registrada.</param>
    /// <returns>Identificador do item registrado.</returns>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver Vigente ou o par item/fornecedor ja existir.</exception>
    public ItemAtaId RegistrarItem(
        ItemCatalogoId itemCatalogoId,
        Guid fornecedorBeneficiarioId,
        ValorMonetario precoRegistrado,
        decimal quantidadeRegistrada)
    {
        GarantirVigente();
        if (_itens.Any(i => i.ItemCatalogoId == itemCatalogoId && i.FornecedorBeneficiarioId == fornecedorBeneficiarioId))
        {
            throw new InvalidOperationException("Item ja registrado para este fornecedor nesta ata.");
        }

        var item = ItemAta.Criar(itemCatalogoId, fornecedorBeneficiarioId, precoRegistrado, quantidadeRegistrada);
        _itens.Add(item);
        return item.Id;
    }

    /// <summary>
    /// Registra uma adesao (carona) a um item da ata, debitando o saldo registrado (art. 86).
    /// </summary>
    /// <param name="itemAtaId">Item registrado objeto da adesao.</param>
    /// <param name="orgaoAderente">Orgao/entidade aderente.</param>
    /// <param name="quantidade">Quantidade aderida.</param>
    /// <param name="hoje">Data de referencia (relogio do tenant).</param>
    /// <returns>Identificador da adesao registrada.</returns>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver vigente na data, o item nao existir ou faltar saldo.</exception>
    public AdesaoId RegistrarAdesao(ItemAtaId itemAtaId, string orgaoAderente, decimal quantidade, DateOnly hoje)
    {
        GarantirVigente();
        if (!EstaVigenteEm(hoje))
        {
            throw new InvalidOperationException("Ata fora do periodo de vigencia; adesao nao permitida.");
        }

        var item = _itens.FirstOrDefault(i => i.Id == itemAtaId)
            ?? throw new InvalidOperationException("Item nao pertence a esta ata.");

        item.ConsumirSaldo(quantidade);
        var adesao = Adesao.Registrar(item.ItemCatalogoId, orgaoAderente, quantidade, hoje);
        _adesoes.Add(adesao);
        return adesao.Id;
    }

    /// <summary>
    /// Consome saldo de um item por contratacao direta do proprio ente (uso da ata, sem carona).
    /// </summary>
    /// <param name="itemAtaId">Item registrado.</param>
    /// <param name="quantidade">Quantidade contratada.</param>
    /// <param name="hoje">Data de referencia.</param>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver vigente na data, o item nao existir ou faltar saldo.</exception>
    public void ContratarItem(ItemAtaId itemAtaId, decimal quantidade, DateOnly hoje)
    {
        GarantirVigente();
        if (!EstaVigenteEm(hoje))
        {
            throw new InvalidOperationException("Ata fora do periodo de vigencia; contratacao nao permitida.");
        }

        var item = _itens.FirstOrDefault(i => i.Id == itemAtaId)
            ?? throw new InvalidOperationException("Item nao pertence a esta ata.");
        item.ConsumirSaldo(quantidade);
    }

    /// <summary>Cancela a ata por ato administrativo (art. 85/86), impedindo novas contratacoes/adesoes.</summary>
    /// <param name="motivo">Motivacao do ato.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver Vigente.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirVigente();
        Situacao = SituacaoAta.Cancelada;
    }

    /// <summary>Encerra a ata por decurso do prazo de vigencia.</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver Vigente ou ainda dentro da vigencia.</exception>
    public void Encerrar(DateOnly hoje)
    {
        GarantirVigente();
        if (hoje <= VigenciaFim)
        {
            throw new InvalidOperationException("Ata ainda vigente; encerramento por decurso so apos o termo final.");
        }

        Situacao = SituacaoAta.Encerrada;
    }

    /// <summary>Indica se a ata esta vigente (situacao Vigente e dentro do periodo) na data informada.</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se vigente.</returns>
    public bool EstaVigenteEm(DateOnly hoje)
        => Situacao == SituacaoAta.Vigente && hoje >= VigenciaInicio && hoje <= VigenciaFim;

    private void GarantirVigente()
    {
        if (Situacao != SituacaoAta.Vigente)
        {
            throw new InvalidOperationException($"Operacao exige ata Vigente. Situacao atual: {Situacao}.");
        }
    }
}
