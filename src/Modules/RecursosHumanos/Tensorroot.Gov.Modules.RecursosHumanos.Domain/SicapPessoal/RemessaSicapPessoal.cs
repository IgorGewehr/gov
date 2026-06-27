using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

/// <summary>Identificador forte do agregado <see cref="RemessaSicapPessoal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RemessaSicapPessoalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RemessaSicapPessoalId"/>.</returns>
    public static RemessaSicapPessoalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Remessa de auditoria de pessoal ao TCE-RS (SICAP-AP / SIAPESweb): o LOTE de atos de admissao a ser
/// transmitido para apreciacao da legalidade. Modela o cabecalho do leiaute estadual (codigo do orgao
/// CD_ORGAO, sequencial do lote NRO_MOV, versao destinatario VERSAO) e o "corpo" — a colecao de
/// <see cref="AtoAdmissaoSicap"/>. A entidade auditada envia a cada dois meses (periodicidade do
/// SIAPES); o sequencial do lote e' apurado por orgao/tenant na borda. A GERACAO do arquivo de
/// importacao e a transmissao real ao SIAPESweb ficam para o M10 (atras de ACL, com Polly + A1).
/// Raiz de agregado, nasce valida via <see cref="Abrir"/>.
/// </summary>
public sealed class RemessaSicapPessoal : AggregateRoot<RemessaSicapPessoalId>, IMustHaveTenant
{
    /// <summary>Versao do leiaute estadual operada por padrao (57 posicoes — TCE-RS).</summary>
    public const int VersaoLeiautePadrao = 57;

    private readonly List<AtoAdmissaoSicap> _atos = [];

    private RemessaSicapPessoal()
    {
    }

