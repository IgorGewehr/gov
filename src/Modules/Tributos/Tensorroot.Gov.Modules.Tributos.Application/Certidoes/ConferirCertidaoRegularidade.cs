using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Tributos.Application.Certidoes;

/// <summary>Resultado da conferência de autenticidade de uma certidão de regularidade.</summary>
/// <param name="Autentica">Indica se número + código conferem com uma certidão emitida.</param>
/// <param name="Vigente">Indica se a certidão está dentro do prazo de validade na data de conferência.</param>
/// <param name="Tipo">Tipo da certidão ("Negativa"/"PositivaComEfeitoNegativa"/"Positiva"), se autêntica.</param>
/// <param name="AtestaRegularidade">Se a certidão produz efeito de regularidade (CND/CPEN), se autêntica.</param>
/// <param name="Numero">Número da certidão conferida, se autêntica.</param>
/// <param name="NomeContribuinte">Nome/razão social, se autêntica.</param>
/// <param name="DataEmissao">Data de emissão, se autêntica.</param>
/// <param name="DataValidade">Data-limite de validade, se autêntica.</param>
public sealed record ConferenciaCertidaoDto(
    bool Autentica,
    bool Vigente,
    string? Tipo,
    bool AtestaRegularidade,
    string? Numero,
    string? NomeContribuinte,
    DateOnly? DataEmissao,
    DateOnly? DataValidade);

/// <summary>
/// Confere a AUTENTICIDADE de uma certidão de regularidade fiscal apresentada por um terceiro (ex.: em
/// licitação/cartório): pelo NÚMERO + CÓDIGO de autenticação. Não vaza dado de contribuinte se a
/// conferência falhar (número inexistente ou código incorreto → não-autêntica, sem detalhes).
/// </summary>
/// <param name="Numero">Número da certidão apresentada.</param>
/// <param name="CodigoAutenticacao">Código de autenticação apresentado.</param>
public sealed record ConferirCertidaoRegularidadeQuery(string Numero, string CodigoAutenticacao)
    : IQuery<ConferenciaCertidaoDto>;

/// <summary>Handler da conferência de autenticidade da certidão.</summary>
public sealed class ConferirCertidaoRegularidadeHandler(
    ICertidaoRegularidadeFiscalRepository certidoes,
    IDataHojeTenant dataHoje)
    : IQueryHandler<ConferirCertidaoRegularidadeQuery, ConferenciaCertidaoDto>
{
    private static readonly ConferenciaCertidaoDto NaoAutentica =
        new(false, false, null, false, null, null, null, null);

    /// <inheritdoc />
    public async Task<ConferenciaCertidaoDto> Handle(ConferirCertidaoRegularidadeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Numero) || string.IsNullOrWhiteSpace(request.CodigoAutenticacao))
        {
            return NaoAutentica;
        }

        var certidao = await certidoes.ObterPorNumeroAsync(request.Numero.Trim(), cancellationToken).ConfigureAwait(false);

        // Conferência em tempo fixo no domínio (anti-timing); número inexistente é indistinguível de
        // código incorreto — não vaza a existência da certidão de terceiros.
        if (certidao is null || !certidao.ConferirAutenticidade(request.CodigoAutenticacao))
        {
            return NaoAutentica;
        }

        return new ConferenciaCertidaoDto(
            true,
            certidao.EstaVigente(dataHoje.Hoje()),
            certidao.Tipo.ToString(),
            certidao.AtestaRegularidade,
            certidao.Numero,
            certidao.NomeContribuinte,
            certidao.DataEmissao,
            certidao.DataValidade);
    }
}
