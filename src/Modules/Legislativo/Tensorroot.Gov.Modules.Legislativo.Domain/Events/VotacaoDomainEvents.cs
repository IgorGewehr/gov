using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Events;

/// <summary>Votacao iniciada (aberta para registro de votos) — emitido por <c>Votacao.Iniciar</c> (I-1).</summary>
/// <param name="VotacaoId">Identificador da votacao.</param>
/// <param name="ProposicaoId">Materia (proposicao) votada.</param>
/// <param name="Tipo">Modalidade de apuracao.</param>
/// <param name="MaioriaExigida">Criterio de aprovacao exigido.</param>
public sealed record VotacaoIniciada(
    VotacaoId VotacaoId,
    ProposicaoId ProposicaoId,
    TipoVotacao Tipo,
    MaioriaExigida MaioriaExigida) : IDomainEvent;

/// <summary>Voto registrado na trilha imutavel — emitido por <c>Votacao.RegistrarVoto</c> (I-3; um por <c>VotoId</c>).</summary>
/// <param name="VotacaoId">Identificador da votacao.</param>
/// <param name="VotoId">Identificador do voto (origem do painel, chave de idempotencia).</param>
public sealed record VotoRegistrado(VotacaoId VotacaoId, VotoId VotoId) : IDomainEvent;

/// <summary>Votacao encerrada e apurada — emitido por <c>Votacao.Encerrar</c> (I-5).</summary>
/// <param name="VotacaoId">Identificador da votacao.</param>
/// <param name="Resultado">Resultado apurado conforme a maioria exigida.</param>
public sealed record VotacaoEncerrada(VotacaoId VotacaoId, ResultadoVotacao Resultado) : IDomainEvent;
