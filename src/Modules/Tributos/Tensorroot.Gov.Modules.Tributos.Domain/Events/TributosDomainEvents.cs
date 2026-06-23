using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Events;

/// <summary>Contribuinte cadastrado.</summary>
/// <param name="ContribuinteId">Identificador do contribuinte.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ContribuinteCadastrado(ContribuinteId ContribuinteId, Guid TenantId) : IDomainEvent;

/// <summary>Crédito tributário lançado (constituído).</summary>
/// <param name="LancamentoId">Identificador do lançamento.</param>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
/// <param name="ValorPrincipal">Valor principal lançado.</param>
public sealed record CreditoTributarioLancado(LancamentoId LancamentoId, ContribuinteId ContribuinteId, decimal ValorPrincipal) : IDomainEvent;

/// <summary>Pagamento de um lançamento registrado.</summary>
/// <param name="LancamentoId">Identificador do lançamento pago.</param>
public sealed record PagamentoRegistrado(LancamentoId LancamentoId) : IDomainEvent;

/// <summary>Lançamento inscrito em Dívida Ativa.</summary>
/// <param name="LancamentoId">Lançamento de origem.</param>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
public sealed record LancamentoInscritoEmDividaAtiva(LancamentoId LancamentoId, ContribuinteId ContribuinteId) : IDomainEvent;

/// <summary>Dívida Ativa inscrita.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
/// <param name="LancamentoId">Lançamento de origem.</param>
public sealed record DividaAtivaInscrita(DividaAtivaId DividaAtivaId, ContribuinteId ContribuinteId, LancamentoId LancamentoId) : IDomainEvent;

/// <summary>Certidão de Dívida Ativa (CDA) emitida.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
/// <param name="NumeroCda">Número da CDA.</param>
public sealed record CdaEmitida(DividaAtivaId DividaAtivaId, string NumeroCda) : IDomainEvent;

/// <summary>Parcelamento (REFIS) firmado — suspende a exigibilidade e interrompe a prescrição.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
public sealed record ParcelamentoFirmado(DividaAtivaId DividaAtivaId) : IDomainEvent;

/// <summary>Dívida Ativa quitada.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
public sealed record DividaQuitada(DividaAtivaId DividaAtivaId) : IDomainEvent;

/// <summary>Imóvel cadastrado no cadastro imobiliário.</summary>
/// <param name="ImovelId">Identificador do imóvel.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ProprietarioId">Contribuinte proprietário.</param>
public sealed record ImovelCadastrado(ImovelId ImovelId, Guid TenantId, ContribuinteId ProprietarioId) : IDomainEvent;

/// <summary>Imóvel atualizado (características, endereço, titularidade ou inativação).</summary>
/// <param name="ImovelId">Identificador do imóvel.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ImovelAtualizado(ImovelId ImovelId, Guid TenantId) : IDomainEvent;

/// <summary>Planta Genérica de Valores (PGV) criada.</summary>
/// <param name="PlantaValoresId">Identificador da PGV.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record PlantaValoresCriada(PlantaValoresId PlantaValoresId, Guid TenantId, int Exercicio) : IDomainEvent;

/// <summary>Planta Genérica de Valores (PGV) publicada (vigente).</summary>
/// <param name="PlantaValoresId">Identificador da PGV.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record PlantaValoresPublicada(PlantaValoresId PlantaValoresId, Guid TenantId, int Exercicio) : IDomainEvent;

/// <summary>Tabela de alíquotas do IPTU criada.</summary>
/// <param name="TabelaAliquotaIptuId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record TabelaAliquotaIptuCriada(TabelaAliquotaIptuId TabelaAliquotaIptuId, Guid TenantId, int Exercicio) : IDomainEvent;

/// <summary>Tabela de alíquotas do IPTU publicada (vigente).</summary>
/// <param name="TabelaAliquotaIptuId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record TabelaAliquotaIptuPublicada(TabelaAliquotaIptuId TabelaAliquotaIptuId, Guid TenantId, int Exercicio) : IDomainEvent;

