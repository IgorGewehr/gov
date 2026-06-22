namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Resumo de um contrato para listagens (tenant-scoped).</summary>
/// <param name="Id">Identificador do contrato.</param>
/// <param name="FornecedorId">Fornecedor contratado.</param>
/// <param name="Objeto">Descricao do objeto.</param>
/// <param name="ValorAtual">Valor vigente.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="Situacao">Situacao atual.</param>
public sealed record ContratoResumo(
    Guid Id,
    Guid FornecedorId,
    string Objeto,
    decimal ValorAtual,
    DateOnly VigenciaFim,
    string Situacao);
