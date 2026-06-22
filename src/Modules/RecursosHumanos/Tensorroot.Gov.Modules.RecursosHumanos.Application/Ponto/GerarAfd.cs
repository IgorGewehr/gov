using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;

/// <summary>Artefato AFD gerado: nome sugerido, conteudo posicional e a assinatura CAdES destacada (.p7s).</summary>
/// <param name="NomeArquivo">Nome sugerido do arquivo (AFD).</param>
/// <param name="Conteudo">Bytes do AFD (ISO-8859-1).</param>
/// <param name="AssinaturaCades">Assinatura CAdES detached (.p7s) sobre o conteudo; vazia se nao assinada.</param>
public sealed record ArtefatoAfd(string NomeArquivo, byte[] Conteudo, byte[] AssinaturaCades);

/// <summary>
/// Gera o AFD (Arquivo Fonte de Dados — Portaria MTP 671/2021) de um periodo: monta o arquivo
/// posicional a partir das marcacoes imutaveis e o assina em CAdES destacado (reusa o assinador A1 do
/// Cofre). NAO altera o AFD. // TODO(validar-oficial): posicoes/larguras dos campos e perfil CAdES.
/// </summary>
/// <param name="Inicio">Data inicial (inclusiva).</param>
/// <param name="Fim">Data final (inclusiva).</param>
/// <param name="Assinar">Quando verdadeiro, anexa a assinatura CAdES detached.</param>
public sealed record GerarAfdQuery(DateOnly Inicio, DateOnly Fim, bool Assinar = true) : IQuery<ArtefatoAfd>;

/// <summary>Regras de validacao da geracao do AFD.</summary>
public sealed class GerarAfdValidator : AbstractValidator<GerarAfdQuery>
{
    /// <summary>Define as regras.</summary>
    public GerarAfdValidator()
        => RuleFor(q => q.Fim).GreaterThanOrEqualTo(q => q.Inicio)
            .WithMessage("Fim do periodo nao pode ser anterior ao inicio.");
}

/// <summary>Handler da geracao do AFD.</summary>
public sealed class GerarAfdHandler(
    IMarcacaoPontoRepository marcacoes,
    IParametrosPontoProvider parametros,
    IAssinaturaEmEscopoDedicado assinatura)
    : IQueryHandler<GerarAfdQuery, ArtefatoAfd>
{
    /// <inheritdoc />
    public async Task<ArtefatoAfd> Handle(GerarAfdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);
        // Fail-closed (CLAUDE.md §16): sem CNPJ do ente nao ha cabecalho valido — recusa a geracao.
        if (string.IsNullOrWhiteSpace(config.CnpjEnte))
        {
            throw new InvalidOperationException("CNPJ do ente nao configurado (RecursosHumanos:Ponto:CnpjEnte).");
        }

        var lista = await marcacoes.ListarPorPeriodoAsync(request.Inicio, request.Fim, cancellationToken).ConfigureAwait(false);
        var cabecalho = new CabecalhoAfd(
            Cnpj.Create(config.CnpjEnte),
            config.RazaoSocialEnte ?? string.Empty,
            request.Inicio,
            request.Fim,
            DateTimeOffset.Now);

        var linhas = lista
            .Select(m => new LinhaMarcacaoAfd(m.Nsr, m.Cpf, m.DataHora, m.Origem))
            .ToList();

        var conteudo = GeradorAfd.Gerar(cabecalho, linhas);

        var assinaturaCades = Array.Empty<byte>();
        if (request.Assinar)
        {
            // CAdES detached (.p7s) sobre o AFD — reusa o A1 do Cofre. // TODO(validar-oficial: perfil 671).
            assinaturaCades = await assinatura
                .AssinarCmsAsync(conteudo, new OpcoesAssinaturaCms(DestinoAssinatura.Ponto, Detached: true), cancellationToken)
                .ConfigureAwait(false);
        }

        var nome = $"AFD_{request.Inicio:yyyyMMdd}_{request.Fim:yyyyMMdd}.txt";
        return new ArtefatoAfd(nome, conteudo, assinaturaCades);
    }
}
