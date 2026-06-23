using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Licenca/Alvara Sanitario de um <see cref="EstabelecimentoFiscalizavel"/>: ato administrativo que
/// autoriza o funcionamento por um periodo (em regra anual). Raiz de agregado que protege a maquina de
/// estado de validade (Vigente → Vencida por decurso; Vigente → Cassada por ato da VISA) e o ciclo de
/// RENOVACAO (emite nova vigencia a partir da anterior). A emissao costuma exigir inspecao aprovada
/// (verificado na orquestracao); estabelecimentos de baixo risco operam por mero registro (dispensa).
/// </summary>
public sealed class LicencaSanitaria : AggregateRoot<LicencaSanitariaId>, IMustHaveTenant
{
    private LicencaSanitaria()
    {
    }

    private LicencaSanitaria(
        LicencaSanitariaId id,
        Guid tenantId,
        EstabelecimentoFiscalizavelId estabelecimentoId,
        string numero,
        DateOnly emitidaEm,
        DateOnly validadeAte,
        InspecaoId? inspecaoId)
        : base(id)
    {
        TenantId = tenantId;
        EstabelecimentoFiscalizavelId = estabelecimentoId;
        Numero = numero;
        EmitidaEm = emitidaEm;
        ValidadeAte = validadeAte;
        InspecaoId = inspecaoId;
        Situacao = SituacaoLicenca.Vigente;
        RaiseDomainEvent(new LicencaSanitariaEmitida(id, estabelecimentoId, numero, validadeAte));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Estabelecimento licenciado (referencia por Id).</summary>
    public EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId { get; private set; }

    /// <summary>Numero do alvara (controle sequencial da VISA).</summary>
    public string Numero { get; private set; } = string.Empty;

    /// <summary>Data de emissao (inicio da vigencia).</summary>
    public DateOnly EmitidaEm { get; private set; }

    /// <summary>Data de validade (fim da vigencia).</summary>
    public DateOnly ValidadeAte { get; private set; }

    /// <summary>Inspecao que embasou a emissao (opcional — dispensa de licenciamento previo em baixo risco).</summary>
    public InspecaoId? InspecaoId { get; private set; }

    /// <summary>Situacao da licenca (Vigente/Vencida/Cassada).</summary>
    public SituacaoLicenca Situacao { get; private set; }

    /// <summary>
    /// Emite uma licenca/alvara para um estabelecimento. A validade deve ser posterior a emissao. As
    /// pre-condicoes (estabelecimento fiscalizavel; inspecao aprovada quando exigida) sao verificadas na
    /// orquestracao.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="estabelecimentoId">Estabelecimento licenciado.</param>
    /// <param name="numero">Numero do alvara.</param>
    /// <param name="emitidaEm">Data de emissao.</param>
    /// <param name="validadeAte">Data de validade.</param>
    /// <param name="inspecaoId">Inspecao fundante (opcional).</param>
    /// <returns>Nova <see cref="LicencaSanitaria"/> vigente.</returns>
    /// <exception cref="ArgumentException">Se o numero/estabelecimento forem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se a validade nao for posterior a emissao.</exception>
    public static LicencaSanitaria Emitir(
        Guid tenantId,
        EstabelecimentoFiscalizavelId estabelecimentoId,
        string numero,
        DateOnly emitidaEm,
        DateOnly validadeAte,
        InspecaoId? inspecaoId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento e obrigatorio.", nameof(estabelecimentoId));
        }

        if (validadeAte <= emitidaEm)
        {
            throw new InvalidOperationException("A validade deve ser posterior a data de emissao.");
        }

        return new LicencaSanitaria(
            LicencaSanitariaId.New(), tenantId, estabelecimentoId, numero.Trim(), emitidaEm, validadeAte, inspecaoId);
    }

    /// <summary>
    /// Renova a licenca a partir da vigencia atual, gerando uma NOVA licenca vigente (a anterior segue
    /// historica). Renovacao nao se aplica a licenca cassada. A nova validade deve ser posterior a emissao.
    /// </summary>
    /// <param name="numero">Numero da nova licenca.</param>
    /// <param name="emitidaEm">Emissao da renovacao.</param>
    /// <param name="validadeAte">Nova validade.</param>
    /// <param name="inspecaoId">Inspecao da renovacao (opcional).</param>
    /// <returns>A nova <see cref="LicencaSanitaria"/> renovada.</returns>
    /// <exception cref="InvalidOperationException">Se a licenca atual estiver cassada.</exception>
    public LicencaSanitaria Renovar(string numero, DateOnly emitidaEm, DateOnly validadeAte, InspecaoId? inspecaoId = null)
    {
        if (Situacao == SituacaoLicenca.Cassada)
        {
            throw new InvalidOperationException("Licenca cassada nao pode ser renovada — exige novo licenciamento.");
        }

        return Emitir(TenantId, EstabelecimentoFiscalizavelId, numero, emitidaEm, validadeAte, inspecaoId);
    }

    /// <summary>
    /// Reavalia a validade na data de referencia: licenca vigente cujo prazo expirou passa a Vencida.
    /// Idempotente; nao afeta licenca cassada. Usado em busca ativa / leitura.
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    public void AvaliarVigencia(DateOnly hoje)
    {
        if (Situacao == SituacaoLicenca.Vigente && hoje > ValidadeAte)
        {
            Situacao = SituacaoLicenca.Vencida;
        }
    }

    /// <summary>
    /// Cassa/revoga a licenca (ato da VISA, em regra apos auto de imposicao de penalidade). Idempotente.
    /// Emite <see cref="LicencaSanitariaCassada"/> na transicao.
    /// </summary>
    /// <param name="motivo">Motivo da cassacao (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    public void Cassar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoLicenca.Cassada)
        {
            return;
        }

        Situacao = SituacaoLicenca.Cassada;
        RaiseDomainEvent(new LicencaSanitariaCassada(Id, motivo.Trim()));
    }

    /// <summary>Indica se a licenca esta valida (vigente) na data de referencia.</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se vigente e dentro do prazo.</returns>
    public bool EstaValida(DateOnly hoje) => Situacao == SituacaoLicenca.Vigente && hoje <= ValidadeAte;
}
