using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure;

/// <summary>
/// Endpoints HTTP do modulo Cofre. Admin (cadastrar/rotacionar/consultar-status) exige
/// <c>admin.certificado.gerenciar</c>; a assinatura exige <c>documentos.assinar</c>. NENHUM endpoint
/// baixa/retorna o .pfx, a senha ou a chave privada (A1-DESIGN §6) — so metadados e o artefato assinado.
/// </summary>
internal static class CofreEndpoints
{
    // Permissoes do catalogo canonico (Identidade.Domain.Permissoes), referenciadas como string
    // literal para preservar o isolamento de modulo (CLAUDE.md §2: nenhum modulo referencia o
    // interno de outro). DEVEM coincidir com o catalogo: admin.certificado.gerenciar / documentos.assinar.
    private const string PermissaoGerenciarCertificado = "admin.certificado.gerenciar";
    private const string PermissaoAssinar = "documentos.assinar";

    // Limite defensivo de upload do .pfx (A1 raramente passa de algumas dezenas de KB).
    private const long TamanhoMaximoPfx = 256 * 1024;

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/cofre").WithTags("Cofre");

        // === Custodia (RBAC: admin.certificado.gerenciar) ===
        var admin = grupo.MapGroup(string.Empty).RequirePermission(PermissaoGerenciarCertificado);

        // Cadastrar/rotacionar: recebe o .pfx (multipart) + senha SO EM MEMORIA; cifra e persiste.
        admin.MapPost("/certificados", async (
            HttpRequest request,
            ServicoCustodiaCertificado custodia,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { erro = "Envie o .pfx via multipart/form-data (campos 'pfx', 'senha', 'cnpj')." });
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var arquivo = form.Files["pfx"];
            var senha = form["senha"].ToString();
            var cnpj = form["cnpj"].ToString();

            if (arquivo is null || arquivo.Length == 0 || string.IsNullOrEmpty(senha) || string.IsNullOrWhiteSpace(cnpj))
            {
                return Results.BadRequest(new { erro = "Campos obrigatorios: 'pfx' (arquivo), 'senha', 'cnpj'." });
            }

            if (arquivo.Length > TamanhoMaximoPfx)
            {
                return Results.BadRequest(new { erro = "Arquivo .pfx excede o tamanho maximo permitido." });
            }

            using var memoria = new MemoryStream();
            await arquivo.CopyToAsync(memoria, cancellationToken);

            try
            {
                var id = await custodia.CadastrarOuRotacionarAsync(cnpj, memoria.ToArray(), senha, cancellationToken);
                return Results.Ok(new { id });
            }
            catch (CertificadoInvalidoException excecao)
            {
                return Results.BadRequest(new { erro = excecao.Message });
            }
        }).DisableAntiforgery();

        // Consultar status do A1 ativo (NUNCA baixa o cert/chave — so metadados).
        admin.MapGet("/certificados/ativo", async (
            IServicoAssinaturaDigital assinatura,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var info = await assinatura.ObterInfoCertificadoAsync(cancellationToken);
                return Results.Ok(info);
            }
            catch (CertificadoInvalidoException excecao)
            {
                return Results.NotFound(new { erro = excecao.Message });
            }
        });

        // === Assinatura (RBAC: documentos.assinar) ===
        var assinar = grupo.MapGroup(string.Empty).RequirePermission(PermissaoAssinar);

        assinar.MapPost("/assinar/xml", async (
            AssinarXmlPayload payload,
            IServicoAssinaturaDigital assinatura,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var xml = Convert.FromBase64String(payload.XmlBase64);
                var resultado = await assinatura.AssinarXmlAsync(
                    xml,
                    new OpcoesAssinaturaXml(payload.Destino, payload.ReferenceUri ?? string.Empty),
                    cancellationToken);

                return Results.Ok(new
                {
                    xmlAssinadoBase64 = Convert.ToBase64String(resultado.XmlAssinado),
                    thumbprint = resultado.Thumbprint,
                    hashArtefato = resultado.HashArtefatoSha256,
                });
            }
            catch (CertificadoInvalidoException excecao)
            {
                return Results.BadRequest(new { erro = excecao.Message });
            }
            catch (FormatException)
            {
                return Results.BadRequest(new { erro = "Conteudo XML deve estar em Base64 valido." });
            }
        });
    }

    private sealed record AssinarXmlPayload(string XmlBase64, DestinoAssinatura Destino, string? ReferenceUri = null);
}
