using Tensorroot.Gov.Modules.Protocolo.Domain.Events;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

/// <summary>Identificador forte do agregado <see cref="Processo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProcessoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProcessoId"/>.</returns>
    public static ProcessoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Processo Administrativo Eletronico (PAE): conjunto ordenado de documentos com
/// finalidade, regido pela Lei 9.784/1999. Cobre todo o ciclo de vida — autuacao
/// (geracao do NUP), tramitacao entre setores, despacho, sobrestamento/reativacao e
/// arquivamento conforme a Tabela de Temporalidade e Destinacao (CONARQ). Raiz de
/// agregado; nasce valida pela fabrica <see cref="Autuar"/>.
/// </summary>
public sealed class Processo : AggregateRoot<ProcessoId>, IMustHaveTenant
{
    /// <summary>Prazo padrao de decisao (Lei 9.784/1999 — em regra 30 dias, prorrogaveis).</summary>
    public const int PrazoDecisaoPadraoDias = 30;

    private readonly List<Despacho> _despachos = [];
    private readonly List<Movimentacao> _movimentacoes = [];

    private Processo()
    {
    }

    private Processo(
        ProcessoId id,
        Guid tenantId,
        Nup nup,
        Classificacao classificacao,
        NivelDeAcesso nivelAcesso,
        Prazo prazo,
        Guid? requerimentoId,
        string? origemModulo,
        Guid? origemId,
        DateOnly dataAutuacao)
        : base(id)
    {
        TenantId = tenantId;
        Nup = nup;
        Classificacao = classificacao;
        NivelAcesso = nivelAcesso;
        Prazo = prazo;
        RequerimentoId = requerimentoId;
        OrigemModulo = origemModulo;
        OrigemId = origemId;
        DataAutuacao = dataAutuacao;
        Situacao = SituacaoProcesso.Autuado;
        RaiseDomainEvent(new ProcessoAutuado(id, nup, origemModulo, origemId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Numero Unico de Protocolo — imutavel apos a autuacao (I-1).</summary>
    public Nup Nup { get; private set; }

    /// <summary>Classe documental (vincula a Tabela de Temporalidade).</summary>
    public Classificacao Classificacao { get; private set; }

    /// <summary>Visibilidade do processo.</summary>
    public NivelDeAcesso NivelAcesso { get; private set; }

    /// <summary>Prazo legal/administrativo (art. 66, Lei 9.784/1999).</summary>
    public Prazo Prazo { get; private set; }

    /// <summary>Setor/unidade atualmente responsavel pelo processo.</summary>
    public Guid? SetorAtualId { get; private set; }

    /// <summary>Modulo originador (ex.: "Licitacao", "RH"); nulo quando autuado pelo proprio Protocolo.</summary>
    public string? OrigemModulo { get; private set; }

    /// <summary>Identificador da origem no modulo originador.</summary>
    public Guid? OrigemId { get; private set; }

    /// <summary>Requerimento que originou o processo (quando aplicavel).</summary>
    public Guid? RequerimentoId { get; private set; }

    /// <summary>
    /// Documento (CPF/CNPJ, somente digitos) do INTERESSADO/parte do processo, quando ha um cidadao
    /// titular. E a ANCORA do "meus processos" do Portal do Cidadao: liga o processo a uma pessoa civil
    /// sem acoplar o Protocolo ao modulo Cidadao (o documento e dado civil, nao chave interna de outro
    /// modulo — CLAUDE.md §2). Nulo em processos internos sem interessado externo. O Portal NUNCA filtra
    /// por este campo a partir de um valor do cliente — o documento e SEMPRE resolvido server-side do
    /// principal autenticado (anti-IDOR).
    /// </summary>
    public string? InteressadoDocumento { get; private set; }

    /// <summary>Data da autuacao do processo.</summary>
    public DateOnly DataAutuacao { get; private set; }

    /// <summary>Situacao atual no ciclo de vida do PAE.</summary>
    public SituacaoProcesso Situacao { get; private set; }

    /// <summary>Despachos do processo (entidade interna, append-only — I-10).</summary>
    public IReadOnlyList<Despacho> Despachos => _despachos;

    /// <summary>Tramitacoes do processo (entidade interna, append-only — I-10).</summary>
    public IReadOnlyList<Movimentacao> Movimentacoes => _movimentacoes;

    /// <summary>
    /// Autua o processo: gera o NUP, define a situacao inicial <see cref="SituacaoProcesso.Autuado"/>,
    /// calcula o prazo padrao de decisao (art. 66) e emite <see cref="ProcessoAutuado"/> (I-2, I-3).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="nup">Numero Unico de Protocolo (nao vazio).</param>
    /// <param name="classificacao">Classe documental (nao vazia).</param>
    /// <param name="nivelAcesso">Nivel de acesso do processo.</param>
    /// <param name="requerimentoId">Requerimento de origem (opcional).</param>
    /// <param name="origemModulo">Modulo originador (quando autuado por outro modulo).</param>
    /// <param name="origemId">Identificador da origem no modulo originador (obrigatorio com <paramref name="origemModulo"/> — I-12).</param>
    /// <param name="dataAutuacao">Data da autuacao.</param>
    /// <returns>Novo <see cref="Processo"/> em situacao <see cref="SituacaoProcesso.Autuado"/>.</returns>
    /// <exception cref="ArgumentException">Se NUP/classificacao forem vazios (I-3).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se houver modulo originador sem identificador de origem (I-12).</exception>
    public static Processo Autuar(
        Guid tenantId,
        Nup nup,
        Classificacao classificacao,
        NivelDeAcesso nivelAcesso,
        Guid? requerimentoId,
        string? origemModulo,
        Guid? origemId,
        DateOnly dataAutuacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nup.Valor);
        ArgumentException.ThrowIfNullOrWhiteSpace(classificacao.Codigo);

        // I-12: quando autuado por outro modulo, origem e obrigatoria e preservada.
        if (!string.IsNullOrWhiteSpace(origemModulo) && (origemId is null || origemId == Guid.Empty))
        {
            throw new ArgumentOutOfRangeException(nameof(origemId), "Identificador de origem e obrigatorio quando ha modulo originador.");
        }

        var prazo = Prazo.APartirDe(dataAutuacao, PrazoDecisaoPadraoDias);
        return new Processo(
            ProcessoId.New(),
            tenantId,
            nup,
            classificacao,
            nivelAcesso,
            prazo,
            requerimentoId,
            string.IsNullOrWhiteSpace(origemModulo) ? null : origemModulo,
            origemId,
            dataAutuacao);
    }

    /// <summary>
    /// Vincula (ou atualiza) o INTERESSADO/parte do processo pelo seu documento civil (CPF/CNPJ, somente
    /// digitos). Usado pela autuacao de processos com titular cidadao (ex.: requerimento via portal) — a
    /// partir dai, o "meus processos" do Portal do Cidadao resolve o titular por este campo. O documento
    /// e validado/normalizado pelo caso de uso (VO Cpf/Cnpj do SharedKernel) antes de chegar aqui.
    /// </summary>
    /// <param name="documento">Documento do interessado (somente digitos, ja validado).</param>
    /// <exception cref="ArgumentException">Se o documento for vazio.</exception>
    public void RegistrarInteressado(string documento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);
        InteressadoDocumento = documento.Trim();
    }

    /// <summary>
    /// Tramita o processo para o setor de destino: registra <see cref="Movimentacao"/>, atualiza
    /// <see cref="SetorAtualId"/>, passa a <see cref="SituacaoProcesso.EmTramitacao"/> e emite
    /// <see cref="ProcessoTramitado"/> (I-4).
    /// </summary>
    /// <param name="setorDestinoId">Setor de destino (informado).</param>
    /// <param name="observacao">Observacao opcional da tramitacao.</param>
    /// <param name="data">Data da tramitacao.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o setor de destino nao for informado.</exception>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver em andamento (I-4, I-8).</exception>
    public void Tramitar(Guid setorDestinoId, string? observacao, DateOnly data)
    {
        GarantirEmAndamento();
        if (setorDestinoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(setorDestinoId), "Setor de destino e obrigatorio.");
        }

        _movimentacoes.Add(Movimentacao.Registrar(SetorAtualId, setorDestinoId, observacao, data));
        SetorAtualId = setorDestinoId;
        Situacao = SituacaoProcesso.EmTramitacao;
        RaiseDomainEvent(new ProcessoTramitado(Id, setorDestinoId));
    }

