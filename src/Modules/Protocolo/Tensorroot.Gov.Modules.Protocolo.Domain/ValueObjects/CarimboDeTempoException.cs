namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Codigos de falha do protocolo RFC 3161 (PKIFailureInfo) que a ACT pode retornar em uma rejeicao,
/// alem das falhas de validacao do cliente (imprint/nonce/cadeia/genTime). Mapeamento explicito de
/// erro (CLAUDE.md S8) — nada de string solta.
/// </summary>
public enum FalhaCarimboDeTempo
{
    /// <summary>Algoritmo de hash nao reconhecido ou nao suportado (badAlg).</summary>
    BadAlg = 1,

    /// <summary>Requisicao malformada (badRequest).</summary>
    BadRequest = 2,

    /// <summary>Formato de dados invalido (badDataFormat).</summary>
    BadDataFormat = 3,

    /// <summary>Fonte de tempo confiavel indisponivel (timeNotAvailable).</summary>
    TimeNotAvailable = 4,

    /// <summary>Politica de carimbo solicitada nao aceita (unacceptedPolicy).</summary>
    UnacceptedPolicy = 5,

    /// <summary>Extensao solicitada nao aceita (unacceptedExtension).</summary>
    UnacceptedExtension = 6,

    /// <summary>Informacao adicional indisponivel (addInfoNotAvailable).</summary>
    AddInfoNotAvailable = 7,

    /// <summary>Falha interna da ACT (systemFailure).</summary>
    SystemFailure = 8,

    /// <summary>MessageImprint do TSTInfo nao bate com o hash enviado (validacao do cliente).</summary>
    ImprintDivergente = 20,

    /// <summary>Nonce ecoado difere do enviado — possivel replay (validacao do cliente).</summary>
    NonceDivergente = 21,

    /// <summary>Cadeia/assinatura do TST nao encadeia a uma AC do tempo confiavel ou EKU ausente.</summary>
    CadeiaNaoConfiavel = 22,

    /// <summary>genTime fora da janela tolerada (validacao do cliente).</summary>
    GenTimeForaDaJanela = 23,

    /// <summary>Status PKI nao concedido sem PKIFailureInfo discriminado.</summary>
    StatusNaoConcedido = 24,
}

/// <summary>
/// Erro de dominio do carimbo de tempo RFC 3161: a solicitacao a ACT foi rejeitada (PKIFailureInfo)
/// ou a validacao do cliente (imprint/nonce/cadeia/EKU/genTime) falhou. Carrega a
/// <see cref="Falha"/> mapeada para diagnostico explicito. Quando lancada, NENHUM carimbo e
/// persistido — a assinatura nao prossegue (oponibilidade ao TCE preservada).
/// </summary>
public sealed class CarimboDeTempoException : Exception
{
    /// <summary>Cria a excecao com a falha mapeada e mensagem opcional.</summary>
    /// <param name="falha">Falha do protocolo/validacao.</param>
    /// <param name="detalhe">Detalhe adicional (opcional).</param>
    public CarimboDeTempoException(FalhaCarimboDeTempo falha, string? detalhe = null)
        : base(MontarMensagem(falha, detalhe))
        => Falha = falha;

    /// <summary>Falha do protocolo RFC 3161 / validacao do cliente.</summary>
    public FalhaCarimboDeTempo Falha { get; }

    private static string MontarMensagem(FalhaCarimboDeTempo falha, string? detalhe)
    {
        var baseMsg = $"Carimbo de tempo (RFC 3161) recusado: {falha}.";
        return string.IsNullOrWhiteSpace(detalhe) ? baseMsg : $"{baseMsg} {detalhe}";
    }
}
