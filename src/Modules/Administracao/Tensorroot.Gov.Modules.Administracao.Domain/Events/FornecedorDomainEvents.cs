using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Events;

/// <summary>Fornecedor cadastrado (nasce <c>Ativo</c>, CNPJ validado na criacao).</summary>
/// <param name="FornecedorId">Identificador do fornecedor.</param>
/// <param name="Cnpj">CNPJ validado do fornecedor.</param>
public sealed record FornecedorCadastrado(FornecedorId FornecedorId, Cnpj Cnpj) : IDomainEvent;

/// <summary>Sancao administrativa aplicada ao fornecedor (art. 156).</summary>
/// <param name="FornecedorId">Identificador do fornecedor.</param>
/// <param name="SancaoId">Identificador da sancao registrada.</param>
/// <param name="Tipo">Tipo da sancao aplicada.</param>
/// <param name="DataInicio">Inicio da vigencia.</param>
/// <param name="DataFim">Termo final da vigencia (nulo = sem termo).</param>
public sealed record FornecedorSancionado(
    FornecedorId FornecedorId,
    Guid SancaoId,
    TipoSancao Tipo,
    DateOnly DataInicio,
    DateOnly? DataFim) : IDomainEvent;

/// <summary>Fornecedor reabilitado (sancao impeditiva cumprida/encerrada; volta a <c>Ativo</c>).</summary>
/// <param name="FornecedorId">Identificador do fornecedor.</param>
public sealed record FornecedorReabilitado(FornecedorId FornecedorId) : IDomainEvent;

/// <summary>Fornecedor inativado (cadastro tornado <c>Inativo</c>).</summary>
/// <param name="FornecedorId">Identificador do fornecedor.</param>
public sealed record FornecedorInativado(FornecedorId FornecedorId) : IDomainEvent;
