using System.Text;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.SicapPessoal;

/// <summary>Fecha (gera) a remessa SICAP-AP/SIAPES: transita a remessa para Gerada e devolve o artefato.</summary>
/// <param name="RemessaId">Remessa (aberta) a gerar.</param>
public sealed record GerarRemessaSicapCommand(Guid RemessaId) : ICommand<ArtefatoRemessaSicap>;

/// <summary>Artefato do arquivo de importacao SIAPES (leiaute estadual 57 posicoes).</summary>
/// <param name="NomeArquivo">Nome sugerido do arquivo.</param>
/// <param name="Conteudo">Bytes do arquivo (ISO-8859-1, padrao dos arquivos legados do TCE-RS).</param>
public sealed record ArtefatoRemessaSicap(string NomeArquivo, byte[] Conteudo);

/// <summary>Handler da geracao de remessa.</summary>
public sealed class GerarRemessaSicapHandler(
    IRemessaSicapPessoalRepository remessas,
    IUnitOfWork unitOfWork,
    TimeProvider tempo)
    : ICommandHandler<GerarRemessaSicapCommand, ArtefatoRemessaSicap>
{
    /// <inheritdoc />
    public async Task<ArtefatoRemessaSicap> Handle(GerarRemessaSicapCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas.ObterPorIdAsync(new RemessaSicapPessoalId(request.RemessaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa de pessoal nao encontrada.");

        remessa.Gerar(tempo.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var conteudo = LeiauteSiapes.Gerar(remessa);
        // Arquivos legados do TCE-RS sao ISO-8859-1 (Latin-1) — preserva acentos/cedilha do NOME.
        var bytes = Encoding.Latin1.GetBytes(conteudo);
        var nome = $"SIAPES_{remessa.CodigoOrgao:000000}_{remessa.SequencialLote:0000000000}.txt";
        return new ArtefatoRemessaSicap(nome, bytes);
    }
}

/// <summary>
/// Marca a remessa como transmitida ao TCE-RS, guardando o protocolo. // TODO(M10): transmissao real
/// ao SIAPESweb atras de ACL (Polly + circuit breaker + A1 do Key Vault); aqui apenas registra o
/// protocolo informado/retornado.
/// </summary>
/// <param name="RemessaId">Remessa (gerada) a transmitir.</param>
/// <param name="Protocolo">Protocolo de transmissao.</param>
public sealed record TransmitirRemessaSicapCommand(Guid RemessaId, string Protocolo) : ICommand;

/// <summary>Handler da transmissao (registro de protocolo).</summary>
public sealed class TransmitirRemessaSicapHandler(IRemessaSicapPessoalRepository remessas, IUnitOfWork unitOfWork)
    : ICommandHandler<TransmitirRemessaSicapCommand>
{
    /// <inheritdoc />
    public async Task Handle(TransmitirRemessaSicapCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var remessa = await remessas.ObterPorIdAsync(new RemessaSicapPessoalId(request.RemessaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa de pessoal nao encontrada.");
        remessa.MarcarTransmitida(request.Protocolo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
