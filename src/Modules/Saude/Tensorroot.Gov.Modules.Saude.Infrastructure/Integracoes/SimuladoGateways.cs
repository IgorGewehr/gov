using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using RegulacaoEstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Regulacao.EstabelecimentoId;
using Procedimento = Tensorroot.Gov.Modules.Saude.Domain.Regulacao.Procedimento;
using Cota = Tensorroot.Gov.Modules.Saude.Domain.Regulacao.Cota;
using SolicitacaoRegulacaoId = Tensorroot.Gov.Modules.Saude.Domain.Regulacao.SolicitacaoRegulacaoId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Integracoes;

/// <summary>
/// Implementacao simulada (dev/testes) do ACL do CADSUS (PIX/PDQ). Em producao, a implementacao
/// concreta usa HTTP resiliente (Polly) atras de Anti-Corruption Layer. Confirma CNS formalmente validos.
/// </summary>
public sealed class SimuladoCadsusGateway : ICadsusGateway
{
    /// <inheritdoc />
    public Task<bool> ValidarCnsAsync(Cns cns, CancellationToken cancellationToken)
        => Task.FromResult(Cns.EhValido(cns.Valor));
}

/// <summary>
/// Implementacao simulada (dev/testes) do master data CNES (estabelecimento/profissional ativos e CRM).
/// Em producao, consulta sincrona/cacheada atras de ACL com timeout/retry/circuit breaker (Polly).
/// </summary>
public sealed class SimuladoEstabelecimentoRepository : IEstabelecimentoRepository
{
    /// <inheritdoc />
    public Task<bool> EstabelecimentoAtivoAsync(
        EstabelecimentoId estabelecimentoId,
        Competencia competencia,
        CancellationToken cancellationToken)
        => Task.FromResult(estabelecimentoId.Value != Guid.Empty);

    /// <inheritdoc />
    public Task<bool> ProfissionalAtivoAsync(
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        Competencia competencia,
        CancellationToken cancellationToken)
        => Task.FromResult(profissionalId.Value != Guid.Empty && estabelecimentoId.Value != Guid.Empty);

    /// <inheritdoc />
    public Task<bool> ProfissionalComCrmAtivoAsync(ProfissionalId profissionalId, CancellationToken cancellationToken)
        => Task.FromResult(profissionalId.Value != Guid.Empty);
}

/// <summary>
/// Implementacao simulada (dev/testes) do ACL de saida da RNDS (Bundle FHIR R4 via mTLS + ICP-Brasil).
/// Em producao, envio idempotente por identificador do Bundle/atendimento, resiliente (Polly).
/// </summary>
public sealed class SimuladoRndsGateway : IRndsGateway
{
    /// <inheritdoc />
    public Task<string> EnviarBundleAsync(AtendimentoId atendimentoId, CancellationToken cancellationToken)
        => Task.FromResult($"RNDS-SIM-{atendimentoId.Value:N}");
}

/// <summary>
/// Implementacao simulada (dev/testes) do ACL de saida do e-SUS APS / SISAB (producao CDS).
/// Em producao, envio idempotente por (AtendimentoId, Competencia), resiliente (Polly).
/// </summary>
public sealed class SimuladoSisabGateway : ISisabGateway
{
    /// <inheritdoc />
    public Task EnviarProducaoAsync(AtendimentoId atendimentoId, Competencia competencia, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Implementacao simulada (dev/testes) do ACL de saida do SISREG (reserva/liberacao de vaga).
/// Em producao, operacoes idempotentes por <see cref="SolicitacaoRegulacaoId"/>, resilientes (Polly).
/// </summary>
public sealed class SimuladoSisregGateway : ISisregGateway
{
    /// <inheritdoc />
    public Task<string> ReservarVagaAsync(
        SolicitacaoRegulacaoId solicitacaoId,
        Procedimento procedimento,
        CancellationToken cancellationToken)
        => Task.FromResult($"SISREG-SIM-{solicitacaoId.Value:N}");

    /// <inheritdoc />
    public Task LiberarReservaAsync(
        SolicitacaoRegulacaoId solicitacaoId,
        string protocoloSisreg,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Implementacao simulada (dev/testes) do servico de assinatura ICP-Brasil (NGS2). Em producao,
/// usa certificados A1/A3 por tenant no Azure Key Vault e valida o hash/cadeia ICP-Brasil.
/// </summary>
public sealed class SimuladoAssinaturaIcpBrasilService : IAssinaturaIcpBrasilService
{
    /// <inheritdoc />
    public Task<AssinaturaDigital> AssinarAsync(string certificadoIcpBrasil, string hash, CancellationToken cancellationToken)
        => Task.FromResult(AssinaturaDigital.De(certificadoIcpBrasil, hash, DateTimeOffset.UtcNow, NivelGarantia.NGS2));
}

/// <summary>
/// Implementacao simulada (dev/testes) da porta de leitura de cota/limite de vagas. Em producao,
/// a fonte de verdade pode ser tabela propria; o agregado guarda apenas o snapshot da solicitacao.
/// </summary>
public sealed class SimuladoCotaRepository : ICotaRepository
{
    private const int VagasPadrao = 10;

    /// <inheritdoc />
    public Task<Cota> ObterCotaAsync(
        string codigoSigtap,
        RegulacaoEstabelecimentoId estabelecimentoSolicitanteId,
        CancellationToken cancellationToken)
        => Task.FromResult(Cota.Criar(VagasPadrao, VagasPadrao));
}
