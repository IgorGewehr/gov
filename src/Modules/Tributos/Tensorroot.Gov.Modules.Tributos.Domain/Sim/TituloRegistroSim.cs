using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Sim;

/// <summary>Identificador forte do agregado <see cref="TituloRegistroSim"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TituloRegistroSimId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TituloRegistroSimId"/>.</returns>
    public static TituloRegistroSimId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Natureza do produto inspecionado pelo Serviço de Inspeção Municipal.</summary>
public enum NaturezaProdutoSim
{
    /// <summary>Produto de origem animal (carnes, leite, ovos, mel, pescado — DECRETO 9.013/2017 RIISPOA).</summary>
    OrigemAnimal = 1,

    /// <summary>Produto de origem vegetal (conservas, polpas, panificados artesanais).</summary>
    OrigemVegetal = 2,
}

/// <summary>Situação (estado) do título de registro no S.I.M.</summary>
public enum SituacaoTituloSim
{
    /// <summary>Em análise (requerimento protocolado; vistoria/documentação em curso).</summary>
    EmAnalise = 1,

    /// <summary>Registrado (título concedido; número do S.I.M. atribuído; apto a produzir/comercializar).</summary>
    Registrado = 2,

    /// <summary>Suspenso (irregularidade sanitária — comercialização vedada até regularização).</summary>
    Suspenso = 3,

    /// <summary>Cassado/cancelado (registro extinto).</summary>
    Cassado = 4,
}

/// <summary>
/// Título de registro de estabelecimento no Serviço de Inspeção Municipal (S.I.M.): ato de polícia
/// sanitária que habilita o estabelecimento a industrializar e comercializar produtos de origem animal
/// (Lei 7.889/1989; Decreto 9.013/2017 — RIISPOA; Lei 13.680/2018 e Decreto 9.918/2019 — SISBI/SUASA)
/// ou vegetal, no âmbito MUNICIPAL. Atribui o NÚMERO DO S.I.M. ao registrar, vincula os produtos
/// inspecionados (com rótulo) e controla a vigência. A taxa de inspeção/licença (TLL) correlata é
/// lançada à parte como Taxa (poder de polícia — CTN art. 77). Paridade com o incumbente SAPI. Domínio
/// rico: o agregado protege seus invariantes (CLAUDE.md §7).
/// </summary>
public sealed class TituloRegistroSim : AggregateRoot<TituloRegistroSimId>, IMustHaveTenant
{
    private readonly List<ProdutoInspecionadoSim> _produtos = [];

    private TituloRegistroSim()
    {
    }

