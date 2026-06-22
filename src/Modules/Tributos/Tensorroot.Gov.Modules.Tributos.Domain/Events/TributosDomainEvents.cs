using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
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
