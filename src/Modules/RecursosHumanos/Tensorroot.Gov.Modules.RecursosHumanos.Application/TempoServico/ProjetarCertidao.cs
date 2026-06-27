using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.TempoServico;

/// <summary>
/// Projecoes read-side e helpers da certidao de tempo de servico/contribuicao (sem dependencia de
/// Infrastructure). Inclui o gerador do DIGEST de autenticacao (puro, deterministico) usado para selar o
/// codigo do documento na borda — o dominio recebe o codigo pronto.
/// </summary>
public static class ProjetarCertidao
{
    /// <summary>
    /// Calcula o DIGEST de autenticacao de uma certidao: SHA-256 sobre os campos ESTAVEIS do documento
    /// (tenant, certidao, servidor, numeracao, finalidade e total de dias), em hexadecimal maiusculo. E'
    /// deterministico (mesma certidao -> mesmo codigo) e opaco (nao revela dado sensivel). Vive aqui, na
    /// borda, para manter o dominio livre de IO/crypto (CLAUDE.md S7).
    /// </summary>
    /// <param name="tenantId">Tenant dono da certidao.</param>
    /// <param name="certidaoId">Identificador da certidao.</param>
    /// <param name="servidorId">Servidor certificado.</param>
    /// <param name="numero">Numeracao oficial.</param>
    /// <param name="finalidade">Finalidade da certidao.</param>
    /// <param name="totalDias">Total de dias equivalentes certificados.</param>
    /// <returns>Digest hexadecimal maiusculo (64 caracteres) para construir o <see cref="CodigoAutenticacao"/>.</returns>
    public static string CalcularDigestAutenticacao(
        Guid tenantId,
        CertidaoTempoServicoId certidaoId,
        ServidorId servidorId,
        NumeroCertidao numero,
        FinalidadeCertidao finalidade,
        int totalDias)
    {
        ArgumentNullException.ThrowIfNull(numero);
        var material = string.Create(CultureInfo.InvariantCulture,
            $"{tenantId:N}|{certidaoId.Value:N}|{servidorId.Value:N}|{numero.Formatado}|{(int)finalidade}|{totalDias}");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Projeta a certidao para o resumo (lista/ficha).</summary>
    /// <param name="certidao">Certidao a projetar.</param>
    /// <returns>Resumo da certidao.</returns>
    public static CertidaoResumoDto ParaResumo(CertidaoTempoServico certidao)
    {
        ArgumentNullException.ThrowIfNull(certidao);
        return new CertidaoResumoDto(
            certidao.Id.Value,
            certidao.Numero.Formatado,
            certidao.Finalidade,
            certidao.DataEmissao,
            certidao.Situacao,
            certidao.TotalDias,
            certidao.TempoTotal.Formatado,
            certidao.CodigoAutenticacao.Valor);
    }

    /// <summary>Projeta a certidao para o detalhe completo (documento), com os dados do servidor.</summary>
    /// <param name="certidao">Certidao a projetar.</param>
    /// <param name="servidor">Servidor certificado (para nome/matricula/CPF mascarado).</param>
    /// <returns>Detalhe completo da certidao.</returns>
    public static CertidaoDetalheDto ParaDetalhe(CertidaoTempoServico certidao, Servidor servidor)
    {
        ArgumentNullException.ThrowIfNull(certidao);
        ArgumentNullException.ThrowIfNull(servidor);

        var tempo = certidao.TempoTotal;
        var periodos = certidao.Periodos.Select(ParaPeriodoDto).ToList();
        return new CertidaoDetalheDto(
            certidao.Id.Value,
            certidao.ServidorId.Value,
            servidor.DadosPessoais.Nome,
            servidor.Matricula.Valor,
            MascararCpf(servidor.Cpf),
            certidao.Numero.Formatado,
            certidao.Finalidade,
            certidao.FinalidadeDescrita,
            certidao.DataEmissao,
            certidao.OrgaoEmissor,
            certidao.Situacao,
            certidao.MotivoAnulacao,
            certidao.Observacao,
            certidao.CodigoAutenticacao.Valor,
            certidao.TotalDias,
            certidao.TotalDiasLiquidos,
            tempo.Anos,
            tempo.Meses,
            tempo.Dias,
            tempo.Formatado,
            periodos);
    }

    /// <summary>Projeta um periodo computado para DTO (linha do documento).</summary>
    /// <param name="periodo">Periodo a projetar.</param>
    /// <returns>DTO do periodo.</returns>
    public static PeriodoTempoDto ParaPeriodoDto(PeriodoTempo periodo)
    {
        ArgumentNullException.ThrowIfNull(periodo);
        return new PeriodoTempoDto(
            periodo.Inicio,
            periodo.Fim,
            periodo.Natureza,
            periodo.RegimeOrigem,
            periodo.Origem,
            periodo.DiasBrutos,
            periodo.DiasNaoComputaveis,
            periodo.DiasLiquidos,
            periodo.Fator,
            periodo.DiasEquivalentes,
            periodo.Observacao);
    }

    /// <summary>Mascara o CPF preservando apenas os tres digitos centrais (LGPD): <c>***.NNN.***-**</c>.</summary>
    /// <param name="cpf">CPF a mascarar.</param>
    /// <returns>CPF mascarado.</returns>
    public static string MascararCpf(Cpf cpf)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        var digitos = cpf.Digitos;
        return $"***.{digitos[3..6]}.***-**";
    }
}
