namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>Tipo de período (competência) da remessa ao TCE-RS.</summary>
public enum TipoPeriodo
{
    /// <summary>Mensal (numero 1..12).</summary>
    Mensal = 1,

    /// <summary>Bimestral (numero 1..6).</summary>
    Bimestre = 2,

    /// <summary>Quadrimestral (numero 1..3).</summary>
    Quadrimestre = 3,

    /// <summary>Anual (numero ignorado/0).</summary>
    Anual = 4,
}

/// <summary>
/// Situação (estado) da remessa no ciclo
/// <c>Gerada → Validada → ProntaParaTransmissao → Enviada → Homologada/Rejeitada</c>.
/// </summary>
/// <remarks>
/// O TCE-RS NÃO tem API de envio: a transmissão é MANUAL (PAD desktop + e-Protocolo + cert A3 pessoal).
/// Por isso <see cref="ProntaParaTransmissao"/> = "ZIP empacotado e disponibilizado para download"; e
/// <see cref="Enviada"/> = "operador registrou o protocolo/recibo retornado pelo portal" — NÃO um POST.
/// </remarks>
public enum SituacaoRemessaTce
{
    /// <summary>Pacote consolidado e montado (estado inicial), ainda não validado.</summary>
    Gerada = 1,

    /// <summary>RDI sem erro e hash íntegro — apta a empacotar.</summary>
    Validada = 2,

    /// <summary>Transmitida ao SIAPC/PAD (protocolo registrado pelo operador).</summary>
    Enviada = 3,

    /// <summary>Homologada pelo TCE-RS (terminal de sucesso).</summary>
    Homologada = 4,

    /// <summary>RDI com erro — envio bloqueado (terminal de falha; admite regeração).</summary>
    Rejeitada = 5,

    /// <summary>
    /// ZIP empacotado e disponibilizado para download; aguardando a transmissão MANUAL no PAD/e-Protocolo
    /// e o registro do protocolo/recibo pelo operador.
    /// </summary>
    ProntaParaTransmissao = 6,
}

/// <summary>Severidade de uma ocorrência do RDI (e-Validador).</summary>
public enum SeveridadeOcorrencia
{
    /// <summary>Aviso — não bloqueia o envio.</summary>
    Aviso = 1,

    /// <summary>Erro — bloqueia o envio (transita a remessa para Rejeitada).</summary>
    Erro = 2,
}
