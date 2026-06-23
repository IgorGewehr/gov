using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Ponto.Coleta;

/// <summary>
/// Driver UNIVERSAL de importacao de arquivo AFD (ACL): recebe um AFD ja exportado (upload pela UI ou
/// pendrive da porta fiscal) e o entrega ao pipeline de ingestao SEM rede nem SDK. Funciona para
/// QUALQUER REP-C/A/P, inclusive marcas sem SDK. A coleta "online incremental" nao se aplica aqui (o
/// arquivo ja chega pronto), entao <see cref="ColetarAsync"/> nao e o caminho deste driver — a entrada
/// e o <see cref="ImportarAfdCommand"/>. Mantido como driver para uniformidade da fabrica.
/// </summary>
public sealed class ColetorArquivoAfd : IColetorRep
{
    /// <inheritdoc />
    public MarcaRep Marca => MarcaRep.ArquivoAfd;

    /// <inheritdoc />
    public ModoColeta Modos => ModoColeta.Arquivo;

    /// <inheritdoc />
    public Task<LoteAfdColetado> ColetarAsync(RepConexao conexao, long ultimoNsrConhecido, CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "ColetorArquivoAfd nao faz coleta online: importe o arquivo via ImportarAfdCommand (upload/pendrive).");
}
