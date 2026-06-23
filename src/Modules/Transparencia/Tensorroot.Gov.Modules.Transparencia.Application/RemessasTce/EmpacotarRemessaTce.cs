using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>
/// Empacota uma remessa <c>Validada</c> no ZIP nomeado e a marca como <c>ProntaParaTransmissao</c>.
/// </summary>
/// <remarks>
/// NÃO transmite ao TCE-RS (não há API de envio): apenas produz o artefato para download e transmissão
/// MANUAL no PAD/e-Protocolo. O resultado retorna o nome do ZIP.
/// </remarks>
/// <param name="RemessaTceId">Identificador da remessa a empacotar.</param>
public sealed record EmpacotarRemessaTceCommand(Guid RemessaTceId) : ICommand<string>;

/// <summary>Regras de validação do comando de empacotamento.</summary>
public sealed class EmpacotarRemessaTceValidator : AbstractValidator<EmpacotarRemessaTceCommand>
{
    /// <summary>Define as regras.</summary>
    public EmpacotarRemessaTceValidator() => RuleFor(comando => comando.RemessaTceId).NotEmpty();
}

/// <summary>Handler do empacotamento da remessa (Validada → ProntaParaTransmissao).</summary>
public sealed class EmpacotarRemessaTceHandler(
    IRemessaTceRepository remessas,
    ILeiauteCatalogo leiauteCatalogo,
    IEmpacotadorRemessaSiapc empacotador,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<EmpacotarRemessaTceCommand, string>
{
    /// <inheritdoc />
    public async Task<string> Handle(EmpacotarRemessaTceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas
            .ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa não encontrada.");

        var identificacao = await leiauteCatalogo
            .ObterIdentificacaoEnteAsync(remessa.Periodo, cancellationToken)
            .ConfigureAwait(false);
        var leiauteSiapc = await leiauteCatalogo
            .ResolverAsync(remessa.Leiaute, cancellationToken)
            .ConfigureAwait(false);

        // Reconstrói os arquivos montados a partir do conteúdo persistido (bytes já posicionais ISO-8859-1).
        var arquivos = remessa.Arquivos
            .Select(arquivo => new ArquivoMontado(
                leiauteSiapc.RegistroPorArquivo(arquivo.NomeArquivo)
                    ?? throw new InvalidOperationException(
                        $"Arquivo '{arquivo.NomeArquivo}' não pertence ao leiaute resolvido."),
                [],
                arquivo.Conteudo))
            .ToList();

        var nomeZip = NomeArquivoRemessaSiapc.Compor(
            identificacao.Cnpj,
            identificacao.DataInicioPeriodo,
            identificacao.DataFimPeriodo,
            remessa.DataGeracao,
            leiauteSiapc.TipoSetorGoverno,
            identificacao.CodigoRemessa);

        var pacote = await empacotador
            .EmpacotarAsync(nomeZip, arquivos, cancellationToken)
            .ConfigureAwait(false);

        // O hash do agregado foi calculado sobre o conteúdo CONSOLIDADO dos .TXT (não sobre o ZIP);
        // confere a integridade desse conteúdo antes de marcar pronta para transmissão. A consolidação
        // segue a ORDEM DO LEIAUTE (leiaute.Registros) — a MESMA usada na geração (MontarArquivos itera
        // leiaute.Registros). NÃO se pode confiar na ordem da coleção-filha recarregada pelo EF (sem
        // OrderBy ela não é determinística), o que, em remessas multi-arquivo (folha: TCE_4810/4820/4960),
        // invertia os bytes e fazia a conferência de hash falhar indevidamente.
        var conteudoConsolidado = ConsolidarConteudo(remessa, leiauteSiapc);
        remessa.MarcarProntaParaTransmissao(pacote.NomeZip, conteudoConsolidado.Span);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _ = timeProvider;
        return pacote.NomeZip;
    }

    private static ReadOnlyMemory<byte> ConsolidarConteudo(RemessaTce remessa, LeiauteSiapc leiaute)
    {
        // Ordena os arquivos persistidos pela posição do seu registro no leiaute (ordem canônica da
        // geração); arquivos fora do leiaute (não deveria ocorrer) vão ao fim, preservando estabilidade.
        var ordemPorArquivo = leiaute.Registros
            .Select((registro, indice) => (registro.NomeArquivo, indice))
            .ToDictionary(item => item.NomeArquivo, item => item.indice, StringComparer.OrdinalIgnoreCase);

        var ordenados = remessa.Arquivos
            .OrderBy(arquivo => ordemPorArquivo.TryGetValue(arquivo.NomeArquivo, out var indice) ? indice : int.MaxValue)
            .ToList();

        var tamanho = ordenados.Sum(arquivo => arquivo.Conteudo.Length);
        var total = new byte[tamanho];
        var offset = 0;
        foreach (var arquivo in ordenados)
        {
            arquivo.Conteudo.Span.CopyTo(total.AsSpan(offset));
            offset += arquivo.Conteudo.Length;
        }

        return total;
    }
}
