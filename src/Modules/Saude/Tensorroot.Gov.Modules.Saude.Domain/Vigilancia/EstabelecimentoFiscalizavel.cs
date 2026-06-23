using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Estabelecimento sujeito a Vigilancia Sanitaria (VISA): restaurante, farmacia, clinica, salao, etc.
/// Raiz de agregado e cadastro-mestre LOCAL do ciclo de fiscalizacao — INDEPENDENTE do PEP/CNES (um bar
/// nao e estabelecimento de saude, mas e fiscalizavel). Identificado pelo documento do responsavel
/// (CNPJ ou CPF), classificado por ramo e grau de risco sanitario (RDC Anvisa 153/2017; Lei 13.874/2019).
/// O grau de risco determina a periodicidade de inspecao e se ha dispensa de licenciamento previo
/// (Risco I — Lei da Liberdade Economica). Inspecoes, autos e licencas referenciam este agregado por Id.
/// </summary>
public sealed class EstabelecimentoFiscalizavel : AggregateRoot<EstabelecimentoFiscalizavelId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo da razao social/nome fantasia.</summary>
    public const int ComprimentoNome = 200;

    private EstabelecimentoFiscalizavel()
    {
    }

    private EstabelecimentoFiscalizavel(
        EstabelecimentoFiscalizavelId id,
        Guid tenantId,
        DocumentoResponsavel documento,
        string razaoSocial,
        RamoVisa ramo,
        GrauRiscoSanitario risco,
        EnderecoVisa endereco)
        : base(id)
    {
        TenantId = tenantId;
        Documento = documento;
        RazaoSocial = razaoSocial;
        Ramo = ramo;
        Risco = risco;
        Endereco = endereco;
        Situacao = SituacaoEstabelecimentoVisa.Ativo;
        RaiseDomainEvent(new EstabelecimentoFiscalizavelCadastrado(id, documento.Digitos, ramo));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Documento do responsavel (CNPJ/CPF) — chave de negocio do cadastro.</summary>
    public DocumentoResponsavel Documento { get; private set; }

    /// <summary>Razao social/nome fantasia do estabelecimento.</summary>
    public string RazaoSocial { get; private set; } = string.Empty;

    /// <summary>Ramo de atividade sujeito a VISA (define o roteiro de inspecao aplicavel).</summary>
    public RamoVisa Ramo { get; private set; }

    /// <summary>Grau de risco sanitario (define periodicidade e dispensa de licenciamento previo).</summary>
    public GrauRiscoSanitario Risco { get; private set; }

    /// <summary>Endereco do estabelecimento.</summary>
    public EnderecoVisa Endereco { get; private set; } = EnderecoVisa.Vazio;

    /// <summary>Situacao cadastral (ativo/inativo/interditado).</summary>
    public SituacaoEstabelecimentoVisa Situacao { get; private set; }

    /// <summary>
    /// Cadastra um estabelecimento fiscalizavel. Nasce Ativo. A unicidade por
    /// <c>(TenantId, Documento, RazaoSocial)</c> e garantida na orquestracao (handler/indice).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="documento">Documento do responsavel (CNPJ/CPF valido).</param>
    /// <param name="razaoSocial">Razao social/nome do estabelecimento.</param>
    /// <param name="ramo">Ramo de atividade.</param>
    /// <param name="risco">Grau de risco sanitario.</param>
    /// <param name="endereco">Endereco do estabelecimento.</param>
    /// <returns>Novo <see cref="EstabelecimentoFiscalizavel"/> ativo.</returns>
    /// <exception cref="ArgumentException">Se a razao social for vazia ou exceder o limite.</exception>
    public static EstabelecimentoFiscalizavel Cadastrar(
        Guid tenantId,
        DocumentoResponsavel documento,
        string razaoSocial,
        RamoVisa ramo,
        GrauRiscoSanitario risco,
        EnderecoVisa endereco)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(razaoSocial);
        if (razaoSocial.Length > ComprimentoNome)
        {
            throw new ArgumentException($"Razao social excede {ComprimentoNome} caracteres.", nameof(razaoSocial));
        }

        return new EstabelecimentoFiscalizavel(
            EstabelecimentoFiscalizavelId.New(), tenantId, documento, razaoSocial.Trim(), ramo, risco, endereco);
    }

    /// <summary>Reclassifica o ramo/risco (ex.: mudanca de atividade) — apenas quando nao interditado.</summary>
    /// <param name="ramo">Novo ramo.</param>
    /// <param name="risco">Novo grau de risco.</param>
    /// <exception cref="InvalidOperationException">Se o estabelecimento estiver interditado.</exception>
    public void Reclassificar(RamoVisa ramo, GrauRiscoSanitario risco)
    {
        if (Situacao == SituacaoEstabelecimentoVisa.Interditado)
        {
            throw new InvalidOperationException("Estabelecimento interditado nao pode ser reclassificado.");
        }

        Ramo = ramo;
        Risco = risco;
    }

    /// <summary>
    /// Interdita o estabelecimento (medida cautelar/definitiva da VISA — Lei 6.437/1977, art. 23). Operacao
    /// fica vedada; o desfecho costuma decorrer de inspecao reprovada ou auto de penalidade. Idempotente.
    /// </summary>
    /// <param name="motivo">Motivo da interdicao (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    public void Interditar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoEstabelecimentoVisa.Interditado)
        {
            return;
        }

        Situacao = SituacaoEstabelecimentoVisa.Interditado;
        RaiseDomainEvent(new EstabelecimentoVisaInterditado(Id, motivo.Trim()));
    }

    /// <summary>Levanta a interdicao (regularizacao), retornando o estabelecimento a Ativo.</summary>
    /// <exception cref="InvalidOperationException">Se o estabelecimento nao estiver interditado.</exception>
    public void LevantarInterdicao()
    {
        if (Situacao != SituacaoEstabelecimentoVisa.Interditado)
        {
            throw new InvalidOperationException("So e possivel levantar interdicao de estabelecimento interditado.");
        }

        Situacao = SituacaoEstabelecimentoVisa.Ativo;
    }

    /// <summary>Inativa o estabelecimento (baixa/encerramento) — sai do ciclo de fiscalizacao.</summary>
    /// <exception cref="InvalidOperationException">Se o estabelecimento estiver interditado.</exception>
    public void Inativar()
    {
        if (Situacao == SituacaoEstabelecimentoVisa.Interditado)
        {
            throw new InvalidOperationException("Levante a interdicao antes de inativar.");
        }

        Situacao = SituacaoEstabelecimentoVisa.Inativo;
    }

    /// <summary>Indica se o estabelecimento esta apto a receber inspecao/licenca (ativo, nao interditado).</summary>
    /// <returns><c>true</c> se fiscalizavel.</returns>
    public bool EstaFiscalizavel() => Situacao == SituacaoEstabelecimentoVisa.Ativo;

    /// <summary>
    /// Indica se o ramo/risco dispensa licenciamento previo (Risco I — Lei 13.874/2019, art. 3º, I).
    /// Estabelecimentos de baixo risco operam mediante mero registro, sem alvara previo.
    /// </summary>
    /// <returns><c>true</c> se dispensado de licenciamento previo.</returns>
    public bool DispensaLicenciamentoPrevio() => Risco == GrauRiscoSanitario.Baixo;
}
