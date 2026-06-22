using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Núcleo da CADEIA DE HASH (hash-chain) da trilha de auditoria — fonte ÚNICA de verdade do
/// conteúdo canônico de uma linha e do cálculo do selo, compartilhada entre o
/// <see cref="AuditSaveChangesInterceptor"/> (que sela ao inserir) e o verificador (que recomputa).
/// <para>
/// Cada linha guarda <c>HashAnterior</c> e <c>HashAtual = SHA-256(HashAnterior ‖ conteúdo canônico)</c>,
/// encadeada POR TENANT na ordem de gravação (<c>Sequencia</c>). Adulterar/remover qualquer linha
/// quebra o elo seguinte, e o verificador aponta a 1ª divergência. É detecção determinística e
/// reproduzível — não depende do banco impor imutabilidade (embora em produção SqlServer um trigger
/// INSTEAD OF UPDATE/DELETE bloqueie a alteração na origem).
/// </para>
/// </summary>
public static class AuditHashChain
{
    /// <summary>
    /// Hash-semente (genesis) usado como <c>HashAnterior</c> da PRIMEIRA linha de cada tenant.
    /// Valor fixo e público — o que protege a cadeia é o encadeamento, não o segredo da semente.
    /// </summary>
    public const string HashGenesis = "GENESIS";

    // Separadores: caracteres de controle ASCII (Unit/Record Separator) ausentes dos dados textuais,
    // evitando colisões de canonicalização (ex.: campo terminando em separador que outro começa).
    private const char SeparadorCampo = '\u001F';
    private const string SeparadorElo = "\u001E";

    /// <summary>
    /// Monta o CONTEÚDO CANÔNICO determinístico de uma linha (campos de negócio na ordem fixa,
    /// separados por um caractere de controle). Não inclui <c>HashAnterior</c>/<c>HashAtual</c>:
    /// o encadeamento é aplicado em <see cref="Selar"/>. Datas em ISO-8601 invariante (round-trip).
    /// </summary>
    public static string ConteudoCanonico(
        Guid id,
        Guid tenantId,
        long sequencia,
        string entityName,
        string? entityId,
        string action,
        string? oldValues,
        string? newValues,
        string? affectedColumns,
        string? userId,
        string? ipAddress,
        DateTime timestampUtc)
    {
        var builder = new StringBuilder(256);
        builder.Append(id.ToString("N", CultureInfo.InvariantCulture)).Append(SeparadorCampo);
        builder.Append(tenantId.ToString("N", CultureInfo.InvariantCulture)).Append(SeparadorCampo);
        builder.Append(sequencia.ToString(CultureInfo.InvariantCulture)).Append(SeparadorCampo);
        builder.Append(entityName).Append(SeparadorCampo);
        builder.Append(entityId ?? string.Empty).Append(SeparadorCampo);
        builder.Append(action).Append(SeparadorCampo);
        builder.Append(oldValues ?? string.Empty).Append(SeparadorCampo);
        builder.Append(newValues ?? string.Empty).Append(SeparadorCampo);
        builder.Append(affectedColumns ?? string.Empty).Append(SeparadorCampo);
        builder.Append(userId ?? string.Empty).Append(SeparadorCampo);
        builder.Append(ipAddress ?? string.Empty).Append(SeparadorCampo);
        // Normaliza para milissegundos E força Kind=Utc: o selo precisa ser ESTÁVEL ao round-trip de
        // persistência. Provedores como SQLite (a) não preservam os 7 dígitos fracionários do "O" e
        // (b) devolvem Kind=Unspecified (sem o sufixo 'Z'), o que mudaria a string canônica. O
        // interceptor grava truncado; normalizar aqui blinda a recomputação do verificador.
        var ticksMs = timestampUtc.Ticks - (timestampUtc.Ticks % TimeSpan.TicksPerMillisecond);
        var instante = new DateTime(ticksMs, DateTimeKind.Utc);
        builder.Append(instante.ToString("O", CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    /// <summary>
    /// Calcula o selo da linha: <c>Base64(SHA-256(hashAnterior ‖ conteúdoCanônico))</c>.
    /// </summary>
    /// <param name="hashAnterior">Selo da linha anterior do tenant (ou <see cref="HashGenesis"/>).</param>
    /// <param name="conteudoCanonico">Conteúdo canônico de <see cref="ConteudoCanonico"/>.</param>
    /// <returns>Selo Base64 da linha.</returns>
    public static string Selar(string hashAnterior, string conteudoCanonico)
    {
        ArgumentNullException.ThrowIfNull(hashAnterior);
        ArgumentNullException.ThrowIfNull(conteudoCanonico);

        var material = string.Concat(hashAnterior, SeparadorElo, conteudoCanonico);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Conveniência: monta o conteúdo canônico de uma <see cref="AuditTrail"/> já preenchida e
    /// recomputa seu selo a partir de <paramref name="hashAnterior"/>. Usado pelo verificador.
    /// </summary>
    /// <param name="linha">Linha de trilha a selar/recomputar.</param>
    /// <param name="hashAnterior">Selo esperado da linha anterior (ou <see cref="HashGenesis"/>).</param>
    /// <returns>Selo recomputado da linha.</returns>
    public static string Recomputar(AuditTrail linha, string hashAnterior)
    {
        ArgumentNullException.ThrowIfNull(linha);

        var conteudo = ConteudoCanonico(
            linha.Id,
            linha.TenantId,
            linha.Sequencia,
            linha.EntityName,
            linha.EntityId,
            linha.Action,
            linha.OldValues,
            linha.NewValues,
            linha.AffectedColumns,
            linha.UserId,
            linha.IpAddress,
            linha.TimestampUtc);

        return Selar(hashAnterior, conteudo);
    }
}