    private RemessaSicapPessoal(
        RemessaSicapPessoalId id,
        Guid tenantId,
        int codigoOrgao,
        int sequencialLote,
        DateOnly dataGeracaoLote,
        int versaoLeiaute)
        : base(id)
    {
        TenantId = tenantId;
        CodigoOrgao = codigoOrgao;
        SequencialLote = sequencialLote;
        DataGeracaoLote = dataGeracaoLote;
        VersaoLeiaute = versaoLeiaute;
        Situacao = SituacaoRemessaSicap.Aberta;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Codigo do orgao remetente (CD_ORGAO do cabecalho — codigo proprio/TCE).</summary>
    public int CodigoOrgao { get; private set; }

    /// <summary>Numero sequencial do lote (NRO_MOV) por orgao/tenant.</summary>
    public int SequencialLote { get; private set; }

    /// <summary>Data de geracao do lote (DT_INICIAL_MOV).</summary>
    public DateOnly DataGeracaoLote { get; private set; }

    /// <summary>Versao do leiaute SIAPES destinatario (VERSAO).</summary>
    public int VersaoLeiaute { get; private set; }

    /// <summary>Situacao (estado) da remessa.</summary>
    public SituacaoRemessaSicap Situacao { get; private set; }

    /// <summary>Data/hora de geracao do arquivo de importacao; nula enquanto aberta.</summary>
    public DateTimeOffset? GeradaEm { get; private set; }

    /// <summary>Protocolo de transmissao ao TCE-RS (// TODO(M10)); nulo ate transmitir.</summary>
    public string? ProtocoloTransmissao { get; private set; }

    /// <summary>Atos de admissao da remessa (corpo do arquivo), expostos somente pela raiz.</summary>
    public IReadOnlyCollection<AtoAdmissaoSicap> Atos => _atos;

    /// <summary>Quantidade de atos de INSERCAO ("I") — base do somatorio do cabecalho (QTD_INCLUSAO).</summary>
    public int QuantidadeAtos => _atos.Count;

    /// <summary>
    /// Abre uma remessa de pessoal SICAP-AP/SIAPES para o orgao/tenant. Nasce
    /// <see cref="SituacaoRemessaSicap.Aberta"/> e emite <see cref="RemessaSicapAberta"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigoOrgao">Codigo do orgao remetente (maior que zero).</param>
    /// <param name="sequencialLote">Sequencial do lote no orgao/tenant (maior que zero).</param>
    /// <param name="dataGeracaoLote">Data de geracao do lote.</param>
    /// <param name="versaoLeiaute">Versao do leiaute (padrao <see cref="VersaoLeiautePadrao"/>).</param>
    /// <returns>Nova <see cref="RemessaSicapPessoal"/> em situacao <see cref="SituacaoRemessaSicap.Aberta"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o codigo do orgao/sequencial/versao forem invalidos.</exception>
    public static RemessaSicapPessoal Abrir(
        Guid tenantId,
        int codigoOrgao,
        int sequencialLote,
        DateOnly dataGeracaoLote,
        int versaoLeiaute = VersaoLeiautePadrao)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(codigoOrgao);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequencialLote);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versaoLeiaute);

        var remessa = new RemessaSicapPessoal(
            RemessaSicapPessoalId.New(),
            tenantId,
            codigoOrgao,
            sequencialLote,
            dataGeracaoLote,
            versaoLeiaute);
        remessa.RaiseDomainEvent(new RemessaSicapAberta(remessa.Id, codigoOrgao, sequencialLote));
        return remessa;
    }

    /// <summary>
    /// Acrescenta um ato de admissao (movimento "I") ao corpo da remessa, com as regras minimas do
    /// leiaute: carga horaria valida, classificacao obrigatoria no concurso publico, e — havendo data
    /// de termino — motivo de extincao obrigatorio (CD_EXTINCAO). So e' possivel enquanto Aberta.
    /// </summary>
    /// <param name="servidorId">Servidor de origem (opcional, rastreio interno).</param>
    /// <param name="identificadorAto">Identificador unico do ato no sistema (nao vazio, ate 50).</param>
    /// <param name="tipoAto">Tipo/titulo da admissao (Tabela 4).</param>
    /// <param name="regime">Regime juridico (Tabela 5).</param>
    /// <param name="cpf">CPF do servidor.</param>
    /// <param name="nome">Nome do servidor.</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <param name="descricaoCargo">Descricao do cargo atual (nao vazia, ate 70).</param>
    /// <param name="cargaHorariaSemanal">Carga horaria semanal (1..44 no geral; ate 100 no temporario).</param>
    /// <param name="classificacaoConcurso">Classificacao no concurso (obrigatoria no titulo 01).</param>
    /// <param name="dataAto">Data do ato.</param>
    /// <param name="dataHistorica">Data historica (opcional/variavel por titulo).</param>
    /// <param name="dataTermino">Data de termino (extincao); nula se vigente.</param>
    /// <param name="motivoExtincao">Motivo da extincao; obrigatorio quando ha data de termino.</param>
    /// <returns>Identificador do ato adicionado.</returns>
    /// <exception cref="ArgumentException">Se identificador/cargo/CPF/nome forem vazios ou excederem o limite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a carga horaria estiver fora da faixa.</exception>
    /// <exception cref="InvalidOperationException">Se a remessa nao estiver aberta, ou faltarem campos obrigatorios por titulo.</exception>
    public AtoAdmissaoSicapId AdicionarAto(
        ServidorId? servidorId,
        string identificadorAto,
        TipoAtoAdmissao tipoAto,
        RegimeJuridicoSiapes regime,
        string cpf,
        string nome,
        DateOnly dataNascimento,
        string descricaoCargo,
        int cargaHorariaSemanal,
        int? classificacaoConcurso,
        DateOnly dataAto,
        DateOnly? dataHistorica,
        DateOnly? dataTermino,
        MotivoExtincaoVinculo? motivoExtincao)
    {
        GarantirAberta();
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorAto);
        ArgumentException.ThrowIfNullOrWhiteSpace(cpf);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricaoCargo);

        var identificador = identificadorAto.Trim();
        if (identificador.Length > AtoAdmissaoSicap.ComprimentoMaximoIdentificador)
        {
            throw new ArgumentException($"Identificador do ato excede {AtoAdmissaoSicap.ComprimentoMaximoIdentificador} caracteres.", nameof(identificadorAto));
        }

        var cargo = descricaoCargo.Trim();
        if (cargo.Length > AtoAdmissaoSicap.ComprimentoMaximoCargo)
        {
            throw new ArgumentException($"Descricao do cargo excede {AtoAdmissaoSicap.ComprimentoMaximoCargo} caracteres.", nameof(descricaoCargo));
        }

        // Carga horaria: contratacao por prazo determinado (titulo 02) admite ate 100h (jornada
        // mensal convertida); concursados e demais titulos limitam a 44h (leiaute, item 13).
        var limiteCarga = tipoAto == TipoAtoAdmissao.PrazoDeterminado ? 100 : 44;
        if (cargaHorariaSemanal is < 1 || cargaHorariaSemanal > limiteCarga)
        {
            throw new ArgumentOutOfRangeException(nameof(cargaHorariaSemanal), cargaHorariaSemanal, $"Carga horaria semanal deve estar em [1, {limiteCarga}] para o titulo informado.");
        }

        // CLASSIFICACAO obrigatoria no concurso publico (titulo 01 — leiaute, item 14).
        if (tipoAto == TipoAtoAdmissao.ConcursoPublico && classificacaoConcurso is null)
        {
            throw new InvalidOperationException("Admissao por concurso publico exige a classificacao no concurso.");
        }

        // Havendo extincao (DATA_TERMINO), o motivo (CD_EXTINCAO) e' obrigatorio (leiaute, item 7).
        if (dataTermino is not null && motivoExtincao is null)
        {
            throw new InvalidOperationException("Ato com data de termino exige o motivo de extincao do vinculo (CD_EXTINCAO).");
        }

        if (dataTermino is { } termino && termino < dataAto)
        {
            throw new InvalidOperationException("Data de termino nao pode ser anterior a data do ato.");
        }

        var ato = new AtoAdmissaoSicap(
            AtoAdmissaoSicapId.New(),
            servidorId,
            identificador,
            tipoAto,
            regime,
            cpf.Trim(),
            nome.Trim().ToUpperInvariant(),
            dataNascimento,
            cargo,
            cargaHorariaSemanal,
            classificacaoConcurso,
            dataAto,
            dataHistorica,
            dataTermino,
            motivoExtincao);

        _atos.Add(ato);
        return ato.Id;
    }

    /// <summary>
    /// Fecha a remessa gerando o arquivo de importacao: exige ao menos um ato e transita para
    /// <see cref="SituacaoRemessaSicap.Gerada"/>, emitindo <see cref="RemessaSicapGerada"/>.
    /// </summary>
    /// <param name="geradaEm">Instante da geracao.</param>
    /// <exception cref="InvalidOperationException">Se a remessa nao estiver aberta ou estiver vazia.</exception>
    public void Gerar(DateTimeOffset geradaEm)
    {
        GarantirAberta();
        if (_atos.Count == 0)
        {
            throw new InvalidOperationException("Remessa sem atos nao pode ser gerada.");
        }

        GeradaEm = geradaEm;
        Situacao = SituacaoRemessaSicap.Gerada;
        RaiseDomainEvent(new RemessaSicapGerada(Id, CodigoOrgao, SequencialLote, _atos.Count));
    }

    /// <summary>
    /// Marca a remessa como transmitida ao TCE-RS, guardando o protocolo. // TODO(M10): integrar a
    /// transmissao real ao SIAPESweb atras de ACL com Polly + certificado A1 do Key Vault.
    /// </summary>
    /// <param name="protocolo">Protocolo de transmissao retornado pelo TCE-RS.</param>
    /// <exception cref="ArgumentException">Se o protocolo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a remessa nao estiver gerada.</exception>
    public void MarcarTransmitida(string protocolo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocolo);
        if (Situacao != SituacaoRemessaSicap.Gerada)
        {
            throw new InvalidOperationException($"Transmissao exige remessa Gerada. Situacao atual: {Situacao}.");
        }

        ProtocoloTransmissao = protocolo.Trim();
        Situacao = SituacaoRemessaSicap.Transmitida;
    }

    private void GarantirAberta()
    {
        if (Situacao != SituacaoRemessaSicap.Aberta)
        {
            throw new InvalidOperationException($"Operacao exige remessa Aberta. Situacao atual: {Situacao}.");
        }
    }
}