/// <summary>DAM (guia/carnê) gerado para um lançamento.</summary>
/// <param name="DamId">Identificador do DAM.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="LancamentoId">Lançamento de origem.</param>
public sealed record DamGerado(DamId DamId, Guid TenantId, LancamentoId LancamentoId) : IDomainEvent;

/// <summary>DAM totalmente quitado (todas as parcelas pagas).</summary>
/// <param name="DamId">Identificador do DAM.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="LancamentoId">Lançamento de origem.</param>
public sealed record DamQuitado(DamId DamId, Guid TenantId, LancamentoId LancamentoId) : IDomainEvent;

/// <summary>Tabela de alíquotas do ISS criada.</summary>
/// <param name="TabelaAliquotaIssId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="VigenciaInicioAaaaMm">Início de vigência (AAAAMM).</param>
public sealed record TabelaAliquotaIssCriada(TabelaAliquotaIssId TabelaAliquotaIssId, Guid TenantId, int VigenciaInicioAaaaMm) : IDomainEvent;

/// <summary>Tabela de alíquotas do ISS publicada (vigente).</summary>
/// <param name="TabelaAliquotaIssId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="VigenciaInicioAaaaMm">Início de vigência (AAAAMM).</param>
public sealed record TabelaAliquotaIssPublicada(TabelaAliquotaIssId TabelaAliquotaIssId, Guid TenantId, int VigenciaInicioAaaaMm) : IDomainEvent;

/// <summary>Apuração mensal do ISS encerrada (livro eletrônico fechado).</summary>
/// <param name="ApuracaoIssId">Identificador da apuração.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ContribuinteId">Contribuinte (prestador) apurado.</param>
/// <param name="IssProprio">ISS próprio a recolher (R$).</param>
public sealed record ApuracaoIssEncerrada(ApuracaoIssId ApuracaoIssId, Guid TenantId, ContribuinteId ContribuinteId, decimal IssProprio) : IDomainEvent;

/// <summary>Alíquota do ITBI criada.</summary>
/// <param name="AliquotaItbiId">Identificador da configuração.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record AliquotaItbiCriada(AliquotaItbiId AliquotaItbiId, Guid TenantId, int Exercicio) : IDomainEvent;

/// <summary>Alíquota do ITBI publicada (vigente).</summary>
/// <param name="AliquotaItbiId">Identificador da configuração.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record AliquotaItbiPublicada(AliquotaItbiId AliquotaItbiId, Guid TenantId, int Exercicio) : IDomainEvent;

/// <summary>Transmissão imobiliária registrada (fato gerador do ITBI).</summary>
/// <param name="TransmissaoImobiliariaId">Identificador da transmissão.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ImovelId">Imóvel transmitido.</param>
/// <param name="ImpostoDevido">ITBI devido (R$).</param>
public sealed record TransmissaoImobiliariaRegistrada(TransmissaoImobiliariaId TransmissaoImobiliariaId, Guid TenantId, ImovelId ImovelId, decimal ImpostoDevido) : IDomainEvent;

/// <summary>
/// Triagem do ITBI sinalizou divergência relevante entre o valor declarado e o valor venal de
/// referência (Tema 1.113/STJ — tese a/c). NÃO altera o tributo: a guia segue pelo valor declarado.
/// Apenas alimenta a fila de revisão fiscal, que decide (humano) se instaura o arbitramento.
/// </summary>
/// <param name="TransmissaoImobiliariaId">Transmissão sob triagem.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ValorDeclarado">Valor declarado (base da guia), R$.</param>
/// <param name="ValorVenalReferencia">Valor venal de referência (parâmetro de triagem), R$.</param>
public sealed record AlertaDivergenciaItbi(TransmissaoImobiliariaId TransmissaoImobiliariaId, Guid TenantId, decimal ValorDeclarado, decimal ValorVenalReferencia) : IDomainEvent;

