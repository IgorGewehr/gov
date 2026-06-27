namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;

/// <summary>
/// Códigos de Poder/Órgão (PO) da Matriz de Saldos Contábeis (Regras Gerais MSC 2026 — Anexo I da Portaria
/// STN 642/2019, IC nº1 "Poder ou Órgão"). O PO tem 5 dígitos: os 2 primeiros identificam o Poder e os 3
/// últimos o Órgão (art. 20 da LRF). A MSC é enviada EXCLUSIVAMENTE pelo Poder Executivo, que consolida e
/// destaca os registros dos demais poderes/órgãos via a própria informação complementar PO.
/// </summary>
public static class PoderOrgaoMsc
{
    /// <summary>Quantidade de dígitos do código PO (2 poder + 3 órgão).</summary>
    public const int Digitos = 5;

    /// <summary>
    /// PO do Poder Executivo (poder <c>01</c>, órgão <c>001</c>). Default da MSC, que é enviada apenas pelo
    /// Executivo; os demais poderes/órgãos do ente entram nas linhas próprias com seus respectivos PO.
    /// </summary>
    public const string Executivo = "01001";

    /// <summary>PO do Poder Legislativo (poder <c>02</c>, órgão <c>001</c>).</summary>
    public const string Legislativo = "02001";
}
