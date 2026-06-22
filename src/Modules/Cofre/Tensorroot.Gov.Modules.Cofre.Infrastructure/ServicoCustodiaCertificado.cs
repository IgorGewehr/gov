using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Cofre.Application.Abstractions;
using Tensorroot.Gov.Modules.Cofre.Domain;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure;

/// <summary>
/// Custodia (cadastro/rotacao) do certificado A1 no cofre — gated por <c>admin.certificado.gerenciar</c>
/// no endpoint. Recebe o .pfx+senha SO EM MEMORIA, cifra (envelope) e persiste; na rotacao, marca o
/// anterior como <see cref="CertificadoStatus.Substituido"/> na MESMA transacao (A1-DESIGN §3.1, §2:
/// um unico ativo por tenant). NUNCA grava .pfx/senha em claro nem retorna a chave.
/// </summary>
internal sealed class ServicoCustodiaCertificado(
    ICofreCertificadoRepository repositorio,
    CofreCripto cripto,
    ITenantContext tenantContext,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Cadastra (ou rotaciona) o certificado A1 ativo do tenant a partir do .pfx+senha em memoria.
    /// </summary>
    /// <param name="cnpjTitularEsperado">CNPJ do tenant (sem mascara) para conferencia (A1-DESIGN §3.1).</param>
    /// <param name="pfxBytes">Bytes do .pfx (so em memoria; nunca em disco).</param>
    /// <param name="senha">Senha do .pfx (so em memoria).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do novo certificado ativo.</returns>
    /// <exception cref="CertificadoInvalidoException">Validade/serie/cadeia/CNPJ invalidos.</exception>
    public async Task<Guid> CadastrarOuRotacionarAsync(
        string cnpjTitularEsperado,
        byte[] pfxBytes,
        string senha,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjTitularEsperado);
        ArgumentNullException.ThrowIfNull(pfxBytes);

        if (!tenantContext.HasTenant)
        {
            throw new InvalidOperationException("Tenant nao resolvido; impossivel custodiar certificado.");
        }

        var tenantId = tenantContext.TenantId;
        var agora = timeProvider.GetUtcNow().UtcDateTime;

        var cifragem = await cripto
            .CifrarParaCadastroAsync(tenantId, pfxBytes, senha, agora, cancellationToken)
            .ConfigureAwait(false);

        var novo = CertificadoA1Cofre.Cadastrar(
            tenantId,
            cifragem.Titular,
            cnpjTitularEsperado,
            cifragem.Thumbprint,
            cifragem.NotBeforeUtc,
            cifragem.NotAfterUtc,
            cifragem.Pfx,
            cifragem.Senha,
            cifragem.DekWrapped,
            cifragem.KekKeyId,
            agora);

        // Rotacao: se ja existe um ativo, marca-o Substituido e encadeia (um unico ativo por tenant).
        var anterior = await repositorio.ObterAtivoAsync(cancellationToken).ConfigureAwait(false);
        if (anterior is not null)
        {
            anterior.MarcarSubstituido(novo.Id);
            novo.EncadearAposRotacao(anterior.Id);
        }

        repositorio.Adicionar(novo);
        await repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

        return novo.Id.Value;
    }
}
