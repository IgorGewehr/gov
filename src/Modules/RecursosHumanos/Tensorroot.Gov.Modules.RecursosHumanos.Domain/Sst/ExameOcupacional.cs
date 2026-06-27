using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

/// <summary>Identificador forte do agregado <see cref="ExameOcupacional"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ExameOcupacionalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ExameOcupacionalId"/>.</returns>
    public static ExameOcupacionalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Atestado de Saude Ocupacional (ASO) de um servidor — base do evento eSocial <b>S-2220</b> (Monitoramento
/// da Saude do Trabalhador) e insumo do <b>PCMSO</b> (NR-07) e do <b>PPP</b> (campo de monitoracao biologica).
/// Modela o exame medico ocupacional (admissional/periodico/retorno/mudanca de risco/demissional), o medico
/// responsavel, o resultado (apto/inapto), os exames complementares (Tabela 27) e a data do proximo exame —
/// dado nuclear do programa de controle medico. Raiz de agregado <see cref="IMustHaveTenant"/>; nasce valida
/// via <see cref="Registrar"/>. // TODO(validar-oficial): campos exatos do grupo <c>exMedOcup</c>/<c>aso</c>
/// e codProcRealizado (Tabela 27) no XSD travado do S-1.3.
/// </summary>
public sealed class ExameOcupacional : AggregateRoot<ExameOcupacionalId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do codigo de exame complementar (Tabela 27).</summary>
    public const int ComprimentoMaximoCodigoExame = 4;

    private readonly List<string> _examesComplementares = [];

    private ExameOcupacional()
    {
    }

    private ExameOcupacional(
        ExameOcupacionalId id,
        Guid tenantId,
        ServidorId servidorId,
        TipoExameOcupacional tipo,
        DateOnly dataExame,
        ResultadoAso resultado,
        MedicoResponsavel medico,
        DateOnly? dataProximoExame,
        string? observacao)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        Tipo = tipo;
        DataExame = dataExame;
        Resultado = resultado;
        Medico = medico;
        DataProximoExame = dataProximoExame;
        Observacao = observacao;
        Situacao = SituacaoRegistroSst.Registrado;
        RaiseDomainEvent(new ExameOcupacionalRegistrado(id, servidorId, tipo, dataExame));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor examinado.</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Tipo do exame ocupacional (admissional/periodico/...).</summary>
    public TipoExameOcupacional Tipo { get; private set; }

    /// <summary>Data de realizacao do exame (<c>dtExm</c>).</summary>
    public DateOnly DataExame { get; private set; }

    /// <summary>Resultado/aptidao do ASO (apto/inapto).</summary>
    public ResultadoAso Resultado { get; private set; }

    /// <summary>Medico responsavel pelo ASO (<c>medico</c>).</summary>
    public MedicoResponsavel Medico { get; private set; } = default!;

    /// <summary>Data prevista do proximo exame (insumo do PCMSO/agendamento); nula quando n/a (ex.: demissional).</summary>
    public DateOnly? DataProximoExame { get; private set; }

    /// <summary>Observacao/restricoes do ASO (texto livre); nula quando ausente.</summary>
    public string? Observacao { get; private set; }

    /// <summary>Situacao do registro (vigente/cancelado).</summary>
    public SituacaoRegistroSst Situacao { get; private set; }

    /// <summary>Motivo do cancelamento, quando cancelado; nulo enquanto vigente.</summary>
    public string? MotivoCancelamento { get; private set; }

    /// <summary>Codigos dos exames complementares realizados (Tabela 27 do eSocial), expostos somente pela raiz.</summary>
    public IReadOnlyCollection<string> ExamesComplementares => _examesComplementares;

    /// <summary>
    /// Registra um ASO de um servidor (estado <see cref="SituacaoRegistroSst.Registrado"/>). Emite
    /// <see cref="ExameOcupacionalRegistrado"/> (gancho p/ geracao do S-2220 e atualizacao do PCMSO).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor examinado.</param>
    /// <param name="tipo">Tipo do exame.</param>
    /// <param name="dataExame">Data de realizacao.</param>
    /// <param name="resultado">Resultado/aptidao.</param>
    /// <param name="medico">Medico responsavel.</param>
    /// <param name="dataProximoExame">Data do proximo exame (opcional).</param>
    /// <param name="observacao">Observacao/restricoes (opcional).</param>
    /// <returns>Novo <see cref="ExameOcupacional"/> em situacao <see cref="SituacaoRegistroSst.Registrado"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o medico for nulo.</exception>
    /// <exception cref="ArgumentException">Se o proximo exame nao for posterior a data do exame.</exception>
    public static ExameOcupacional Registrar(
        Guid tenantId,
        ServidorId servidorId,
        TipoExameOcupacional tipo,
        DateOnly dataExame,
        ResultadoAso resultado,
        MedicoResponsavel medico,
        DateOnly? dataProximoExame = null,
        string? observacao = null)
    {
        ArgumentNullException.ThrowIfNull(medico);

        if (dataProximoExame is { } proximo && proximo <= dataExame)
        {
            throw new ArgumentException("Data do proximo exame deve ser posterior a data do exame.", nameof(dataProximoExame));
        }

        var observacaoNormalizada = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        return new ExameOcupacional(
            ExameOcupacionalId.New(),
            tenantId,
            servidorId,
            tipo,
            dataExame,
            resultado,
            medico,
            dataProximoExame,
            observacaoNormalizada);
    }

    /// <summary>Acrescenta um exame complementar realizado (codigo da Tabela 27 do eSocial), deduplicando.</summary>
    /// <param name="codigoProcedimento">Codigo do procedimento/exame complementar (nao vazio, ate 4 caracteres).</param>
    /// <exception cref="ArgumentException">Se o codigo for vazio ou exceder o limite.</exception>
    /// <exception cref="InvalidOperationException">Se o registro estiver cancelado.</exception>
    public void AdicionarExameComplementar(string codigoProcedimento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoProcedimento);
        GarantirRegistrado();

        var codigo = codigoProcedimento.Trim();
        if (codigo.Length > ComprimentoMaximoCodigoExame)
        {
            throw new ArgumentException($"Codigo de exame complementar excede {ComprimentoMaximoCodigoExame} caracteres.", nameof(codigoProcedimento));
        }

        if (!_examesComplementares.Contains(codigo, StringComparer.OrdinalIgnoreCase))
        {
            _examesComplementares.Add(codigo);
        }
    }

    /// <summary>
    /// Cancela o ASO (tornado sem efeito; estado terminal). Espelha o gancho de exclusao do S-2220 quando
    /// ja transmitido. Emite <see cref="RegistroSstCancelado"/>.
    /// </summary>
    /// <param name="motivo">Motivo do cancelamento (nao vazio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se ja cancelado.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirRegistrado();

        Situacao = SituacaoRegistroSst.Cancelado;
        MotivoCancelamento = motivo.Trim();
        RaiseDomainEvent(new RegistroSstCancelado(Id.Value, nameof(ExameOcupacional), MotivoCancelamento));
    }

    private void GarantirRegistrado()
    {
        if (Situacao == SituacaoRegistroSst.Cancelado)
        {
            throw new InvalidOperationException("ASO cancelado e estado terminal; nao admite alteracao.");
        }
    }
}
