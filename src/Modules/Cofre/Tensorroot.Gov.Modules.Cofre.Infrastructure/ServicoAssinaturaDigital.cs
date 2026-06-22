using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.Modules.Cofre.Application.Abstractions;
using Tensorroot.Gov.Modules.Cofre.Domain;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure;

/// <summary>
/// Implementacao transversal de <see cref="IServicoAssinaturaDigital"/> (A1-DESIGN §5). Carrega o A1
/// ATIVO do tenant, decifra SO-EM-MEMORIA (<see cref="CofreCripto"/>), assina conforme o destino e
/// grava a trilha de auditoria de CADA uso (sucesso E falha — A1-DESIGN §6), sem material sensivel.
/// </summary>
internal sealed class ServicoAssinaturaDigital(
    ICofreCertificadoRepository repositorio,
    CofreCripto cripto,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<ServicoAssinaturaDigital> logger) : IServicoAssinaturaDigital
{
    /// <inheritdoc />
    public async Task<ResultadoAssinatura> AssinarXmlAsync(
        ReadOnlyMemory<byte> xmlUtf8,
        OpcoesAssinaturaXml opcoes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        var hash = HashSha256Hex(xmlUtf8.Span);
        return await ExecutarAuditadoAsync(
            opcoes.Destino,
            hash,
            async certificado =>
            {
                var xmlAssinado = await cripto
                    .UsarCertificadoAsync(certificado, cert => AssinadorXmlDsig.Assinar(xmlUtf8, opcoes, cert), cancellationToken)
                    .ConfigureAwait(false);
                return new ResultadoAssinatura(xmlAssinado, certificado.Thumbprint, hash);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<byte[]> AssinarCmsAsync(
        ReadOnlyMemory<byte> conteudo,
        OpcoesAssinaturaCms opcoes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        var hash = HashSha256Hex(conteudo.Span);
        var resultado = await ExecutarAuditadoAsync(
            opcoes.Destino,
            hash,
            async certificado =>
            {
                var cms = await cripto
                    .UsarCertificadoAsync(certificado, cert => AssinadorCms.Assinar(conteudo, cert, opcoes.Detached), cancellationToken)
                    .ConfigureAwait(false);
                return new ResultadoAssinatura(cms, certificado.Thumbprint, hash);
            },
            cancellationToken).ConfigureAwait(false);
        return resultado.XmlAssinado;
    }

    /// <inheritdoc />
    public async Task<CertificadoInfo> ObterInfoCertificadoAsync(CancellationToken cancellationToken)
    {
        var certificado = await repositorio.ObterAtivoAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new CertificadoInvalidoException("Nenhum certificado A1 ativo no cofre do tenant.");

        return new CertificadoInfo(
            certificado.Titular,
            certificado.CnpjTitular,
            certificado.Thumbprint,
            certificado.NotBeforeUtc,
            certificado.NotAfterUtc,
            certificado.Serie,
            certificado.Status.ToString());
    }

    /// <summary>
    /// Orquestra a assinatura com auditoria de uso: resolve o cert ativo, executa a operacao e grava
    /// SEMPRE um registro (sucesso ou falha). A excecao e relancada apos auditar — nunca silenciosa.
    /// </summary>
    private async Task<ResultadoAssinatura> ExecutarAuditadoAsync(
        DestinoAssinatura destino,
        string hashArtefato,
        Func<CertificadoA1Cofre, Task<ResultadoAssinatura>> operacao,
        CancellationToken cancellationToken)
    {
        var agora = timeProvider.GetUtcNow().UtcDateTime;
        var tenantId = tenantContext.HasTenant ? tenantContext.TenantId : Guid.Empty;
        var correlationId = Activity.Current?.Id;
        var destinoTexto = destino.ToString();
        CertificadoA1Cofre? certificado = null;

        try
        {
            certificado = await repositorio.ObterAtivoAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new CertificadoInvalidoException("Nenhum certificado A1 ativo no cofre do tenant.");

            var resultado = await operacao(certificado).ConfigureAwait(false);

            repositorio.RegistrarUso(AssinaturaAuditLog.Sucedido(
                tenantId, currentUser.UserId, agora, certificado.Thumbprint, certificado.Titular,
                destinoTexto, hashArtefato, correlationId, currentUser.IpAddress));
            await repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

            return resultado;
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException)
        {
            // Toda TENTATIVA e auditavel (A1-DESIGN §6). Motivo sem material sensivel.
            repositorio.RegistrarUso(AssinaturaAuditLog.Falho(
                tenantId, currentUser.UserId, agora, certificado?.Thumbprint, certificado?.Titular,
                destinoTexto, hashArtefato, excecao.GetType().Name, correlationId, currentUser.IpAddress));
            try
            {
                await repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception persistencia) when (persistencia is not OperationCanceledException)
            {
                logger.LogError(persistencia, "Falha ao persistir auditoria de assinatura para o destino {Destino}.", destinoTexto);
            }

            throw;
        }
    }

    private static string HashSha256Hex(ReadOnlySpan<byte> dados)
    {
        Span<byte> destino = stackalloc byte[32];
        SHA256.HashData(dados, destino);
        return Convert.ToHexString(destino).ToLower(CultureInfo.InvariantCulture);
    }
}
