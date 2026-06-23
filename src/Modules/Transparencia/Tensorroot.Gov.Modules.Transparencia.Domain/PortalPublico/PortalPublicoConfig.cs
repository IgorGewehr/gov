using System.Globalization;
using System.Text;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

/// <summary>Identificador forte de <see cref="PortalPublicoConfig"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PortalPublicoConfigId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PortalPublicoConfigId"/>.</returns>
    public static PortalPublicoConfigId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Configuracao do PORTAL PUBLICO de um tenant: o <see cref="Slug"/> publico (ex.:
/// <c>maximiliano-de-almeida</c>) que identifica o ente na URL anonima <c>/publico/transparencia/{slug}</c>
/// e o nome de exibicao. Vive no schema do PROPRIO modulo Transparencia (tenant-scoped pelo Global Query
/// Filter), evitando alterar o catalogo da Plataforma. O resolver publico (anonimo) traduz slug -&gt;
/// TenantId consultando ESTE registro por um caminho de leitura dedicado e auditado (sem expor dado
/// sensivel — so o mapa slug/nome). Unicidade do slug garantida por indice unico (TenantId, Slug) + o
/// catalogo cruzado e a verificacao no resolver.
/// </summary>
public sealed class PortalPublicoConfig : AggregateRoot<PortalPublicoConfigId>, IMustHaveTenant
{
    private PortalPublicoConfig()
    {
    }

    private PortalPublicoConfig(PortalPublicoConfigId id, Guid tenantId, string slug, string nomeEnte)
        : base(id)
    {
        TenantId = tenantId;
        Slug = slug;
        NomeEnte = nomeEnte;
        Ativo = true;
    }

    /// <summary>Tenant (ente publico) dono da configuracao.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Slug publico unico do ente (kebab-case, sem acento) usado na URL anonima.</summary>
    public string Slug { get; private set; } = default!;

    /// <summary>Nome de exibicao do ente no portal publico.</summary>
    public string NomeEnte { get; private set; } = default!;

    /// <summary>Indica se o portal publico esta ativo para o ente.</summary>
    public bool Ativo { get; private set; }

    /// <summary>
    /// Cria a configuracao do portal publico, normalizando o slug para kebab-case ASCII (sem acento, sem
    /// espaco, sem maiuscula). O slug e a chave estavel da URL publica — invariante: nao vazio apos
    /// normalizacao.
    /// </summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="slug">Slug bruto (sera normalizado).</param>
    /// <param name="nomeEnte">Nome de exibicao do ente.</param>
    /// <returns>Nova <see cref="PortalPublicoConfig"/> ativa.</returns>
    /// <exception cref="ArgumentException">Se o slug normalizado ou o nome forem vazios.</exception>
    public static PortalPublicoConfig Criar(Guid tenantId, string slug, string nomeEnte)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeEnte);
        var normalizado = NormalizarSlug(slug);
        if (normalizado.Length == 0)
        {
            throw new ArgumentException("Slug publico invalido apos normalizacao.", nameof(slug));
        }

        return new PortalPublicoConfig(PortalPublicoConfigId.New(), tenantId, normalizado, nomeEnte.Trim());
    }

    /// <summary>Atualiza o slug (re-normaliza).</summary>
    /// <param name="slug">Novo slug bruto.</param>
    /// <exception cref="ArgumentException">Se o slug normalizado for vazio.</exception>
    public void AlterarSlug(string slug)
    {
        var normalizado = NormalizarSlug(slug);
        if (normalizado.Length == 0)
        {
            throw new ArgumentException("Slug publico invalido apos normalizacao.", nameof(slug));
        }

        Slug = normalizado;
    }

    /// <summary>Ativa o portal publico do ente.</summary>
    public void Ativar() => Ativo = true;

    /// <summary>Desativa o portal publico do ente (rotas anonimas passam a 404).</summary>
    public void Desativar() => Ativo = false;

    /// <summary>
    /// Normaliza um slug para kebab-case ASCII: remove acentos, troca nao-alfanumericos por hifen,
    /// colapsa hifens e baixa para minusculas. Deterministico (sem relogio).
    /// </summary>
    /// <param name="valor">Slug bruto.</param>
    /// <returns>Slug normalizado (pode ser vazio se a entrada nao tiver alfanumericos).</returns>
    public static string NormalizarSlug(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var semAcento = RemoverAcentos(valor).ToLowerInvariant();
        var builder = new StringBuilder(semAcento.Length);
        var ultimoFoiHifen = false;
        foreach (var caractere in semAcento)
        {
            if (char.IsLetterOrDigit(caractere) && caractere < 128)
            {
                builder.Append(caractere);
                ultimoFoiHifen = false;
            }
            else if (!ultimoFoiHifen)
            {
                builder.Append('-');
                ultimoFoiHifen = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string RemoverAcentos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposto.Length);
        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caractere);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
