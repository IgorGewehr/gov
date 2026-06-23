namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;

/// <summary>
/// Eixo de condicionalidade do Programa Bolsa Familia (Lei 14.601/2023; Decreto 11.617/2023;
/// Portaria MDS 978/2023). As condicionalidades sao compromissos das familias beneficiarias nas
/// areas de educacao e saude — o descumprimento gera efeitos GRADATIVOS no acompanhamento.
/// </summary>
public enum TipoCondicionalidade
{
    /// <summary>
    /// Educacao: frequencia escolar minima — 60% para 4-5 anos; 75% para 6-17 anos
    /// (Portaria MEC/MDS; acompanhamento bimestral pelo MEC/Sistema Presenca).
    /// </summary>
    EducacaoFrequenciaEscolar = 1,

    /// <summary>Saude: acompanhamento do estado nutricional e calendario vacinal de criancas ate 7 anos.</summary>
    SaudeVacinacaoNutricaoInfantil = 2,

    /// <summary>Saude: pre-natal das gestantes da familia (acompanhamento semestral pelo MS).</summary>
    SaudePreNatalGestante = 3,
}

/// <summary>
/// Situacao do cumprimento de uma condicionalidade no periodo de acompanhamento.
/// </summary>
public enum StatusCondicionalidade
{
    /// <summary>Registrado, aguardando apuracao do periodo (estado inicial).</summary>
    Pendente = 1,

    /// <summary>Condicionalidade cumprida no periodo.</summary>
    Cumprida = 2,

    /// <summary>Condicionalidade descumprida — gera efeito gradativo (advertencia/bloqueio/suspensao).</summary>
    Descumprida = 3,

    /// <summary>Descumprimento justificado pela equipe (motivo registrado) — nao gera efeito.</summary>
    Justificada = 4,
}

/// <summary>
/// Efeito gradativo do descumprimento de condicionalidades do PBF (Decreto 11.617/2023 art. 38 e ss.;
/// Portaria MDS 978/2023). A gradacao e federal (a base MDS/SICON e autoritativa) — aqui registramos o
/// EFEITO GERENCIAL local para a busca ativa do CRAS; nao alteramos o beneficio federal (I-9 de Familia).
/// </summary>
public enum EfeitoDescumprimento
{
    /// <summary>Sem efeito (condicionalidades em dia ou descumprimento justificado).</summary>
    Nenhum = 0,

    /// <summary>Advertencia: primeiro registro de descumprimento (nao afeta o pagamento).</summary>
    Advertencia = 1,

    /// <summary>Bloqueio: segundo registro — parcela bloqueada, recuperavel.</summary>
    Bloqueio = 2,

    /// <summary>Suspensao: descumprimentos reiterados — beneficio suspenso por periodo.</summary>
    Suspensao = 3,
}
