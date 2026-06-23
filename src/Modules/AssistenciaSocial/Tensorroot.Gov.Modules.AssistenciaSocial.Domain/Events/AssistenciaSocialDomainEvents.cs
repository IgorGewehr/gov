using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;

/// <summary>
/// Familia referenciada a uma Unidade de Atendimento (CRAS). Emitido pela factory
/// <see cref="Familia.Referenciar"/>. O NIS trafega mascarado (minimizacao — LGPD art. 11).
/// </summary>
/// <param name="FamiliaId">Identificador da familia referenciada.</param>
/// <param name="NisMascarado">NIS do responsavel familiar, mascarado.</param>
/// <param name="UnidadeId">Identificador da Unidade de Atendimento (CRAS) de referencia.</param>
/// <param name="Territorio">Territorio de cobertura ao qual a familia pertence.</param>
/// <param name="DataReferenciamento">Data do referenciamento ao CRAS.</param>
public sealed record FamiliaReferenciada(
    FamiliaId FamiliaId,
    string NisMascarado,
    Guid UnidadeId,
    string Territorio,
    DateOnly DataReferenciamento) : IDomainEvent;

/// <summary>
/// Um atendimento (PAIF/PAEFI/SCFV) foi registrado no prontuario. Carrega apenas
/// identificadores e metadados — nunca o conteudo sigiloso (I-5, I-9).
/// </summary>
/// <param name="ProntuarioSuasId">Identificador do prontuario.</param>
/// <param name="UnidadeId">Unidade de atendimento (CRAS/CREAS) responsavel.</param>
/// <param name="Servico">Servico socioassistencial do atendimento.</param>
/// <param name="DataAtendimento">Data do atendimento.</param>
public sealed record AtendimentoRegistrado(
    ProntuarioSuasId ProntuarioSuasId,
    Guid UnidadeId,
    TipoServico Servico,
    DateOnly DataAtendimento) : IDomainEvent;

/// <summary>
/// O acompanhamento familiar foi encerrado, com motivo (I-6). Carrega apenas
/// identificadores e metadados — nunca o conteudo sigiloso (I-9).
/// </summary>
/// <param name="ProntuarioSuasId">Identificador do prontuario.</param>
/// <param name="MotivoEncerramento">Motivo do encerramento.</param>
public sealed record AcompanhamentoEncerrado(
    ProntuarioSuasId ProntuarioSuasId,
    string MotivoEncerramento) : IDomainEvent;

/// <summary>Beneficio concedido a uma familia (Beneficio I-6). Emitido por <see cref="Beneficio.Conceder"/>.</summary>
/// <param name="BeneficioId">Identificador do beneficio.</param>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Tipo">Tipo do beneficio concedido.</param>
/// <param name="Competencia">Competencia de referencia.</param>
/// <param name="Valor">Valor concedido (nulo em cesta basica/provisao em especie).</param>
public sealed record BeneficioConcedido(
    BeneficioId BeneficioId,
    Guid FamiliaId,
    TipoBeneficio Tipo,
    Competencia Competencia,
    decimal? Valor) : IDomainEvent;

/// <summary>Beneficio indeferido com motivo fundamentado (Beneficio I-7). Emitido por <see cref="Beneficio.Indeferir"/>.</summary>
/// <param name="BeneficioId">Identificador do beneficio.</param>
/// <param name="FamiliaId">Familia requerente.</param>
/// <param name="Tipo">Tipo do beneficio avaliado.</param>
/// <param name="MotivoIndeferimento">Motivo da negativa.</param>
public sealed record BeneficioIndeferido(
    BeneficioId BeneficioId,
    Guid FamiliaId,
    TipoBeneficio Tipo,
    string MotivoIndeferimento) : IDomainEvent;

/// <summary>Cesta basica entregue sobre beneficio eventual concedido (Beneficio I-8). Emitido por <see cref="Beneficio.EntregarCestaBasica"/>.</summary>
/// <param name="BeneficioId">Identificador do beneficio.</param>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Quantidade">Quantidade de cestas entregues.</param>
/// <param name="DataEntrega">Data da entrega.</param>
public sealed record CestaBasicaEntregue(
    BeneficioId BeneficioId,
    Guid FamiliaId,
    int Quantidade,
    DateOnly DataEntrega) : IDomainEvent;

/// <summary>
/// A-2: o RMA de uma unidade foi fechado numa competencia, selando-a para envio ao MDS (RMA/SAGI).
/// Carrega apenas volumes agregados — nunca dado sigiloso identificavel (LGPD art. 11).
/// </summary>
/// <param name="RegistroMensalAtendimentoId">Identificador do RMA fechado.</param>
/// <param name="UnidadeAtendimentoId">Unidade (CRAS/CREAS/Centro POP) consolidada.</param>
/// <param name="Competencia">Competencia (ano/mes) de referencia.</param>
/// <param name="TotalAtendimentos">Total de atendimentos consolidados na competencia.</param>
public sealed record RmaFechado(
    RegistroMensalAtendimentoId RegistroMensalAtendimentoId,
    Guid UnidadeAtendimentoId,
    Competencia Competencia,
    int TotalAtendimentos) : IDomainEvent;

/// <summary>
/// 3d.1: o acompanhamento de condicionalidades do PBF de uma familia escalou para um efeito gradativo
/// com impacto (bloqueio/suspensao) numa competencia — dispara a BUSCA ATIVA do CRAS. Carrega apenas o
/// efeito e a contagem agregada — nunca o conteudo sigiloso do membro/condicionalidade (LGPD art. 11).
/// </summary>
/// <param name="AcompanhamentoId">Identificador do acompanhamento de condicionalidades.</param>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Competencia">Competencia (ano/mes) de referencia.</param>
/// <param name="Efeito">Efeito gradativo vigente (advertencia/bloqueio/suspensao).</param>
/// <param name="DescumprimentosEfetivos">Quantidade de descumprimentos efetivos no periodo.</param>
public sealed record CondicionalidadeDescumprida(
    AcompanhamentoCondicionalidadeId AcompanhamentoId,
    FamiliaId FamiliaId,
    Competencia Competencia,
    EfeitoDescumprimento Efeito,
    int DescumprimentosEfetivos) : IDomainEvent;