/// <summary>Processo de arbitramento da base do ITBI (CTN art. 148) instaurado.</summary>
/// <param name="ProcessoArbitramentoItbiId">Identificador do processo.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="TransmissaoImobiliariaId">Transmissão sob arbitramento.</param>
public sealed record ProcessoArbitramentoItbiInstaurado(ProcessoArbitramentoItbiId ProcessoArbitramentoItbiId, Guid TenantId, TransmissaoImobiliariaId TransmissaoImobiliariaId) : IDomainEvent;

/// <summary>Contribuinte notificado para o contraditório (art. 148, parte final).</summary>
/// <param name="ProcessoArbitramentoItbiId">Identificador do processo.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ContraditorioArbitramentoItbiAberto(ProcessoArbitramentoItbiId ProcessoArbitramentoItbiId, Guid TenantId) : IDomainEvent;

/// <summary>Defesa/avaliação contraditória registrada; processo entra em análise pelo fisco.</summary>
/// <param name="ProcessoArbitramentoItbiId">Identificador do processo.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ContraditorioArbitramentoItbiApresentado(ProcessoArbitramentoItbiId ProcessoArbitramentoItbiId, Guid TenantId) : IDomainEvent;

/// <summary>Processo de arbitramento concluído com decisão final fundamentada (base arbitrada definida).</summary>
/// <param name="ProcessoArbitramentoItbiId">Identificador do processo.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="TransmissaoImobiliariaId">Transmissão arbitrada.</param>
/// <param name="ValorArbitrado">Valor arbitrado (nova base de ofício), R$.</param>
public sealed record ProcessoArbitramentoItbiConcluido(ProcessoArbitramentoItbiId ProcessoArbitramentoItbiId, Guid TenantId, TransmissaoImobiliariaId TransmissaoImobiliariaId, decimal ValorArbitrado) : IDomainEvent;

/// <summary>Processo de arbitramento cancelado — a declaração do contribuinte prevaleceu.</summary>
/// <param name="ProcessoArbitramentoItbiId">Identificador do processo.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ProcessoArbitramentoItbiCancelado(ProcessoArbitramentoItbiId ProcessoArbitramentoItbiId, Guid TenantId) : IDomainEvent;

/// <summary>
/// Arbitramento aplicado a uma transmissão: base elevada para o valor arbitrado (origem
/// <see cref="OrigemBaseCalculoItbi.ArbitradaArt148"/>) — habilita o lançamento de ofício complementar auditado.
/// </summary>
/// <param name="TransmissaoImobiliariaId">Transmissão recalculada.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ProcessoArbitramentoItbiId">Processo de arbitramento de origem.</param>
/// <param name="ImpostoDevido">ITBI devido após o arbitramento (R$).</param>
public sealed record ArbitramentoItbiAplicado(TransmissaoImobiliariaId TransmissaoImobiliariaId, Guid TenantId, ProcessoArbitramentoItbiId ProcessoArbitramentoItbiId, decimal ImpostoDevido) : IDomainEvent;

// ---------------------------------------------------------------------------------------------------
// TAXAS / TLL — poder de polícia e serviço (CTN arts. 77–80, SV 19/29). M6-DESIGN §3.2.
// ---------------------------------------------------------------------------------------------------

/// <summary>Tabela de taxa criada (lei municipal).</summary>
/// <param name="TabelaTaxaId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Codigo">Código da taxa no CTM.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record TabelaTaxaCriada(TabelaTaxaId TabelaTaxaId, Guid TenantId, string Codigo, int Exercicio) : IDomainEvent;

/// <summary>Tabela de taxa publicada (vigente).</summary>
/// <param name="TabelaTaxaId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Codigo">Código da taxa no CTM.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record TabelaTaxaPublicada(TabelaTaxaId TabelaTaxaId, Guid TenantId, string Codigo, int Exercicio) : IDomainEvent;

