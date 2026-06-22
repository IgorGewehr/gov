using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Artefato MSC gerado para download e upload MANUAL no portal SICONFI.</summary>
/// <param name="NomeArquivo">Nome do ZIP gerado.</param>
/// <param name="ContentType">Tipo MIME (application/zip).</param>
/// <param name="Conteudo">Bytes do ZIP.</param>
public sealed record MscDownload(string NomeArquivo, string ContentType, ReadOnlyMemory<byte> Conteudo);

/// <summary>
/// Gera a Matriz de Saldos Contábeis (MSC) zipada a partir de uma <see cref="DeclaracaoFiscal"/>
/// consolidada, pronta para upload MANUAL no portal SICONFI (homologação por e-CPF A3 é ato humano).
/// </summary>
/// <param name="DeclaracaoFiscalId">Declaração fiscal consolidada de origem.</param>
public sealed record GerarMscCommand(Guid DeclaracaoFiscalId) : ICommand<MscDownload>;

/// <summary>Regras de validação da geração da MSC.</summary>
public sealed class GerarMscValidator : AbstractValidator<GerarMscCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarMscValidator() => RuleFor(comando => comando.DeclaracaoFiscalId).NotEmpty();
}

/// <summary>Handler da geração do artefato MSC (SICONFI).</summary>
public sealed class GerarMscHandler(IDeclaracaoFiscalRepository declaracoes, IGeradorMsc gerador)
    : ICommandHandler<GerarMscCommand, MscDownload>
{
    private const string ContentTypeZip = "application/zip";

    /// <inheritdoc />
    public async Task<MscDownload> Handle(GerarMscCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var declaracao = await declaracoes
            .ObterPorIdAsync(new DeclaracaoFiscalId(request.DeclaracaoFiscalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Declaração fiscal não encontrada.");

        var artefato = await gerador.GerarAsync(declaracao, cancellationToken).ConfigureAwait(false);
        return new MscDownload(artefato.NomeArquivo, ContentTypeZip, artefato.Conteudo);
    }
}
