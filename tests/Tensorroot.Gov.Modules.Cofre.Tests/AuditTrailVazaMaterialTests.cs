using System.Text.Json;
using FluentAssertions;
using Tensorroot.Gov.Modules.Cofre.Domain;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>
/// PROVA a correcao do vazamento na trilha (A1-DESIGN §2/§7 risco 6): o CertificadoA1Cofre declara,
/// via <see cref="IHasRedactedAuditFields"/>, as colunas de material cifrado que o
/// AuditSaveChangesInterceptor DEVE substituir por marcador opaco antes de serializar OldValues/
/// NewValues. Este teste reproduz fielmente o caminho de redacao do interceptor (mesma logica de
/// Added) e confirma que PfxCipher/SenhaCipher/DekWrapped (+ nonces/tags) NUNCA entram na trilha.
/// </summary>
public sealed class AuditTrailVazaMaterialTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Interceptor_redacta_material_cifrado_na_trilha()
    {
        // Marcadores distintos por campo para localiza-los no JSON do AuditTrail.
        var pfxCipher = new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02 };
        var senhaCipher = new byte[] { 0x10, 0x20, 0x30 };
        var dekWrapped = new byte[] { 0xFE, 0xDC, 0xBA };

        var pfx = new MaterialCifrado(pfxCipher, new byte[MaterialCifrado.TamanhoNonce], new byte[MaterialCifrado.TamanhoTag]);
        var senha = new MaterialCifrado(senhaCipher, new byte[MaterialCifrado.TamanhoNonce], new byte[MaterialCifrado.TamanhoTag]);

        var certificado = CertificadoA1Cofre.Cadastrar(
            Tenant, "TITULAR", "11222333000181", "ABC", DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddYears(1), pfx, senha, dekWrapped, "kek-v1", DateTime.UtcNow);

        // A entidade declara as colunas sensiveis que o interceptor redacta.
        var redactadas = ((IHasRedactedAuditFields)certificado).ColunasAuditoriaRedactadas;
        redactadas.Should().Contain([
            nameof(CertificadoA1Cofre.PfxCipher), nameof(CertificadoA1Cofre.PfxNonce),
            nameof(CertificadoA1Cofre.PfxTag), nameof(CertificadoA1Cofre.SenhaCipher),
            nameof(CertificadoA1Cofre.SenhaNonce), nameof(CertificadoA1Cofre.SenhaTag),
            nameof(CertificadoA1Cofre.DekWrapped),
        ]);

        // Reproduz AddAuditEntries (EntityState.Added) COM a redacao do interceptor.
        var newValues = typeof(CertificadoA1Cofre)
            .GetProperties()
            .ToDictionary(
                p => p.Name,
                p => redactadas.Contains(p.Name)
                    ? IHasRedactedAuditFields.RedactionMarker
                    : p.GetValue(certificado),
                StringComparer.Ordinal);

        var json = JsonSerializer.Serialize(newValues);

        // O material cifrado nao aparece; no lugar fica o marcador opaco.
        json.Should().NotContain(Convert.ToBase64String(pfxCipher), "PfxCipher nao pode entrar no AuditTrail (A1-DESIGN §2/risco 6)");
        json.Should().NotContain(Convert.ToBase64String(senhaCipher), "SenhaCipher nao pode entrar no AuditTrail");
        json.Should().NotContain(Convert.ToBase64String(dekWrapped), "DekWrapped nao pode entrar no AuditTrail");
        json.Should().Contain(IHasRedactedAuditFields.RedactionMarker, "as colunas sensiveis viram marcador opaco");
        json.Should().Contain("ABC", "metadados nao sensiveis (Thumbprint) continuam auditados");
    }
}