    /// <summary>
    /// Registra um despacho (entidade interna, append-only) sem alterar a situacao; preserva a
    /// trilha documental (I-10). So ocorre sobre processo em andamento.
    /// </summary>
    /// <param name="texto">Conteudo do despacho.</param>
    /// <param name="autoridadeId">Autoridade que profere o despacho.</param>
    /// <param name="data">Data do despacho.</param>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver em andamento (I-8).</exception>
    public void Despachar(string texto, Guid autoridadeId, DateOnly data)
    {
        GarantirEmAndamento();
        _despachos.Add(Despacho.Registrar(texto, autoridadeId, data));
    }

    /// <summary>
    /// Sobresta o processo (suspende temporariamente o andamento), passando a
    /// <see cref="SituacaoProcesso.Sobrestado"/> e emitindo <see cref="ProcessoSobrestado"/> (I-5).
    /// </summary>
    /// <param name="motivo">Motivo do sobrestamento (nao vazio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver em andamento (I-5).</exception>
    public void Sobrestar(string motivo)
    {
        GarantirEmAndamento();
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        Situacao = SituacaoProcesso.Sobrestado;
        RaiseDomainEvent(new ProcessoSobrestado(Id, motivo.Trim()));
    }

    /// <summary>
    /// Reativa o processo sobrestado, retornando-o a <see cref="SituacaoProcesso.EmTramitacao"/> e
    /// emitindo <see cref="ProcessoTramitado"/> (I-6).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver sobrestado (B-7).</exception>
    public void Reativar()
    {
        if (Situacao != SituacaoProcesso.Sobrestado)
        {
            throw new InvalidOperationException($"Reativacao exige processo Sobrestado. Situacao atual: {Situacao}.");
        }

        // Um processo EmTramitacao DEVE ter setor responsável (Lei 9.784/1999 — rastreabilidade). Um
        // processo apenas Autuado (nunca tramitado) sobrestado não pode "reativar" para tramitação sem
        // destino: emitir ProcessoTramitado com SetorDestino=Guid.Empty invalidaria a trilha.
        if (SetorAtualId is null)
        {
            throw new InvalidOperationException(
                "Nao e possivel reativar um processo que nunca foi tramitado a um setor.");
        }

        Situacao = SituacaoProcesso.EmTramitacao;
        RaiseDomainEvent(new ProcessoTramitado(Id, SetorAtualId.Value));
    }

