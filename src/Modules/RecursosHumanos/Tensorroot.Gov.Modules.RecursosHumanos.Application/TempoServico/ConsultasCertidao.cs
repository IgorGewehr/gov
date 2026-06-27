using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.TempoServico;

/// <summary>Lista as certidoes de tempo de um servidor (ficha — mais recentes primeiro); read-only.</summary>
/// <param name="ServidorId">Servidor.</param>
public sealed record ListarCertidoesDoServidorQuery(Guid ServidorId) : IQuery<IReadOnlyList<CertidaoResumoDto>>;

/// <summary>Handler da listagem de certidoes por servidor.</summary>
public sealed class ListarCertidoesDoServidorHandler(ICertidaoTempoServicoRepository certidoes)
    : IQueryHandler<ListarCertidoesDoServidorQuery, IReadOnlyList<CertidaoResumoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CertidaoResumoDto>> Handle(ListarCertidoesDoServidorQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await certidoes.ListarPorServidorAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false);
        return [.. itens.Select(ProjetarCertidao.ParaResumo)];
    }
}

/// <summary>Obtem o DETALHE completo de uma certidao (documento, com os periodos); read-only.</summary>
/// <param name="CertidaoId">Identificador da certidao.</param>
public sealed record ObterCertidaoQuery(Guid CertidaoId) : IQuery<CertidaoDetalheDto>;

/// <summary>Handler do detalhe de certidao.</summary>
public sealed class ObterCertidaoHandler(
    ICertidaoTempoServicoRepository certidoes,
    IServidorRepository servidores)
    : IQueryHandler<ObterCertidaoQuery, CertidaoDetalheDto>
{
    /// <inheritdoc />
    public async Task<CertidaoDetalheDto> Handle(ObterCertidaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var certidao = await certidoes.ObterPorIdAsync(new CertidaoTempoServicoId(request.CertidaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Certidao nao encontrada.");
        var servidor = await servidores.ObterPorIdAsync(certidao.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor da certidao nao encontrado.");
        return ProjetarCertidao.ParaDetalhe(certidao, servidor);
    }
}

/// <summary>
/// VALIDA a autenticidade de uma certidao pelo codigo de autenticacao (servico publico do balcao). Devolve o
/// minimo para conferencia (sem dado sensivel) e nunca confirma documento anulado.
/// </summary>
/// <param name="Codigo">Codigo de autenticacao (hex, 16 caracteres).</param>
public sealed record ValidarCertidaoQuery(string Codigo) : IQuery<ValidacaoCertidaoDto>;

/// <summary>Handler da validacao publica de certidao.</summary>
public sealed class ValidarCertidaoHandler(
    ICertidaoTempoServicoRepository certidoes,
    IServidorRepository servidores)
    : IQueryHandler<ValidarCertidaoQuery, ValidacaoCertidaoDto>
{
    /// <inheritdoc />
    public async Task<ValidacaoCertidaoDto> Handle(ValidarCertidaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var codigo = (request.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        var certidao = string.IsNullOrEmpty(codigo)
            ? null
            : await certidoes.ObterVigentePorCodigoAsync(codigo, cancellationToken).ConfigureAwait(false);

        if (certidao is null)
        {
            return new ValidacaoCertidaoDto(false, null, null, null, null, null);
        }

        var servidor = await servidores.ObterPorIdAsync(certidao.ServidorId, cancellationToken).ConfigureAwait(false);
        return new ValidacaoCertidaoDto(
            true,
            certidao.Numero.Formatado,
            servidor?.DadosPessoais.Nome,
            certidao.Finalidade,
            certidao.DataEmissao,
            certidao.TempoTotal.Formatado);
    }
}
