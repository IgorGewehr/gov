using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;

/// <summary>Identificador forte do agregado <see cref="ProcessoTrabalhista"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProcessoTrabalhistaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProcessoTrabalhistaId"/>.</returns>
    public static ProcessoTrabalhistaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Processo trabalhista em que o ente publico figura como reclamado (Justica do Trabalho): cadastro e
/// acompanhamento do numero do processo (CNJ), vara/orgao julgador, reclamante (e servidor vinculado,
/// quando ex-empregado), objeto/pedidos, valores (causa, acordo, condenacao) e a SITUACAO processual,
/// com o controle da PROVISAO CONTABIL (NBC TG 25/CPC 25): so o prognostico PROVAVEL gera valor
/// provisionado (passivo reconhecido); POSSIVEL e' passivo contingente (divulgacao); REMOTA, nada.
/// Raiz de agregado, nasce valida via <see cref="Cadastrar"/>.
/// </summary>
public sealed class ProcessoTrabalhista : AggregateRoot<ProcessoTrabalhistaId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do numero do processo (formato CNJ: 20 digitos + mascara).</summary>
    public const int ComprimentoMaximoNumero = 30;

    /// <summary>Comprimento maximo da descricao do objeto/pedidos.</summary>
    public const int ComprimentoMaximoObjeto = 2_000;

    private ProcessoTrabalhista()
    {
    }

    private ProcessoTrabalhista(
        ProcessoTrabalhistaId id,
        Guid tenantId,
        string numeroProcesso,
        string vara,
        string reclamante,
        ServidorId? servidorId,
        string objeto,
        decimal valorCausa,
        DateOnly dataAjuizamento,
        PrognosticoPerda prognostico)
        : base(id)
    {
        TenantId = tenantId;
        NumeroProcesso = numeroProcesso;
        Vara = vara;
        Reclamante = reclamante;
        ServidorId = servidorId;
        Objeto = objeto;
        ValorCausa = valorCausa;
        DataAjuizamento = dataAjuizamento;
        Prognostico = prognostico;
        Situacao = SituacaoProcessoTrabalhista.EmAndamento;
        ValorProvisionado = CalcularProvisao(prognostico, valorCausa);
        RaiseDomainEvent(new ProcessoTrabalhistaCadastrado(id, numeroProcesso, valorCausa, ValorProvisionado));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Numero unico do processo (padrao CNJ) no tenant.</summary>
    public string NumeroProcesso { get; private set; } = default!;

    /// <summary>Vara / orgao julgador (ex.: "1a Vara do Trabalho de ...").</summary>
    public string Vara { get; private set; } = default!;

    /// <summary>Nome do reclamante (autor da reclamatoria).</summary>
    public string Reclamante { get; private set; } = default!;

    /// <summary>Servidor (ex-servidor) vinculado, quando o reclamante integrou o quadro (opcional).</summary>
    public ServidorId? ServidorId { get; private set; }

    /// <summary>Objeto / pedidos da reclamatoria.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Valor da causa (BRL).</summary>
    public decimal ValorCausa { get; private set; }

    /// <summary>Valor do acordo homologado (BRL), quando houver; nulo enquanto inexistente.</summary>
    public decimal? ValorAcordo { get; private set; }

    /// <summary>Valor da condenacao transitada em julgado (BRL), quando houver; nulo enquanto inexistente.</summary>
    public decimal? ValorCondenacao { get; private set; }

    /// <summary>Data de ajuizamento da reclamatoria.</summary>
    public DateOnly DataAjuizamento { get; private set; }

    /// <summary>Data de encerramento (acordo/transito/arquivamento); nula enquanto em andamento.</summary>
    public DateOnly? DataEncerramento { get; private set; }

    /// <summary>Prognostico de perda (NBC TG 25) — fonte do valor provisionado.</summary>
    public PrognosticoPerda Prognostico { get; private set; }

    /// <summary>
    /// Valor PROVISIONADO (BRL): igual ao valor da causa enquanto provavel e em andamento; zero quando
    /// possivel/remoto; e o valor efetivo (acordo/condenacao) ao encerrar com saida de recursos.
    /// </summary>
    public decimal ValorProvisionado { get; private set; }

    /// <summary>Situacao processual atual.</summary>
    public SituacaoProcessoTrabalhista Situacao { get; private set; }

    /// <summary>
    /// Cadastra um processo trabalhista contra o ente. Define o valor provisionado a partir do
    /// prognostico inicial e nasce <see cref="SituacaoProcessoTrabalhista.EmAndamento"/>, emitindo
    /// <see cref="ProcessoTrabalhistaCadastrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="numeroProcesso">Numero unico do processo (CNJ).</param>
    /// <param name="vara">Vara/orgao julgador.</param>
    /// <param name="reclamante">Nome do reclamante.</param>
    /// <param name="servidorId">Servidor vinculado (opcional).</param>
    /// <param name="objeto">Objeto/pedidos da reclamatoria.</param>
    /// <param name="valorCausa">Valor da causa (maior ou igual a zero).</param>
    /// <param name="dataAjuizamento">Data de ajuizamento.</param>
    /// <param name="prognostico">Prognostico de perda inicial.</param>
    /// <returns>Novo <see cref="ProcessoTrabalhista"/> em situacao <see cref="SituacaoProcessoTrabalhista.EmAndamento"/>.</returns>
    /// <exception cref="ArgumentException">Se numero/vara/reclamante/objeto forem vazios ou excederem o limite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor da causa for negativo.</exception>
    public static ProcessoTrabalhista Cadastrar(
        Guid tenantId,
        string numeroProcesso,
        string vara,
        string reclamante,
        ServidorId? servidorId,
        string objeto,
        decimal valorCausa,
        DateOnly dataAjuizamento,
        PrognosticoPerda prognostico)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroProcesso);
        ArgumentException.ThrowIfNullOrWhiteSpace(vara);
        ArgumentException.ThrowIfNullOrWhiteSpace(reclamante);
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        ArgumentOutOfRangeException.ThrowIfNegative(valorCausa);

        var numero = numeroProcesso.Trim();
        if (numero.Length > ComprimentoMaximoNumero)
        {
            throw new ArgumentException($"Numero do processo excede {ComprimentoMaximoNumero} caracteres.", nameof(numeroProcesso));
        }

        var objetoNormalizado = objeto.Trim();
        if (objetoNormalizado.Length > ComprimentoMaximoObjeto)
        {
            throw new ArgumentException($"Objeto do processo excede {ComprimentoMaximoObjeto} caracteres.", nameof(objeto));
        }

        return new ProcessoTrabalhista(
            ProcessoTrabalhistaId.New(),
            tenantId,
            numero,
            vara.Trim(),
            reclamante.Trim(),
            servidorId,
            objetoNormalizado,
            valorCausa,
            dataAjuizamento,
            prognostico);
    }

    /// <summary>
    /// Atualiza o prognostico de perda (reavaliacao periodica do juridico). Enquanto em andamento,
    /// recalcula o valor provisionado (provavel = valor da causa; demais = zero) e emite
    /// <see cref="ProvisaoProcessoAtualizada"/>.
    /// </summary>
    /// <param name="novoPrognostico">Novo prognostico de perda.</param>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver em andamento.</exception>
    public void ReavaliarPrognostico(PrognosticoPerda novoPrognostico)
    {
        GarantirEmAndamento();
        Prognostico = novoPrognostico;
        ValorProvisionado = CalcularProvisao(novoPrognostico, ValorCausa);
        RaiseDomainEvent(new ProvisaoProcessoAtualizada(Id, Prognostico, ValorProvisionado));
    }

    /// <summary>
    /// Homologa um acordo encerrando o processo: grava o valor do acordo, ajusta a provisao ao valor
    /// efetivo e transita para <see cref="SituacaoProcessoTrabalhista.Acordo"/>.
    /// </summary>
    /// <param name="valorAcordo">Valor homologado do acordo (maior ou igual a zero).</param>
    /// <param name="dataAcordo">Data da homologacao.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor do acordo for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver em andamento.</exception>
    public void RegistrarAcordo(decimal valorAcordo, DateOnly dataAcordo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valorAcordo);
        GarantirEmAndamento();
        ValorAcordo = valorAcordo;
        ValorProvisionado = valorAcordo;
        DataEncerramento = dataAcordo;
        Situacao = SituacaoProcessoTrabalhista.Acordo;
        RaiseDomainEvent(new ProcessoTrabalhistaEncerrado(Id, Situacao, valorAcordo));
    }

    /// <summary>
    /// Registra o transito em julgado com condenacao do ente: grava o valor da condenacao, fixa a
    /// provisao no valor efetivo e transita para <see cref="SituacaoProcessoTrabalhista.Condenado"/>.
    /// </summary>
    /// <param name="valorCondenacao">Valor da condenacao (maior ou igual a zero).</param>
    /// <param name="dataTransito">Data do transito em julgado.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor da condenacao for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver em andamento.</exception>
    public void RegistrarCondenacao(decimal valorCondenacao, DateOnly dataTransito)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valorCondenacao);
        GarantirEmAndamento();
        ValorCondenacao = valorCondenacao;
        ValorProvisionado = valorCondenacao;
        DataEncerramento = dataTransito;
        Situacao = SituacaoProcessoTrabalhista.Condenado;
        RaiseDomainEvent(new ProcessoTrabalhistaEncerrado(Id, Situacao, valorCondenacao));
    }

    /// <summary>
    /// Encerra o processo sem saida de recursos para o ente (improcedencia/extincao): zera a provisao e
    /// transita para <see cref="SituacaoProcessoTrabalhista.Improcedente"/>.
    /// </summary>
    /// <param name="dataTransito">Data do transito em julgado.</param>
    /// <exception cref="InvalidOperationException">Se o processo nao estiver em andamento.</exception>
    public void RegistrarImprocedencia(DateOnly dataTransito)
    {
        GarantirEmAndamento();
        ValorProvisionado = 0m;
        DataEncerramento = dataTransito;
        Situacao = SituacaoProcessoTrabalhista.Improcedente;
        RaiseDomainEvent(new ProcessoTrabalhistaEncerrado(Id, Situacao, 0m));
    }

    /// <summary>Arquiva definitivamente o processo ja encerrado (acordo/condenacao/improcedencia).</summary>
    /// <exception cref="InvalidOperationException">Se o processo estiver em andamento ou ja arquivado.</exception>
    public void Arquivar()
    {
        if (Situacao is SituacaoProcessoTrabalhista.EmAndamento or SituacaoProcessoTrabalhista.Arquivado)
        {
            throw new InvalidOperationException("So e' possivel arquivar um processo ja encerrado (acordo/condenacao/improcedencia).");
        }

        Situacao = SituacaoProcessoTrabalhista.Arquivado;
    }

    private void GarantirEmAndamento()
    {
        if (Situacao != SituacaoProcessoTrabalhista.EmAndamento)
        {
            throw new InvalidOperationException($"Operacao exige processo em andamento. Situacao atual: {Situacao}.");
        }
    }

    // Provisao contabil (NBC TG 25): provavel => provisiona o valor estimado (causa); possivel/remoto => 0.
    private static decimal CalcularProvisao(PrognosticoPerda prognostico, decimal valorEstimado)
        => prognostico == PrognosticoPerda.Provavel ? valorEstimado : 0m;
}
