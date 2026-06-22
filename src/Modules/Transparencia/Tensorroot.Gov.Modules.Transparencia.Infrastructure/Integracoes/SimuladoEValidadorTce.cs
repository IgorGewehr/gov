using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// e-Validador simulado (dev/demonstracao): produz um RDI sem erro para pacotes com ao menos um
/// arquivo e leiaute versionado, permitindo exercitar o ciclo Gerada -&gt; Validada sem o validador
/// real do TCE-RS. A implementacao real executa o e-Validador local e mapeia explicitamente as
/// ocorrencias (Anti-Corruption Layer).
/// </summary>
public sealed class SimuladoEValidadorTce(TimeProvider timeProvider) : IEValidadorTce
{
    /// <inheritdoc />
    public Task<ResultadoValidacao> ValidarAsync(RemessaTce remessa, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(remessa);

        var ocorrencias = remessa.Arquivos.Count == 0
            ? new[]
            {
                OcorrenciaValidacao.Criar("pacote", 0, SeveridadeOcorrencia.Erro, "Pacote sem arquivos componentes."),
            }
            : [];

        var rdi = ResultadoValidacao.Criar(
            remessa.Leiaute.Versao,
            timeProvider.GetUtcNow(),
            ocorrencias);

        return Task.FromResult(rdi);
    }
}