    private TituloRegistroSim(
        TituloRegistroSimId id,
        Guid tenantId,
        ContribuinteId responsavelId,
        string razaoSocialEstabelecimento,
        NaturezaProdutoSim natureza,
        string enderecoEstabelecimento,
        DateOnly dataRequerimento)
        : base(id)
    {
        TenantId = tenantId;
        ResponsavelId = responsavelId;
        RazaoSocialEstabelecimento = razaoSocialEstabelecimento;
        Natureza = natureza;
        EnderecoEstabelecimento = enderecoEstabelecimento;
        DataRequerimento = dataRequerimento;
        Situacao = SituacaoTituloSim.EmAnalise;
        RaiseDomainEvent(new TituloRegistroSimRequerido(id, tenantId, responsavelId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte responsável legal pelo estabelecimento.</summary>
    public ContribuinteId ResponsavelId { get; private set; }

    /// <summary>Razão social/nome do estabelecimento inspecionado.</summary>
    public string RazaoSocialEstabelecimento { get; private set; } = default!;

    /// <summary>Natureza do produto inspecionado (origem animal/vegetal).</summary>
    public NaturezaProdutoSim Natureza { get; private set; }

    /// <summary>Endereço do estabelecimento (escopo municipal da inspeção).</summary>
    public string EnderecoEstabelecimento { get; private set; } = default!;

    /// <summary>Data do requerimento de registro.</summary>
    public DateOnly DataRequerimento { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoTituloSim Situacao { get; private set; }

    /// <summary>Número do S.I.M. atribuído na concessão do registro (nulo enquanto em análise).</summary>
    public string? NumeroSim { get; private set; }

    /// <summary>Data da concessão do registro (nula enquanto não registrado).</summary>
    public DateOnly? DataRegistro { get; private set; }

    /// <summary>Fim da vigência do título (renovável); nulo enquanto não registrado.</summary>
    public DateOnly? FimVigencia { get; private set; }

    /// <summary>Produtos inspecionados habilitados sob este título (com rótulo).</summary>
    public IReadOnlyCollection<ProdutoInspecionadoSim> Produtos => _produtos;

    /// <summary>Quantidade de produtos habilitados.</summary>
    public int QuantidadeProdutos => _produtos.Count;

    /// <summary>Protocola o requerimento de registro de um estabelecimento no S.I.M. (entra em análise).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="responsavelId">Contribuinte responsável legal.</param>
    /// <param name="razaoSocialEstabelecimento">Razão social/nome do estabelecimento.</param>
    /// <param name="natureza">Natureza do produto (origem animal/vegetal).</param>
    /// <param name="enderecoEstabelecimento">Endereço do estabelecimento.</param>
    /// <param name="dataRequerimento">Data do requerimento.</param>
    /// <returns>Novo <see cref="TituloRegistroSim"/> em análise.</returns>
    public static TituloRegistroSim Requerer(
        Guid tenantId,
        ContribuinteId responsavelId,
        string razaoSocialEstabelecimento,
        NaturezaProdutoSim natureza,
        string enderecoEstabelecimento,
        DateOnly dataRequerimento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(razaoSocialEstabelecimento);
        ArgumentException.ThrowIfNullOrWhiteSpace(enderecoEstabelecimento);
        if (!Enum.IsDefined(natureza))
        {
            throw new ArgumentOutOfRangeException(nameof(natureza), natureza, "Natureza do produto inválida.");
        }

        return new TituloRegistroSim(
            TituloRegistroSimId.New(),
            tenantId,
            responsavelId,
            razaoSocialEstabelecimento.Trim(),
            natureza,
            enderecoEstabelecimento.Trim(),
            dataRequerimento);
    }

    /// <summary>
    /// Concede o registro: atribui o NÚMERO DO S.I.M., habilita o estabelecimento e fixa a vigência.
    /// Exige ao menos um produto inspecionado habilitado (não se registra estabelecimento sem produto).
    /// </summary>
    /// <param name="numeroSim">Número do S.I.M. atribuído (sequência do tenant).</param>
    /// <param name="dataRegistro">Data da concessão.</param>
    /// <param name="fimVigencia">Fim da vigência do título.</param>
    /// <exception cref="InvalidOperationException">Se não estiver em análise ou não houver produto habilitado.</exception>
    public void Conceder(string numeroSim, DateOnly dataRegistro, DateOnly fimVigencia)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroSim);
        if (Situacao != SituacaoTituloSim.EmAnalise)
        {
            throw new InvalidOperationException($"Só é possível conceder o registro de um título em análise. Situação atual: {Situacao}.");
        }

        if (_produtos.Count == 0)
        {
            throw new InvalidOperationException("Não se concede registro no S.I.M. sem ao menos um produto inspecionado habilitado.");
        }

        if (fimVigencia <= dataRegistro)
        {
            throw new ArgumentOutOfRangeException(nameof(fimVigencia), "O fim da vigência deve ser posterior à data de registro.");
        }

        NumeroSim = numeroSim.Trim();
        DataRegistro = dataRegistro;
        FimVigencia = fimVigencia;
        Situacao = SituacaoTituloSim.Registrado;
        RaiseDomainEvent(new TituloRegistroSimConcedido(Id, TenantId, ResponsavelId, NumeroSim));
    }

    /// <summary>
    /// Habilita um produto inspecionado sob o título (com rótulo aprovado). Permitido em análise (compor
    /// o requerimento) e após registrado (inclusão de novo produto à linha já habilitada).
    /// </summary>
    /// <param name="denominacao">Denominação de venda do produto (ex.: "Queijo Colonial").</param>
    /// <param name="classificacao">Classificação do produto (categoria sanitária).</param>
    /// <param name="numeroRotulo">Número do rótulo aprovado (controle de rotulagem).</param>
    /// <exception cref="InvalidOperationException">Se o título estiver suspenso ou cassado.</exception>
    public void HabilitarProduto(string denominacao, string classificacao, string numeroRotulo)
    {
        if (Situacao is SituacaoTituloSim.Suspenso or SituacaoTituloSim.Cassado)
        {
            throw new InvalidOperationException($"Não é possível habilitar produtos num título {Situacao}.");
        }

        _produtos.Add(ProdutoInspecionadoSim.Criar(Id, denominacao, classificacao, numeroRotulo));
    }

    /// <summary>
    /// Suspende o título por irregularidade sanitária (comercialização vedada até regularização).
    /// </summary>
    /// <param name="motivo">Motivo da suspensão (auditoria).</param>
    /// <exception cref="InvalidOperationException">Se o título não estiver registrado.</exception>
    public void Suspender(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoTituloSim.Registrado)
        {
            throw new InvalidOperationException($"Só é possível suspender um título registrado. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoTituloSim.Suspenso;
        RaiseDomainEvent(new TituloRegistroSimSuspenso(Id, TenantId, motivo.Trim()));
    }

    /// <summary>Reativa um título suspenso (irregularidade sanada).</summary>
    /// <exception cref="InvalidOperationException">Se o título não estiver suspenso.</exception>
    public void Reativar()
    {
        if (Situacao != SituacaoTituloSim.Suspenso)
        {
            throw new InvalidOperationException($"Só é possível reativar um título suspenso. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoTituloSim.Registrado;
    }

    /// <summary>Cassa/cancela o título (registro extinto — irreversível).</summary>
    /// <param name="motivo">Motivo da cassação (auditoria).</param>
    /// <exception cref="InvalidOperationException">Se o título já estiver cassado.</exception>
    public void Cassar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoTituloSim.Cassado)
        {
            throw new InvalidOperationException("O título já está cassado.");
        }

        Situacao = SituacaoTituloSim.Cassado;
        RaiseDomainEvent(new TituloRegistroSimCassado(Id, TenantId, motivo.Trim()));
    }

    /// <summary>Renova a vigência do título (período subsequente).</summary>
    /// <param name="novoFimVigencia">Novo fim de vigência.</param>
    /// <exception cref="InvalidOperationException">Se o título não estiver registrado.</exception>
    public void Renovar(DateOnly novoFimVigencia)
    {
        if (Situacao != SituacaoTituloSim.Registrado)
        {
            throw new InvalidOperationException($"Só é possível renovar um título registrado. Situação atual: {Situacao}.");
        }

        if (FimVigencia is not null && novoFimVigencia <= FimVigencia)
        {
            throw new ArgumentOutOfRangeException(nameof(novoFimVigencia), "O novo fim de vigência deve ser posterior ao atual.");
        }

        FimVigencia = novoFimVigencia;
        RaiseDomainEvent(new TituloRegistroSimRenovado(Id, TenantId, novoFimVigencia));
    }
}
