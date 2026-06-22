namespace Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

/// <summary>
/// Opcoes da assinatura XML-DSig. O <see cref="Destino"/> seleciona o perfil criptografico
/// (canonicalizacao, KeyInfo, transforms) conforme A1-DESIGN §4 — o chamador NAO escolhe algoritmo
/// solto, evitando a "pegadinha" do default exc-c14n do .NET (A1-DESIGN §7, risco 4).
/// </summary>
/// <param name="Destino">Orgao receptor que define o perfil de assinatura.</param>
/// <param name="ReferenceUri">
/// URI do elemento assinado (vazio = documento inteiro; "#Id" = elemento com atributo Id).
/// </param>
public sealed record OpcoesAssinaturaXml(DestinoAssinatura Destino, string ReferenceUri = "");

/// <summary>
/// Opcoes da assinatura CMS/PKCS#7 (CAdES). Use apenas para artefatos NAO-XML quando o destino
/// exigir assinatura criptografica embutida (A1-DESIGN §4.2 — // TODO(validar-oficial)).
/// </summary>
/// <param name="Destino">Orgao receptor que define o perfil de assinatura.</param>
/// <param name="Detached">
/// Verdadeiro para assinatura destacada (conteudo nao embutido no envelope CMS); falso para anexada.
/// </param>
public sealed record OpcoesAssinaturaCms(DestinoAssinatura Destino, bool Detached = false);
