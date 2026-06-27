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
/// (art. 82-86, Lei 14.133/2021; Dec. 11.462/2023). Possui orgao gerenciador, orgaos participantes,
/// vigencia (1 ano + prorrogacao por igual periodo, art. 84), itens com saldo e adesoes por orgao nao
/// participante (carona, art. 86, com limites de 50%/orgao e 200% total). Raiz de agregado, isolada por
/// tenant.
/// </summary>
public sealed class Ata : AggregateRoot<AtaId>, IMustHaveTenant
{
    private readonly List<ItemAta> _itens = [];
    private readonly List<Adesao> _adesoes = [];
    private readonly List<ParticipanteAta> _participantes = [];

    private Ata()
    {
    }

    private Ata(
        AtaId id,
        Guid tenantId,
        string numero,
        Guid? licitacaoId,
        string cnpjOrgaoGerenciador,
        string nomeOrgaoGerenciador,
        DateOnly vigenciaInicio,
        DateOnly vigenciaFim)
        : base(id)
    {
        TenantId = tenantId;
        Numero = numero;
        LicitacaoId = licitacaoId;
        CnpjOrgaoGerenciador = cnpjOrgaoGerenciador;
        NomeOrgaoGerenciador = nomeOrgaoGerenciador;
        VigenciaInicio = vigenciaInicio;
        VigenciaFim = vigenciaFim;
        VigenciaInicioOriginal = vigenciaInicio;
        Situacao = SituacaoAta.Vigente;
        _participantes.Add(ParticipanteAta.Registrar(cnpjOrgaoGerenciador, nomeOrgaoGerenciador, TipoOrgaoSrp.Gerenciador));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Numero/identificacao da ata (unico por tenant).</summary>
    public string Numero { get; private set; } = default!;

    /// <summary>Licitacao (SRP) que originou a ata, quando aplicavel.</summary>
    public Guid? LicitacaoId { get; private set; }

    /// <summary>CNPJ do orgao GERENCIADOR da ata (art. 86, caput).</summary>
    public string CnpjOrgaoGerenciador { get; private set; } = default!;

    /// <summary>Nome do orgao GERENCIADOR da ata.</summary>
    public string NomeOrgaoGerenciador { get; private set; } = default!;

    /// <summary>Inicio da vigencia (apos prorrogacao, permanece o inicio original — ver <see cref="VigenciaInicioOriginal"/>).</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Inicio original da vigencia (marco do teto de 24 meses do art. 84).</summary>
    public DateOnly VigenciaInicioOriginal { get; private set; }

    /// <summary>Termo final da vigencia.</summary>
    public DateOnly VigenciaFim { get; private set; }

    /// <summary>Indica se a ata ja foi prorrogada (art. 84 admite UMA prorrogacao por igual periodo).</summary>
    public bool Prorrogada { get; private set; }

    /// <summary>Situacao atual no ciclo de vida.</summary>
    public SituacaoAta Situacao { get; private set; }

    /// <summary>Itens registrados (preco + saldo por item de catalogo).</summary>
    public IReadOnlyCollection<ItemAta> Itens => _itens;

    /// <summary>Adesoes (carona) registradas.</summary>
    public IReadOnlyCollection<Adesao> Adesoes => _adesoes;

    /// <summary>Orgaos gerenciador e participantes da ata (art. 86, §1º).</summary>
    public IReadOnlyCollection<ParticipanteAta> Participantes => _participantes;

    /// <summary>
    /// Cria uma Ata de Registro de Precos com orgao gerenciador (nasce <see cref="SituacaoAta.Vigente"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="numero">Numero/identificacao (obrigatorio).</param>
    /// <param name="licitacaoId">Licitacao SRP de origem (opcional).</param>
    /// <param name="cnpjOrgaoGerenciador">CNPJ do orgao gerenciador (obrigatorio).</param>
    /// <param name="nomeOrgaoGerenciador">Nome do orgao gerenciador (obrigatorio).</param>
    /// <param name="vigenciaInicio">Inicio da vigencia.</param>
    /// <param name="vigenciaFim">Termo final da vigencia.</param>
    /// <returns>Nova <see cref="Ata"/>.</returns>
    /// <exception cref="ArgumentException">Se o numero ou os dados do gerenciador forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a vigencia final nao for posterior ao inicio.</exception>
    public static Ata Registrar(
        Guid tenantId,
        string numero,
        Guid? licitacaoId,
        string cnpjOrgaoGerenciador,
        string nomeOrgaoGerenciador,
        DateOnly vigenciaInicio,
        DateOnly vigenciaFim)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjOrgaoGerenciador);
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeOrgaoGerenciador);
        if (vigenciaFim <= vigenciaInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(vigenciaFim), "Vigencia final deve ser posterior ao inicio.");
        }

        return new Ata(
            AtaId.New(),
            tenantId,
            numero.Trim(),
            licitacaoId,
            cnpjOrgaoGerenciador.Trim(),
            nomeOrgaoGerenciador.Trim(),
            vigenciaInicio,
            vigenciaFim);
    }

    /// <summary>
    /// Inclui um orgao PARTICIPANTE na ata vigente (art. 86, §1º). Diferente da adesao (carona), o
    /// participante integrou o planejamento; nao admite duplicidade de CNPJ.
    /// </summary>
    /// <param name="cnpjOrgao">CNPJ do orgao participante.</param>
    /// <param name="nomeOrgao">Nome do orgao participante.</param>
    /// <returns>Identificador do participante incluido.</returns>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver Vigente ou o CNPJ ja constar.</exception>
    public ParticipanteAtaId IncluirParticipante(string cnpjOrgao, string nomeOrgao)
    {
        GarantirVigente();
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjOrgao);
        var cnpj = cnpjOrgao.Trim();
        if (_participantes.Any(p => string.Equals(p.CnpjOrgao, cnpj, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Orgao com este CNPJ ja consta na ata (gerenciador ou participante).");
        }

        var participante = ParticipanteAta.Registrar(cnpj, nomeOrgao, TipoOrgaoSrp.Participante);
        _participantes.Add(participante);
        return participante.Id;
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
    /// Registra uma adesao (carona) de orgao NAO participante a um item da ata, validando os limites
    /// legais de adesao (art. 86, §§ 4º e 5º): 50% do registrado por orgao aderente (acumulado pelo CNPJ)
    /// e 200% do registrado no total de adesoes do item.
    /// </summary>
    /// <param name="itemAtaId">Item registrado objeto da adesao.</param>
    /// <param name="cnpjOrgaoAderente">CNPJ do orgao aderente (apura o teto de 50% por orgao).</param>
    /// <param name="nomeOrgaoAderente">Nome do orgao aderente.</param>
    /// <param name="quantidade">Quantidade aderida.</param>
    /// <param name="hoje">Data de referencia (relogio do tenant).</param>
    /// <returns>Identificador da adesao registrada.</returns>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver vigente, o item nao existir, o orgao for participante ou faltar saldo/limite.</exception>
    public AdesaoId RegistrarAdesao(
        ItemAtaId itemAtaId,
        string cnpjOrgaoAderente,
        string nomeOrgaoAderente,
        decimal quantidade,
        DateOnly hoje)
    {
        GarantirVigente();
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjOrgaoAderente);
        if (!EstaVigenteEm(hoje))
        {
            throw new InvalidOperationException("Ata fora do periodo de vigencia; adesao nao permitida.");
        }

        var cnpj = cnpjOrgaoAderente.Trim();

        // Um orgao gerenciador/participante NAO adere (carona); ele usa o saldo registrado via contratacao
        // direta. Cobrar adesao de quem participou inverteria o regime de limites (art. 86, §1º vs §§ 4º/5º).
        if (_participantes.Any(p => string.Equals(p.CnpjOrgao, cnpj, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Orgao gerenciador/participante usa o saldo registrado (contratacao direta), nao adesao (carona).");
        }

        var item = _itens.FirstOrDefault(i => i.Id == itemAtaId)
            ?? throw new InvalidOperationException("Item nao pertence a esta ata.");

        // Teto de 50% por orgao e apurado pelo ACUMULADO ja aderido por este CNPJ no mesmo item da ata.
        var jaAderidoPeloOrgao = _adesoes
            .Where(a => a.ItemAtaId == itemAtaId && string.Equals(a.CnpjOrgaoAderente, cnpj, StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.Quantidade);

        item.ConsumirAdesao(quantidade, jaAderidoPeloOrgao);
        var adesao = Adesao.Registrar(item.Id, item.ItemCatalogoId, cnpj, nomeOrgaoAderente, quantidade, hoje);
        _adesoes.Add(adesao);
        return adesao.Id;
    }

    /// <summary>
    /// Consome saldo registrado de um item por contratacao direta do gerenciador/participante (uso da ata,
    /// sem carona).
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

    /// <summary>
    /// Remaneja (ajusta) o quantitativo registrado de um item da ata vigente (Dec. 11.462/2023, art. 33).
    /// O novo quantitativo nao pode ficar abaixo do ja contratado.
    /// </summary>
    /// <param name="itemAtaId">Item a remanejar.</param>
    /// <param name="novaQuantidadeRegistrada">Novo quantitativo registrado.</param>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver Vigente, o item nao existir ou o remanejamento for invalido.</exception>
    public void RemanejarItem(ItemAtaId itemAtaId, decimal novaQuantidadeRegistrada)
    {
        GarantirVigente();
        var item = _itens.FirstOrDefault(i => i.Id == itemAtaId)
            ?? throw new InvalidOperationException("Item nao pertence a esta ata.");
        item.Remanejar(novaQuantidadeRegistrada);
    }

    /// <summary>
    /// Prorroga a vigencia da ata (art. 84): admite UMA prorrogacao, por periodo igual ao original, com
    /// teto de 24 meses total. Exige pesquisa que comprove a vantajosidade do preco registrado (registrada
    /// na justificativa do termo aditivo de prorrogacao).
    /// </summary>
    /// <param name="novaVigenciaFim">Novo termo final de vigencia (posterior ao atual).</param>
    /// <param name="vantajosidadeComprovada">Atesta a comprovacao da vantajosidade dos precos (art. 84).</param>
    /// <exception cref="InvalidOperationException">Se a ata nao estiver Vigente, ja tiver sido prorrogada, a vantajosidade nao for comprovada, a nova vigencia for invalida ou exceder o teto de igual periodo.</exception>
    public void Prorrogar(DateOnly novaVigenciaFim, bool vantajosidadeComprovada)
    {
        GarantirVigente();
        if (Prorrogada)
        {
            throw new InvalidOperationException("Ata ja prorrogada; o art. 84 admite uma unica prorrogacao por igual periodo.");
        }

        if (!vantajosidadeComprovada)
        {
            throw new InvalidOperationException("Prorrogacao exige pesquisa que comprove a vantajosidade do preco registrado (art. 84).");
        }

        if (novaVigenciaFim <= VigenciaFim)
        {
            throw new InvalidOperationException($"Nova vigencia deve ser posterior a atual ({VigenciaFim:O}). Informado: {novaVigenciaFim:O}.");
        }

        // Teto do art. 84: a prorrogacao e por ATE igual periodo. O total nao pode exceder o dobro da
        // vigencia original (vigenciaFim original = inicio + periodo; teto = inicio + 2*periodo).
        var periodoOriginalDias = VigenciaFim.DayNumber - VigenciaInicioOriginal.DayNumber;
        var tetoFinal = VigenciaInicioOriginal.AddDays(periodoOriginalDias * 2);
        if (novaVigenciaFim > tetoFinal)
        {
            throw new InvalidOperationException(
                $"Prorrogacao limitada a igual periodo (art. 84): teto {tetoFinal:O}. Informado: {novaVigenciaFim:O}.");
        }

        VigenciaFim = novaVigenciaFim;
        Prorrogada = true;
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
