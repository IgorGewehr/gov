using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

/// <summary>Identificador forte do agregado <see cref="ExposicaoAgenteNocivo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ExposicaoAgenteNocivoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ExposicaoAgenteNocivoId"/>.</returns>
    public static ExposicaoAgenteNocivoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Condicoes ambientais do trabalho de um servidor — base do evento eSocial <b>S-2240</b> (Condicoes
/// Ambientais do Trabalho — Agentes Nocivos) e fonte primaria do <b>PPP</b> (Perfil Profissiografico
/// Previdenciario, IN INSS 128/2022): a exposicao a agentes nocivos por periodo determina o direito a
/// aposentadoria especial. Modela o periodo de exposicao (inicio/fim), o setor/atividade e a lista de
/// <see cref="AgenteNocivo"/> (Tabela 23), com EPC/EPI. Raiz de agregado <see cref="IMustHaveTenant"/>;
/// nasce valida via <see cref="Iniciar"/>. // TODO(validar-oficial): grupos <c>infoExpRisco</c>/<c>agNoc</c>
/// e responsavel pelos registros ambientais no XSD travado do S-1.3.
/// </summary>
public sealed class ExposicaoAgenteNocivo : AggregateRoot<ExposicaoAgenteNocivoId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo da descricao do setor/atividade.</summary>
    public const int ComprimentoMaximoSetor = 255;

    private readonly List<AgenteNocivo> _agentes = [];

    private ExposicaoAgenteNocivo()
    {
    }

    private ExposicaoAgenteNocivo(
        ExposicaoAgenteNocivoId id,
        Guid tenantId,
        ServidorId servidorId,
        DateOnly inicioExposicao,
        string setorAtividade)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        InicioExposicao = inicioExposicao;
        SetorAtividade = setorAtividade;
        Situacao = SituacaoRegistroSst.Registrado;
        RaiseDomainEvent(new ExposicaoAgenteNocivoIniciada(id, servidorId, inicioExposicao));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor exposto.</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Inicio do periodo de exposicao (<c>dtIniCondicao</c>).</summary>
    public DateOnly InicioExposicao { get; private set; }

    /// <summary>Fim do periodo de exposicao (<c>dtFimCondicao</c>); nulo enquanto vigente.</summary>
    public DateOnly? FimExposicao { get; private set; }

    /// <summary>Setor/atividade onde ocorre a exposicao (descricao das atribuicoes — PPP).</summary>
    public string SetorAtividade { get; private set; } = default!;

    /// <summary>Situacao do registro (vigente/cancelado).</summary>
    public SituacaoRegistroSst Situacao { get; private set; }

    /// <summary>Motivo do cancelamento, quando cancelado; nulo enquanto vigente.</summary>
    public string? MotivoCancelamento { get; private set; }

    /// <summary>Agentes nocivos do periodo (Tabela 23), expostos somente pela raiz.</summary>
    public IReadOnlyCollection<AgenteNocivo> Agentes => _agentes;

    /// <summary>Indica se o periodo de exposicao ainda esta vigente (sem data de fim).</summary>
    public bool Vigente => Situacao == SituacaoRegistroSst.Registrado && FimExposicao is null;

    /// <summary>
    /// Inicia um periodo de exposicao a condicoes ambientais para um servidor (estado
    /// <see cref="SituacaoRegistroSst.Registrado"/>). Emite <see cref="ExposicaoAgenteNocivoIniciada"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor exposto.</param>
    /// <param name="inicioExposicao">Inicio do periodo.</param>
    /// <param name="setorAtividade">Setor/atividade (nao vazio, ate 255 caracteres).</param>
    /// <returns>Nova <see cref="ExposicaoAgenteNocivo"/> vigente.</returns>
    /// <exception cref="ArgumentException">Se o setor/atividade for vazio ou exceder o limite.</exception>
    public static ExposicaoAgenteNocivo Iniciar(
        Guid tenantId,
        ServidorId servidorId,
        DateOnly inicioExposicao,
        string setorAtividade)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(setorAtividade);
        var setorNormalizado = setorAtividade.Trim();
        if (setorNormalizado.Length > ComprimentoMaximoSetor)
        {
            throw new ArgumentException($"Setor/atividade excede {ComprimentoMaximoSetor} caracteres.", nameof(setorAtividade));
        }

        return new ExposicaoAgenteNocivo(
            ExposicaoAgenteNocivoId.New(),
            tenantId,
            servidorId,
            inicioExposicao,
            setorNormalizado);
    }

    /// <summary>Acrescenta um agente nocivo ao periodo de exposicao, deduplicando por igualdade de valor.</summary>
    /// <param name="agente">Agente nocivo (Tabela 23).</param>
    /// <exception cref="ArgumentNullException">Se o agente for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se o registro estiver cancelado.</exception>
    public void AdicionarAgente(AgenteNocivo agente)
    {
        ArgumentNullException.ThrowIfNull(agente);
        GarantirRegistrado();
        if (!_agentes.Contains(agente))
        {
            _agentes.Add(agente);
        }
    }

    /// <summary>
    /// Encerra o periodo de exposicao na data informada (gera o fim do periodo no PPP/S-2240). Emite
    /// <see cref="ExposicaoAgenteNocivoEncerrada"/>.
    /// </summary>
    /// <param name="fimExposicao">Data de fim (deve ser igual ou posterior ao inicio).</param>
    /// <exception cref="ArgumentException">Se o fim for anterior ao inicio.</exception>
    /// <exception cref="InvalidOperationException">Se o registro estiver cancelado ou ja encerrado.</exception>
    public void Encerrar(DateOnly fimExposicao)
    {
        GarantirRegistrado();
        if (FimExposicao is not null)
        {
            throw new InvalidOperationException("Periodo de exposicao ja encerrado.");
        }

        if (fimExposicao < InicioExposicao)
        {
            throw new ArgumentException("Fim da exposicao nao pode ser anterior ao inicio.", nameof(fimExposicao));
        }

        FimExposicao = fimExposicao;
        RaiseDomainEvent(new ExposicaoAgenteNocivoEncerrada(Id, ServidorId, fimExposicao));
    }

    /// <summary>Cancela o registro de exposicao (tornado sem efeito; estado terminal). Emite <see cref="RegistroSstCancelado"/>.</summary>
    /// <param name="motivo">Motivo do cancelamento (nao vazio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se ja cancelado.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirRegistrado();

        Situacao = SituacaoRegistroSst.Cancelado;
        MotivoCancelamento = motivo.Trim();
        RaiseDomainEvent(new RegistroSstCancelado(Id.Value, nameof(ExposicaoAgenteNocivo), MotivoCancelamento));
    }

    private void GarantirRegistrado()
    {
        if (Situacao == SituacaoRegistroSst.Cancelado)
        {
            throw new InvalidOperationException("Registro de exposicao cancelado e estado terminal; nao admite alteracao.");
        }
    }
}
