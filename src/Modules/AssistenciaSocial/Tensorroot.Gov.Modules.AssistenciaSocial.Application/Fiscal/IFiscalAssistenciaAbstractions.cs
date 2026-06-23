using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;

/// <summary>Repositorio do <see cref="FundoMunicipalAssistencia"/> (FMAS, raiz de agregado).</summary>
public interface IFundoMunicipalAssistenciaRepository
{
    /// <summary>Adiciona um novo fundo.</summary>
    /// <param name="fundo">Fundo a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(FundoMunicipalAssistencia fundo, CancellationToken cancellationToken);

    /// <summary>Obtem o fundo por identificador (com suas contas por bloco/piso), tenant-scoped.</summary>
    /// <param name="id">Identificador do fundo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O fundo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<FundoMunicipalAssistencia?> ObterPorIdAsync(FundoMunicipalAssistenciaId id, CancellationToken cancellationToken);
}

/// <summary>Repositorio do <see cref="RegistroMensalAtendimento"/> (RMA, raiz de agregado).</summary>
public interface IRegistroMensalAtendimentoRepository
{
    /// <summary>Adiciona um novo RMA.</summary>
    /// <param name="rma">RMA a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(RegistroMensalAtendimento rma, CancellationToken cancellationToken);

    /// <summary>Obtem o RMA por identificador (com suas linhas), tenant-scoped.</summary>
    /// <param name="id">Identificador do RMA.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O RMA, ou <c>null</c> se inexistente no tenant.</returns>
    Task<RegistroMensalAtendimento?> ObterPorIdAsync(RegistroMensalAtendimentoId id, CancellationToken cancellationToken);

    /// <summary>Obtem o RMA de uma unidade numa competencia (chave de negocio), tenant-scoped.</summary>
    /// <param name="unidadeAtendimentoId">Unidade consolidada.</param>
    /// <param name="competencia">Competencia (ano/mes).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O RMA, ou <c>null</c> se ainda nao aberto no tenant.</returns>
    Task<RegistroMensalAtendimento?> ObterPorUnidadeCompetenciaAsync(Guid unidadeAtendimentoId, Competencia competencia, CancellationToken cancellationToken);
}

/// <summary>
/// <b>A-2 — fonte de consolidacao do RMA a partir do Prontuario SUAS.</b> Le, do prontuario/atendimentos
/// ja existentes (intra-modulo, tenant-scoped), as contagens de atendimentos por servico de uma unidade
/// numa competencia. Evita dupla digitacao (a fonte e o registro do acompanhamento, nao uma planilha
/// paralela). Retorna apenas volumes agregados — nenhum dado sigiloso identificavel sai do prontuario.
/// </summary>
public interface IConsolidacaoRmaReadModel
{
    /// <summary>
    /// Conta os atendimentos por servico (PAIF/PAEFI/SCFV) registrados nos prontuarios de uma unidade,
    /// no mes da competencia, tenant-scoped.
    /// </summary>
    /// <param name="unidadeAtendimentoId">Unidade (CRAS/CREAS/Centro POP).</param>
    /// <param name="competencia">Competencia (ano/mes).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contagem por servico (apenas servicos com atendimento no mes).</returns>
    Task<IReadOnlyDictionary<TipoServico, int>> ContarAtendimentosPorServicoAsync(
        Guid unidadeAtendimentoId,
        Competencia competencia,
        CancellationToken cancellationToken);
}