    /// <summary>Motivo do arquivamento (registro do ato — Lei 9.784/1999). Nulo enquanto não arquivado.</summary>
    public string? MotivoArquivamento { get; private set; }

    /// <summary>Data do arquivamento. Nula enquanto não arquivado.</summary>
    public DateOnly? DataArquivamento { get; private set; }

    /// <summary>
    /// Arquiva o processo (estado terminal), passando a <see cref="SituacaoProcesso.Arquivado"/> e
    /// emitindo <see cref="ProcessoArquivado"/>. A guarda/eliminacao posterior segue a TTD/CONARQ (I-7).
    /// </summary>
    /// <param name="motivo">Motivo do arquivamento (opcional).</param>
    /// <param name="data">Data do arquivamento.</param>
    /// <exception cref="InvalidOperationException">Se o processo ja estiver arquivado (I-7, B-5).</exception>
    public void Arquivar(string? motivo, DateOnly data)
    {
        if (Situacao == SituacaoProcesso.Arquivado)
        {
            throw new InvalidOperationException("Processo ja esta arquivado.");
        }

        // Persiste motivo e data (dados legalmente obrigatorios do ato — Lei 9.784/1999) em vez de descarta-los.
        MotivoArquivamento = motivo;
        DataArquivamento = data;
        Situacao = SituacaoProcesso.Arquivado;
        RaiseDomainEvent(new ProcessoArquivado(Id, motivo, data));
    }

    private void GarantirEmAndamento()
    {
        if (Situacao is not (SituacaoProcesso.Autuado or SituacaoProcesso.EmTramitacao))
        {
            throw new InvalidOperationException($"Operacao exige processo em andamento (Autuado/EmTramitacao). Situacao atual: {Situacao}.");
        }
    }
}
