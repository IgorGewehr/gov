using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Events;

/// <summary>
/// Escola credenciada e habilitada a operar na rede de ensino (situacao inicial
/// <see cref="SituacaoEscola.Credenciada"/>). Emitida pela fabrica <c>Escola.Credenciar</c> (I-4).
/// </summary>
/// <param name="EscolaId">Identificador da escola credenciada.</param>
/// <param name="CodigoInep">Codigo INEP unico nacional da escola.</param>
public sealed record EscolaCredenciada(EscolaId EscolaId, CodigoInep CodigoInep) : IDomainEvent;

/// <summary>
/// Dados cadastrais exigidos pelo EducaCenso atualizados sobre a escola (endereco e
/// infraestrutura). Nao altera a situacao. Emitida por <c>Escola.AtualizarDadosCenso</c> (I-5).
/// </summary>
/// <param name="EscolaId">Identificador da escola atualizada.</param>
public sealed record DadosCensoAtualizados(EscolaId EscolaId) : IDomainEvent;