// ---------------------------------------------------------------------------------------------------
// ALVARÁS — ato de polícia (a TLL é lançada à parte como Taxa). M6-DESIGN §3.3.
// ---------------------------------------------------------------------------------------------------

/// <summary>Alvará emitido (ato administrativo de polícia).</summary>
/// <param name="AlvaraId">Identificador do alvará.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ContribuinteId">Contribuinte titular.</param>
/// <param name="Especie">Espécie do alvará.</param>
public sealed record AlvaraEmitido(AlvaraId AlvaraId, Guid TenantId, ContribuinteId ContribuinteId, EspecieAlvara Especie) : IDomainEvent;

/// <summary>Alvará renovado por novo período.</summary>
/// <param name="AlvaraId">Identificador do alvará.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="NovoFimVigencia">Novo fim de vigência.</param>
public sealed record AlvaraRenovado(AlvaraId AlvaraId, Guid TenantId, DateOnly NovoFimVigencia) : IDomainEvent;

/// <summary>Alvará vencido (vigência encerrada).</summary>
/// <param name="AlvaraId">Identificador do alvará.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record AlvaraVencido(AlvaraId AlvaraId, Guid TenantId) : IDomainEvent;

/// <summary>Alvará cancelado/cassado.</summary>
/// <param name="AlvaraId">Identificador do alvará.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record AlvaraCancelado(AlvaraId AlvaraId, Guid TenantId) : IDomainEvent;

// ---------------------------------------------------------------------------------------------------
// COSIP — Contribuição para Custeio da Iluminação Pública (CF art. 149-A). M6-DESIGN §3.4.
// ---------------------------------------------------------------------------------------------------

/// <summary>Tabela de COSIP criada (lei municipal).</summary>
/// <param name="TabelaCosipId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record TabelaCosipCriada(TabelaCosipId TabelaCosipId, Guid TenantId, int Exercicio) : IDomainEvent;

/// <summary>Tabela de COSIP publicada (vigente).</summary>
/// <param name="TabelaCosipId">Identificador da tabela.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
public sealed record TabelaCosipPublicada(TabelaCosipId TabelaCosipId, Guid TenantId, int Exercicio) : IDomainEvent;

// ---------------------------------------------------------------------------------------------------
// CONTRIBUIÇÃO DE MELHORIA — valorização por obra pública (CTN arts. 81–82). M6-DESIGN §3.5.
// ---------------------------------------------------------------------------------------------------

/// <summary>Edital da obra de Contribuição de Melhoria publicado (CTN art. 82).</summary>
/// <param name="ObraContribuicaoMelhoriaId">Identificador da obra.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="IdentificacaoObra">Identificação da obra.</param>
public sealed record ObraMelhoriaEditalPublicado(ObraContribuicaoMelhoriaId ObraContribuicaoMelhoriaId, Guid TenantId, string IdentificacaoObra) : IDomainEvent;

/// <summary>Prazo de impugnação ao edital encerrado (apto a ratear).</summary>
/// <param name="ObraContribuicaoMelhoriaId">Identificador da obra.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ObraMelhoriaImpugnacaoEncerrada(ObraContribuicaoMelhoriaId ObraContribuicaoMelhoriaId, Guid TenantId) : IDomainEvent;

/// <summary>Contribuição de Melhoria rateada entre os imóveis beneficiados.</summary>
/// <param name="ObraContribuicaoMelhoriaId">Identificador da obra.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="TotalRateado">Valor total efetivamente rateado (R$).</param>
public sealed record ObraMelhoriaRateada(ObraContribuicaoMelhoriaId ObraContribuicaoMelhoriaId, Guid TenantId, decimal TotalRateado) : IDomainEvent;

/// <summary>Obra de Contribuição de Melhoria cancelada.</summary>
/// <param name="ObraContribuicaoMelhoriaId">Identificador da obra.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ObraMelhoriaCancelada(ObraContribuicaoMelhoriaId ObraContribuicaoMelhoriaId, Guid TenantId) : IDomainEvent;
