using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Metadata;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Politica UNICA de redacao de PII na trilha de auditoria (LG-3) — fonte de verdade compartilhada
/// pelo <see cref="AuditSaveChangesInterceptor"/> (que redige na ESCRITA) e pelo visualizador
/// administrativo (que mascara o que ainda estiver em claro na LEITURA, ex.: linhas legadas).
/// <para>
/// A redacao e DENY-BY-DEFAULT por CONVENCAO, nao opt-in: uma propriedade e redigida quando
/// (a) a entidade a declara material cifrado via <see cref="IHasRedactedAuditFields"/> (Cofre);
/// (b) carrega o atributo <see cref="CampoSensivelLgpdAttribute"/>; ou (c) seu nome casa com a
/// convencao de campos sensiveis (CPF, NIS, CNS, dados clinicos). Assim, CPF/NIS/CNS nunca sao
/// gravados em claro mesmo que o desenvolvedor esqueca de anotar a entidade.
/// </para>
/// </summary>
public static class PoliticaRedacaoAuditoria
{
    /// <summary>Marcador opaco que substitui o valor de uma coluna sensivel na trilha.</summary>
    public const string Marcador = IHasRedactedAuditFields.RedactionMarker;

    // Nomes (CLR/coluna) reconhecidos como PII por CONVENCAO, independentemente de atributo — rede de
    // seguranca para entidades nao anotadas. Inclui APENAS identificadores INEQUIVOCAMENTE pessoais:
    // nomes genericos (Codigo/Descricao) ficam de fora para nao redigir campos NAO-pessoais de outros
    // modulos (ex.: codigo de tributo/processo no TCE). Dado clinico e marcado por atributo na
    // propria entidade (CondicaoDeSaude/Alergia). Comparacao case-insensitive.
    private static readonly HashSet<string> NomesSensiveisPorConvencao = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cpf", "CpfResponsavel", "Nis", "Cns", "Pis", "Pasep", "NumeroCns", "NumeroNis",
    };

    // Mascaramento de LEITURA (visualizador): pode ser mais ABRANGENTE que a redacao de escrita,
    // pois mascarar em excesso uma resposta nunca vaza — cobre tambem chaves clinicas de linhas
    // LEGADAS gravadas em claro antes do LG-3 (quando ainda nao havia atributo nas entidades).
    private static readonly HashSet<string> NomesMascaradosNaLeitura = new(NomesSensiveisPorConvencao, StringComparer.OrdinalIgnoreCase)
    {
        "Cid", "Cid10", "Codigo", "Descricao", "Substancia", "Gravidade", "Diagnostico", "Anamnese",
    };

    /// <summary>
    /// Indica se uma propriedade EF deve ser REDIGIDA na trilha (decisao na escrita). Consulta, nesta
    /// ordem: material cifrado opt-in (Cofre) → atributo <see cref="CampoSensivelLgpdAttribute"/> →
    /// convencao de nome. Qualquer uma positiva implica redacao.
    /// </summary>
    /// <param name="property">Metadado da propriedade no modelo EF.</param>
    /// <param name="colunasCifradas">Colunas cipher declaradas pela entidade (Cofre), ou nulo.</param>
    /// <returns><c>true</c> se o valor da propriedade nao pode ir em claro para a trilha.</returns>
    public static bool DeveRedigir(IProperty property, IReadOnlySet<string>? colunasCifradas)
    {
        ArgumentNullException.ThrowIfNull(property);

        var nome = property.Name;

        if (colunasCifradas is not null && colunasCifradas.Contains(nome))
        {
            return true;
        }

        if (property.PropertyInfo?.GetCustomAttribute<CampoSensivelLgpdAttribute>() is not null)
        {
            return true;
        }

        return NomesSensiveisPorConvencao.Contains(nome);
    }

    /// <summary>
    /// Mascara, num blob JSON ja serializado de <c>OldValues</c>/<c>NewValues</c>, os valores cujas
    /// CHAVES sejam sensiveis por convencao ou ja estejam redigidas. Usado pelo VISUALIZADOR para
    /// nunca devolver PII em claro — inclusive de linhas LEGADAS gravadas antes de LG-3. Retorna o
    /// proprio valor quando nao ha o que mascarar (ou quando o JSON e invalido — fail-safe: nao
    /// vaza, mantem a string como veio apenas se nao parsear como objeto).
    /// <para>
    /// A varredura e RECURSIVA (objetos e arrays aninhados): PII guardada em Value Object OWNED/
    /// serializado como sub-objeto (ex.: <c>Identificacao.Cpf.Digitos</c> do Paciente, ou qualquer
    /// VO cujo conversor produz JSON aninhado) tambem e mascarada. Quando uma chave sensivel e
    /// encontrada, TODO o seu valor — escalar, objeto ou array — e substituido pelo marcador, de modo
    /// que o conteudo interno (ex.: <c>{"Digitos":"..."}</c>) nunca vaze por ter ficado fora do
    /// nivel raiz. Deny-by-default: na duvida, mascara.
    /// </para>
    /// </summary>
    /// <param name="json">Conteudo JSON da coluna de valores (pode ser nulo).</param>
    /// <returns>JSON com os campos sensiveis mascarados, ou o valor original quando nao aplicavel.</returns>
    public static string? MascararJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            // Conteudo nao-JSON: nao ha chaves a inspecionar; devolve como veio.
            return json;
        }

        var alterou = MascararNo(node);
        return alterou ? node!.ToJsonString() : json;
    }

    /// <summary>
    /// Percorre recursivamente um no JSON mascarando, em qualquer profundidade, os valores cujas
    /// CHAVES sejam sensiveis. Retorna <c>true</c> se algo foi mascarado (para evitar re-serializar a
    /// toa). Uma chave sensivel tem TODO o seu valor (escalar/objeto/array) trocado pelo marcador.
    /// </summary>
    private static bool MascararNo(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject objeto:
            {
                var alterou = false;
                foreach (var par in objeto.ToList())
                {
                    if (NomesMascaradosNaLeitura.Contains(par.Key))
                    {
                        objeto[par.Key] = Marcador;
                        alterou = true;
                        continue;
                    }

                    alterou |= MascararNo(par.Value);
                }

                return alterou;
            }

            case JsonArray array:
            {
                var alterou = false;
                foreach (var item in array)
                {
                    alterou |= MascararNo(item);
                }

                return alterou;
            }

            default:
                return false;
        }
    }
}
